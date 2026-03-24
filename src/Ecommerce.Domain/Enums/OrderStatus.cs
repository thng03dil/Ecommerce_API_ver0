using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Enums
{
    public enum OrderStatus
    {
        Pending = 0,    // Chờ thanh toán (Mặc định)
        Paid = 1,       // Đã thanh toán
        Shipping = 2,   // Đang giao hàng
        Cancelled = 3,  // Đã hủy
        Completed = 4   // Hoàn thành
    }
}
