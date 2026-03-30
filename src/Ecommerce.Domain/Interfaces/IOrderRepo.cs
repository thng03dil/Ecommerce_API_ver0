using Ecommerce.Domain.Common;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;

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

    Task<(IReadOnlyList<Order> Items, int TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize, CancellationToken cancellationToken = default);

    Task<Order?> GetByIdWithItemsAndProductsAsync(int orderId, CancellationToken cancellationToken = default);

    Task<(IReadOnlyList<Order> Items, int TotalCount)> ListForUserPagedAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<Order?> GetByIdTrackedAsync(int orderId, CancellationToken cancellationToken = default);

    Task<bool> TryUpdateStatusAsync(int orderId, OrderStatus newStatus, CancellationToken cancellationToken = default);

    /// <summary>
    /// Hủy đơn Pending chưa thanh toán: hoàn kho, đặt <see cref="OrderStatus.Cancelled"/>.
    /// </summary>
    Task<OrderCancelResult> TryCancelPendingOrderForUserAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default);
}
