using System.Text.Json.Serialization;
using Ecommerce.Application.DTOs.Order;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Application.DTOs.OrderDtos;

public class OrderResponseDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public decimal TotalAmount { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public OrderStatus Status { get; set; }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public PaymentStatus PaymentStatus { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? StripeCheckoutSessionId { get; set; }
    public string? StripePaymentIntentId { get; set; }

    public List<OrderItemDetailResponseDto> Items { get; set; } = new();
}
