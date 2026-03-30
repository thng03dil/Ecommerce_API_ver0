using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs.OrderDtos;

public class CreateOrderDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Order must contain at least one product.")]
    public List<OrderItemRequestDto> Items { get; set; } = new();
}
