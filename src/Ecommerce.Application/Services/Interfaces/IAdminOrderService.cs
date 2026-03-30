using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Application.DTOs.OrderDtos;

namespace Ecommerce.Application.Services.Interfaces
{
    public interface IOrderAdminService
    {
        Task<ApiResponse<PagedResponse<OrderResponseDto>>> GetAllOrdersAsync(PaginationDto pagination);

        Task<ApiResponse<OrderDetailResponseDto>> GetOrderDetailAsync(int orderId);

        Task<ApiResponse<PagedResponse<OrderResponseDto>>> GetOrdersByUserAsync(int userId, PaginationDto pagination);

        Task<ApiResponse<object?>> UpdateStatusAsync(int orderId, UpdateOrderStatusRequestDto request);
    }
}
