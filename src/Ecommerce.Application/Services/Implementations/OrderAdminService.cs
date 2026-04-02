using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Application.DTOs.OrderDtos;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Interfaces;

namespace Ecommerce.Application.Services.Implementations;

public class OrderAdminService : IOrderAdminService
{
    private readonly IOrderRepo _orderRepo;
    private readonly ICacheService _cacheService;
    private readonly IOrderPaymentService _paymentService;

    public OrderAdminService(
        IOrderRepo orderRepo,
        ICacheService cacheService,
        IOrderPaymentService paymentService)
    {
        _orderRepo = orderRepo;
        _cacheService = cacheService;
        _paymentService = paymentService;
    }

    public async Task<ApiResponse<PagedResponse<OrderResponseDto>>> GetAllOrdersAsync(
        PaginationDto pagination,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _orderRepo.GetAllPagedAsync(
            pagination.PageNumber,
            pagination.PageSize,
            cancellationToken);
        var data = items.Select(OrderResponseMapper.ToDto).ToList();
        var paged = new PagedResponse<OrderResponseDto>(data, pagination.PageNumber, pagination.PageSize, total);
        return ApiResponse<PagedResponse<OrderResponseDto>>.SuccessResponse(paged);
    }

    public async Task<ApiResponse<OrderDetailResponseDto>> GetOrderDetailAsync(
    int orderId,
    CancellationToken cancellationToken = default)
    {
        var order = await _orderRepo.GetByIdWithItemsAndProductsAsync(orderId, cancellationToken);
        
        if (order == null)
            return ApiResponse<OrderDetailResponseDto>.ErrorResponse("Order not found.", 404);

        var dto = OrderResponseMapper.ToDetailDto(order);

        return ApiResponse<OrderDetailResponseDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<PagedResponse<OrderResponseDto>>> GetOrdersByUserAsync(
        int userId,
        PaginationDto pagination,
        CancellationToken cancellationToken = default)
    {
        var (items, total) = await _orderRepo.ListForUserPagedAsync(
            userId,
            pagination.PageNumber,
            pagination.PageSize,
            cancellationToken);

        var data = items.Select(OrderResponseMapper.ToDto).ToList();
        var paged = new PagedResponse<OrderResponseDto>(data, pagination.PageNumber, pagination.PageSize, total);
        return ApiResponse<PagedResponse<OrderResponseDto>>.SuccessResponse(paged);
    }

    public async Task<OrderResponseDto> CancelOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var existing = await _orderRepo.GetByIdWithItemsAndProductsAsync(orderId, cancellationToken);
        if (existing == null)
            throw new NotFoundException("Order not found.");

        await TryStripeRefundIfPaidAsync(existing, cancellationToken);

        var outcome = await _orderRepo.TryCancelOrderByAdminAsync(orderId, cancellationToken);

        if (!outcome.Success)
        {
            if (outcome.Failure == OrderCancelFailure.NotFound)
                throw new NotFoundException("Order not found.");
            throw new ConflictException("This order cannot be cancelled.");
        }

        await _cacheService.IncrementAsync(CacheKeyGenerator.CategoryVersionKey());

        var reloaded = await _orderRepo.GetByIdWithItemsAndProductsAsync(orderId, cancellationToken);
        if (reloaded != null)
            return OrderResponseMapper.ToDto(reloaded);

        return FallbackCancelledDto(outcome);
    }

    public async Task<OrderResponseDto> ApproveReturnAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var existing = await _orderRepo.GetByIdWithItemsAndProductsAsync(orderId, cancellationToken);
        if (existing == null)
            throw new NotFoundException("Order not found.");

        if (existing.Status != OrderStatus.ReturnRequested)
            throw new ConflictException("Order is not awaiting return approval.");

        await TryStripeRefundIfPaidAsync(existing, cancellationToken);

        var outcome = await _orderRepo.TryProcessReturnByAdminAsync(orderId, cancellationToken);

        if (!outcome.Success)
            throw new ConflictException("Return could not be processed.");

        await _cacheService.IncrementAsync(CacheKeyGenerator.CategoryVersionKey());

        var reloaded = await _orderRepo.GetByIdWithItemsAndProductsAsync(orderId, cancellationToken);
        if (reloaded != null)
            return OrderResponseMapper.ToDto(reloaded);

        return FallbackCancelledDto(outcome);
    }

    public async Task<ApiResponse<OrderDetailResponseDto>> UpdateOrderStatusAsync(
        int orderId,
        AdminOrderFulfillmentStatus newStatus,
        CancellationToken ct)
    {
        if (await _orderRepo.GetByIdTrackedAsync(orderId, ct) == null)
            return ApiResponse<OrderDetailResponseDto>.ErrorResponse("Order not found.", 404);

        var orderStatus = (OrderStatus)(int)newStatus;

        var success = await _orderRepo.TryUpdateStatusByAdminAsync(orderId, orderStatus, ct);

        if (!success)
        {
            return ApiResponse<OrderDetailResponseDto>.ErrorResponse(
                "Status cannot be updated. The order must be paid (Succeeded), and the new status must advance the workflow (e.g. Paid → Shipping → Completed).",
                409);
        }

        var order = await _orderRepo.GetByIdWithItemsAndProductsAsync(orderId, ct); 
        
        if (order == null)
            return ApiResponse<OrderDetailResponseDto>.ErrorResponse("Order not found after update.", 404);

        var dto = OrderResponseMapper.ToDetailDto(order);

        return ApiResponse<OrderDetailResponseDto>.SuccessResponse(
            dto,
            $"Order status successfully updated to {orderStatus}.");
    }
    
    private async Task TryStripeRefundIfPaidAsync(Order order, CancellationToken cancellationToken)
    {
        if (order.PaymentStatus != PaymentStatus.Succeeded)
            return;

        if (string.IsNullOrWhiteSpace(order.StripePaymentIntentId))
            return;

        var refunded = await _paymentService.RefundAsync(order.StripePaymentIntentId!, cancellationToken);
        if (!refunded)
            throw new ConflictException("Payment refund could not be processed.");
    }

    private static OrderResponseDto FallbackCancelledDto(OrderCancelResult outcome) =>
        new()
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
