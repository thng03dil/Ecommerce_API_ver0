using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.OrderDtos;

namespace Ecommerce.Application.Services.Interfaces;

public interface IOrderService
{
    Task<ApiResponse<OrderResponseDto>> PlaceOrderAsync(int userId, CreateOrderDto dto, CancellationToken cancellationToken = default);

    Task<ApiResponse<OrderResponseDto>> GetByIdForUserAsync(int userId, int orderId, CancellationToken cancellationToken = default);

    Task<ApiResponse<IReadOnlyList<OrderResponseDto>>> ListForUserAsync(int userId, CancellationToken cancellationToken = default);
}
