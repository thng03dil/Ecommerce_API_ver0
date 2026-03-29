using Ecommerce.Domain.Common;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Domain.Interfaces;

public interface IOrderRepo
{
    /// <summary>
    /// Đặt hàng trong một transaction: trừ kho có điều kiện (atomic), tạo Order + OrderItems.
    /// Commit DB trước khi caller bump cache.
    /// </summary>
    Task<OrderPlaceResult> PlaceOrderAsync(
        int userId,
        IReadOnlyList<OrderLineInput> lines,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdForUserWithItemsAndProductsAsync(int orderId, int userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Order>> ListForUserAsync(int userId, CancellationToken cancellationToken = default);

    Task<bool> TrySetStripeCheckoutSessionIdAsync(int orderId, int userId, string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Idempotent: returns true if order was updated to Paid or was already Paid for this session.
    /// </summary>
    Task<bool> TryMarkPaidByStripeSessionAsync(string stripeCheckoutSessionId, string? paymentIntentId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels pending orders past <see cref="Order.PaymentExpiresAt"/> and restores product stock.
    /// </summary>
    Task<int> CancelExpiredPendingOrdersAndRestockAsync(CancellationToken cancellationToken = default);
}
