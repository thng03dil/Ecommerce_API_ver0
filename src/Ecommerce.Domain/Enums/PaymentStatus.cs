namespace Ecommerce.Domain.Enums;

/// <summary>
/// Trạng thái thanh toán, tách khỏi <see cref="OrderStatus"/> (giao hàng / vòng đời đơn).
/// </summary>
public enum PaymentStatus
{
    NotPaid = 0,
    Succeeded = 1,
    Failed = 2
}
