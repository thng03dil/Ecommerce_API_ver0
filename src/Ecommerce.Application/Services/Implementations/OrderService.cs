using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.OrderDtos;
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

    public OrderService(IOrderRepo orderRepo, ICacheService cacheService)
    {
        _orderRepo = orderRepo;
        _cacheService = cacheService;
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

        return ApiResponse<OrderResponseDto>.SuccessResponse(MapToDto(order));
    }

    public async Task<ApiResponse<IReadOnlyList<OrderResponseDto>>> ListForUserAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        if (userId <= 0)
            return ApiResponse<IReadOnlyList<OrderResponseDto>>.ErrorResponse("User is not authenticated.", 401);

        var orders = await _orderRepo.ListForUserAsync(userId, cancellationToken);
        var dtos = orders.Select(MapToDto).ToList();
        return ApiResponse<IReadOnlyList<OrderResponseDto>>.SuccessResponse(dtos);
    }

    private static OrderResponseDto MapToDto(Order order) =>
        new()
        {
            Id = order.Id,
            UserId = order.UserId,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            CreatedAt = order.CreatedAt,
            PaidAt = order.PaidAt,
            PaymentExpiresAt = order.PaymentExpiresAt
        };

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

        var response = new OrderResponseDto
        {
            Id = outcome.OrderId,
            UserId = userId,
            TotalAmount = outcome.TotalAmount,
            Status = OrderStatus.Pending,
            CreatedAt = outcome.CreatedAt,
            PaymentExpiresAt = outcome.PaymentExpiresAt
        };

        return ApiResponse<OrderResponseDto>.SuccessResponse(response, "Order placed successfully.");
    }
}
