namespace Ecommerce.Domain.Interfaces;

public sealed class OrderPlaceResult
{
    public bool Success { get; private init; }
    public string? ErrorMessage { get; private init; }
    public int OrderId { get; private init; }
    public decimal TotalAmount { get; private init; }
    public DateTime CreatedAt { get; private init; }
    public DateTime? PaymentExpiresAt { get; private init; }

    public static OrderPlaceResult Ok(int orderId, decimal totalAmount, DateTime createdAt, DateTime? paymentExpiresAt = null) =>
        new() { Success = true, OrderId = orderId, TotalAmount = totalAmount, CreatedAt = createdAt, PaymentExpiresAt = paymentExpiresAt };

    public static OrderPlaceResult Fail(string message) =>
        new() { Success = false, ErrorMessage = message };
}
