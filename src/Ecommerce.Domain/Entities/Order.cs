using System;
using System.Collections.Generic;
using System.Text;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Entities
{
    public class Order
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public decimal TotalAmount { get; set; }
        public OrderStatus Status { get; set; } = OrderStatus.Pending;
        public PaymentStatus PaymentStatus { get; set; } = PaymentStatus.NotPaid;

        public string? StripeCheckoutSessionId { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public DateTime? PaidAt { get; set; }

        // Navigation property
        public User? User { get; set; }
        public  ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
