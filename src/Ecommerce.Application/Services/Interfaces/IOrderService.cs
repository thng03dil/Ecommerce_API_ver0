using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Application.DTOs.OrderDtos;
using Ecommerce.Application.Common.Pagination;

namespace Ecommerce.Application.Services.Interfaces;

public interface IOrderService
{
    Task<ApiResponse<OrderResponseDto>> GetByIdForUserAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default);
    Task<ApiResponse<IReadOnlyList<OrderResponseDto>>> ListForUserAsync(
         int userId,
         CancellationToken cancellationToken = default);

    Task<ApiResponse<OrderResponseDto>> PlaceOrderAsync(
     int userId,
     CreateOrderDto dto,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// Hủy đơn Pending chưa thanh toán. Lỗi nghiệp vụ: ném exception kế thừa BaseException.
    /// </summary>
    Task<OrderResponseDto> CancelPendingOrderAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Yêu cầu hoàn hàng: chỉ khi đơn <see cref="OrderStatus.Completed"/> và đã thanh toán (<see cref="PaymentStatus.Succeeded"/>).
    /// </summary>
    Task<ApiResponse<OrderResponseDto>> RequestReturnAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default);
}
