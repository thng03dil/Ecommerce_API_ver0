namespace Ecommerce.Application.DTOs.OrderDtos;

public class CheckoutSessionResponseDto
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;

    /// <summary>Đơn sau khi gắn Stripe session (lines, Stripe ids, trạng thái).</summary>
    public OrderResponseDto? Order { get; set; }
}
