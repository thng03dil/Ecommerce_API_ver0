using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.DTOs.OrderDtos;

public class OrderResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
