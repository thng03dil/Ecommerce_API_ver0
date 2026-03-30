using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Application.DTOs.OrderDtos;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Interfaces;

namespace Ecommerce.Application.Services.Implementations;

public class OrderAdminService : IOrderAdminService
{
    private readonly IOrderRepo _orderRepo;

    public OrderAdminService(IOrderRepo orderRepo)
    {
        _orderRepo = orderRepo;
    }

    public async Task<ApiResponse<PagedResponse<OrderResponseDto>>> GetAllOrdersAsync(PaginationDto pagination)
    {
        var (items, total) = await _orderRepo.GetAllPagedAsync(pagination.PageNumber, pagination.PageSize);
        var data = items.Select(OrderResponseMapper.ToDto).ToList();
        var paged = new PagedResponse<OrderResponseDto>(data, pagination.PageNumber, pagination.PageSize, total);
        return ApiResponse<PagedResponse<OrderResponseDto>>.SuccessResponse(paged);
    }

    public async Task<ApiResponse<OrderDetailResponseDto>> GetOrderDetailAsync(int orderId)
    {
        var order = await _orderRepo.GetByIdWithItemsAndProductsAsync(orderId);
        if (order == null)
            return ApiResponse<OrderDetailResponseDto>.ErrorResponse("Order not found.", 404);

        var dto = new OrderDetailResponseDto
        {
            Id = order.Id,
            UserId = order.UserId,
            TotalAmount = order.TotalAmount,
            Status = order.Status.ToString(),
            PaymentStatus = order.PaymentStatus.ToString(),
            CreatedAt = order.CreatedAt,
            PaidAt = order.PaidAt
        };

        return ApiResponse<OrderDetailResponseDto>.SuccessResponse(dto);
    }

    public async Task<ApiResponse<PagedResponse<OrderResponseDto>>> GetOrdersByUserAsync(int userId, PaginationDto pagination)
    {
        var (items, total) = await _orderRepo.ListForUserPagedAsync(
            userId,
            pagination.PageNumber,
            pagination.PageSize);

        var data = items.Select(OrderResponseMapper.ToDto).ToList();
        var paged = new PagedResponse<OrderResponseDto>(data, pagination.PageNumber, pagination.PageSize, total);
        return ApiResponse<PagedResponse<OrderResponseDto>>.SuccessResponse(paged);
    }

    public async Task<ApiResponse<object?>> UpdateStatusAsync(int orderId, UpdateOrderStatusRequestDto request)
    {
        var updated = await _orderRepo.TryUpdateStatusAsync(orderId, request.Status);
        if (!updated)
            return ApiResponse<object?>.ErrorResponse("Order not found.", 404);

        return ApiResponse<object?>.SuccessResponse(null, "Order status updated.");
    }
}
