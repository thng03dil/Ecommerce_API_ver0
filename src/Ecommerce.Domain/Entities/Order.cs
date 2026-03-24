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

        // Navigation property
        public  ICollection<OrderDetail> OrderItems { get; set; } = new List<OrderDetail>();
    }
}
