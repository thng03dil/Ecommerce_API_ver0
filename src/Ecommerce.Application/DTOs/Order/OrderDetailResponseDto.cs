using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.DTOs.Order
{
    public class OrderDetailResponseDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public List<OrderItemDetailResponseDto> Items { get; set; } = new();
        public int ItemCount => Items.Count;
    }
}
