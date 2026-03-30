using System.ComponentModel.DataAnnotations;

namespace Ecommerce.Application.DTOs.OrderDtos;

public class OrderItemRequestDto
{
    [Range(1, int.MaxValue)]
    public int ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
