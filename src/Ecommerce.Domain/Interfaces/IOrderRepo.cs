using Ecommerce.Domain.Common;

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
}
