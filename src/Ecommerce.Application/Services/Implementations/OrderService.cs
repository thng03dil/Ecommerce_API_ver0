using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Application.DTOs.OrderDtos;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Interfaces;

namespace Ecommerce.Application.Services.Implementations;

public class OrderService : IOrderService
{
    private readonly IOrderRepo _orderRepo;
    private readonly ICacheService _cacheService;
    private readonly IOrderPaymentService _paymentService;

    public OrderService(
        IOrderRepo orderRepo,
        ICacheService cacheService,
        IOrderPaymentService paymentService)
    {
        _orderRepo = orderRepo;
        _cacheService = cacheService;
        _paymentService = paymentService;
    }

    public async Task<ApiResponse<OrderResponseDto>> GetByIdForUserAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return ApiResponse<OrderResponseDto>.ErrorResponse("User is not authenticated.", 401);

        var order = await _orderRepo.GetByIdForUserWithItemsAndProductsAsync(orderId, userId, cancellationToken);
        if (order == null)
            return ApiResponse<OrderResponseDto>.ErrorResponse("Order not found.", 404);

        return ApiResponse<OrderResponseDto>.SuccessResponse(OrderResponseMapper.ToDto(order));
    }

    public async Task<ApiResponse<IReadOnlyList<OrderResponseDto>>> ListForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return ApiResponse<IReadOnlyList<OrderResponseDto>>.ErrorResponse("User is not authenticated.", 401);

        var orders = await _orderRepo.ListForUserAsync(userId, cancellationToken);
        var dtos = orders.Select(OrderResponseMapper.ToDto).ToList();
        return ApiResponse<IReadOnlyList<OrderResponseDto>>.SuccessResponse(dtos);
    }

    public async Task<ApiResponse<OrderResponseDto>> PlaceOrderAsync(
        int userId,
        CreateOrderDto dto,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return ApiResponse<OrderResponseDto>.ErrorResponse("User is not authenticated.", 401);

        if (dto.Items == null || dto.Items.Count == 0)
            return ApiResponse<OrderResponseDto>.ErrorResponse("Order must contain at least one line.");

        var lines = dto.Items
            .Select(x => new OrderLineInput(x.ProductId, x.Quantity))
            .ToList();

        var outcome = await _orderRepo.PlaceOrderAsync(userId, lines, cancellationToken);

        if (!outcome.Success)
            return ApiResponse<OrderResponseDto>.ErrorResponse(outcome.ErrorMessage ?? "Order failed.");

        await _cacheService.IncrementAsync(CacheKeyGenerator.CategoryVersionKey());

        var order = await _orderRepo.GetByIdForUserWithItemsAndProductsAsync(outcome.OrderId, userId, cancellationToken);
        if (order == null)
        {
            return ApiResponse<OrderResponseDto>.SuccessResponse(
                201,
                new OrderResponseDto
                {
                    Id = outcome.OrderId,
                    UserId = userId,
                    TotalAmount = outcome.TotalAmount,
                    Status = OrderStatus.Pending,
                    PaymentStatus = PaymentStatus.NotPaid,
                    CreatedAt = outcome.CreatedAt,
                    ItemCount = 0,
                    Items = new List<OrderItemDetailResponseDto>()
                },
                "Order placed successfully.");
        }

        return ApiResponse<OrderResponseDto>.SuccessResponse(
            OrderResponseMapper.ToDto(order),
            "Order placed successfully.");
    }

    public async Task<OrderResponseDto> CancelPendingOrderAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            throw new UnauthorizedException("User is not authenticated.");

        var existing = await _orderRepo.GetByIdForUserWithItemsAndProductsAsync(orderId, userId, cancellationToken);
        if (existing == null)
            throw new NotFoundException("Order not found.");

        if (existing.PaymentStatus == PaymentStatus.Succeeded
            && !string.IsNullOrWhiteSpace(existing.StripePaymentIntentId))
        {
            var refunded = await _paymentService.RefundAsync(existing.StripePaymentIntentId!, cancellationToken);
            if (!refunded)
                throw new ConflictException("Payment refund could not be processed. Order was not cancelled.");
        }

        var outcome = await _orderRepo.TryCancelOrderByUserAsync(orderId, userId, cancellationToken);

        if (!outcome.Success)
        {
            if (outcome.Failure == OrderCancelFailure.NotFound)
                throw new NotFoundException("Order not found.");
            throw new ConflictException("This order cannot be cancelled.");
        }

        await _cacheService.IncrementAsync(CacheKeyGenerator.CategoryVersionKey());

        var reloaded = await _orderRepo.GetByIdForUserWithItemsAndProductsAsync(orderId, userId, cancellationToken);
        if (reloaded != null)
            return OrderResponseMapper.ToDto(reloaded);

        return new OrderResponseDto
        {
            Id = outcome.OrderId,
            UserId = outcome.UserId,
            TotalAmount = outcome.TotalAmount,
            Status = OrderStatus.Cancelled,
            PaymentStatus = outcome.PaymentStatus,
            CreatedAt = outcome.CreatedAt,
            PaidAt = outcome.PaidAt,
            StripeCheckoutSessionId = null,
            StripePaymentIntentId = null,
            ItemCount = 0,
            Items = new List<OrderItemDetailResponseDto>()
        };
    }

    public async Task<ApiResponse<OrderResponseDto>> RequestReturnAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return ApiResponse<OrderResponseDto>.ErrorResponse("User is not authenticated.", 401);

        var outcome = await _orderRepo.TryRequestReturnByUserAsync(orderId, userId, cancellationToken);

        if (!outcome.Success) 
        {
            return outcome.Failure switch
            {
                OrderReturnRequestFailure.NotFound =>
                    ApiResponse<OrderResponseDto>.ErrorResponse("Order not found.", 404),
                OrderReturnRequestFailure.AlreadyRequested =>
                    ApiResponse<OrderResponseDto>.ErrorResponse("Return has already been requested for this order.", 409),
                OrderReturnRequestFailure.NotEligible =>
                    ApiResponse<OrderResponseDto>.ErrorResponse(
                        "Return can only be requested for completed orders that have been paid.",
                        400),
                _ => ApiResponse<OrderResponseDto>.ErrorResponse("Return request could not be processed.", 400)
            };
        }

        var order = await _orderRepo.GetByIdForUserWithItemsAndProductsAsync(orderId, userId, cancellationToken);
        if (order == null)
            return ApiResponse<OrderResponseDto>.ErrorResponse("Order not found.", 404);

        return ApiResponse<OrderResponseDto>.SuccessResponse(
            OrderResponseMapper.ToDto(order),
            "Return request submitted.");
    }
}
