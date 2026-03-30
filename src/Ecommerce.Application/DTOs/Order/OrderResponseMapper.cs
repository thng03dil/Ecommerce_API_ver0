using Ecommerce.Application.DTOs.Order;
using OrderEntity = Ecommerce.Domain.Entities.Order;
using OrderItemEntity = Ecommerce.Domain.Entities.OrderItem;

namespace Ecommerce.Application.DTOs.OrderDtos;

public static class OrderResponseMapper
{
    public static OrderResponseDto ToDto(OrderEntity order)
    {
        var items = (order.OrderItems ?? Enumerable.Empty<OrderItemEntity>())
            .Select(MapItem)
            .ToList();

        return new OrderResponseDto
        {
            Id = order.Id,
            UserId = order.UserId,
            TotalAmount = order.TotalAmount,
            Status = order.Status,
            PaymentStatus = order.PaymentStatus,
            CreatedAt = order.CreatedAt,
            PaidAt = order.PaidAt,
            StripeCheckoutSessionId = order.StripeCheckoutSessionId,
            StripePaymentIntentId = order.StripePaymentIntentId,
            ItemCount = items.Count,
            Items = items
        };
    }

    private static OrderItemDetailResponseDto MapItem(OrderItemEntity i) =>
        new()
        {
            ProductId = i.ProductId,
            ProductName = i.Product?.Name ?? $"Product {i.ProductId}",
            ProductImageUrl = null,
            Quantity = i.Quantity,
            PriceAtPurchase = i.PriceAtPurchase,
            Subtotal = i.PriceAtPurchase * i.Quantity
        };
}
