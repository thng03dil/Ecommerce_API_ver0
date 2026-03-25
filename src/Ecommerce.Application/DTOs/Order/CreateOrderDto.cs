using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs.OrderDtos;

public class CreateOrderDto
{
    [Required]
    [MinLength(1, ErrorMessage = "Order must contain at least one line.")]
    public List<OrderLineDto> Items { get; set; } = new();
}
