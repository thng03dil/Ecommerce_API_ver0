using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.DTOs.Order
{
    public class OrderItemDetailResponseDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string? ProductImageUrl { get; set; }
        public int Quantity { get; set; }
        public decimal PriceAtPurchase { get; set; }
        public decimal Subtotal { get; set; }
    }
}
