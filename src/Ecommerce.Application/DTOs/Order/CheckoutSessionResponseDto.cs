namespace Ecommerce.Application.DTOs.OrderDtos;

public class CheckoutSessionResponseDto
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string SessionId { get; set; } = string.Empty;
}
