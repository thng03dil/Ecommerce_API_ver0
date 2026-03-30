using Ecommerce.Domain.Enums;

namespace Ecommerce.Domain.Interfaces;

public enum OrderCancelFailure
{
    NotFound,
    NotCancellable
}

public sealed class OrderCancelResult
{
    public bool Success { get; private init; }
    public OrderCancelFailure? Failure { get; private init; }
    public int OrderId { get; private init; }
    public int UserId { get; private init; }
    public decimal TotalAmount { get; private init; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? PaidAt { get; private init; }
    public PaymentStatus PaymentStatus { get; private init; }

    public static OrderCancelResult Ok(
        int orderId,
        int userId,
        decimal totalAmount,
        DateTime createdAt,
        DateTime? paidAt,
        PaymentStatus paymentStatus) =>
        new()
        {
            Success = true,
            OrderId = orderId,
            UserId = userId,
            TotalAmount = totalAmount,
            CreatedAt = createdAt,
            PaidAt = paidAt,
            PaymentStatus = paymentStatus
        };

    public static OrderCancelResult Fail(OrderCancelFailure failure) =>
        new() { Success = false, Failure = failure };
}
