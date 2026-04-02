using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Application.DTOs.OrderDtos;
namespace Ecommerce.Application.Services.Interfaces;

public interface IOrderAdminService
{
    Task<ApiResponse<PagedResponse<OrderResponseDto>>> GetAllOrdersAsync(
        PaginationDto pagination,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<OrderDetailResponseDto>> GetOrderDetailAsync(
        int orderId,
        CancellationToken cancellationToken = default);

    Task<ApiResponse<PagedResponse<OrderResponseDto>>> GetOrdersByUserAsync(
        int userId,
        PaginationDto pagination,
        CancellationToken cancellationToken = default);

    Task<OrderResponseDto> CancelOrderAsync(int orderId, CancellationToken cancellationToken = default);

    Task<OrderResponseDto> ApproveReturnAsync(int orderId, CancellationToken cancellationToken = default);
    Task<ApiResponse<OrderDetailResponseDto>> UpdateOrderStatusAsync(
        int orderId,
        AdminOrderFulfillmentStatus newStatus,
        CancellationToken cancellationToken = default);
}
