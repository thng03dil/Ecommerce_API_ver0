using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.OrderDtos;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common;
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

        await _cacheService.IncrementAsync(CacheKeyGenerator.ProductVersionKey());
        await _cacheService.IncrementAsync(CacheKeyGenerator.CategoryVersionKey());

        var response = new OrderResponseDto
        {
            Id = outcome.OrderId,
            UserId = userId,
            TotalAmount = outcome.TotalAmount,
            Status = OrderStatus.Pending,
            CreatedAt = outcome.CreatedAt
        };

        return ApiResponse<OrderResponseDto>.SuccessResponse(response, "Order placed successfully.");
    }
}
