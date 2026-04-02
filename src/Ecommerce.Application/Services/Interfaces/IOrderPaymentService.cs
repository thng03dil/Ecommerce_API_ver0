using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.OrderDtos;

namespace Ecommerce.Application.Services.Interfaces;

public interface IOrderPaymentService
{
    Task<ApiResponse<CheckoutSessionResponseDto>> CreateCheckoutSessionAsync(
        int userId,
        int orderId,
        CancellationToken cancellationToken = default);
        
    Task<bool> RefundAsync(string paymentIntentId, CancellationToken ct = default);  
        

}
