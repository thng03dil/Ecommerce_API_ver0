using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Application.DTOs.Order
{
    public class CheckoutResponseDto
    {
        public int OrderId { get; set; }
        public decimal TotalAmount { get; set; }
        public string PaymentUrl { get; set; } = string.Empty;
    }
}
