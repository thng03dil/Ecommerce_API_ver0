using Ecommerce.Domain.Common;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories;

public class OrderRepo : IOrderRepo
{
    private readonly AppDbContext _context;

    public OrderRepo(AppDbContext context)
    {
        _context = context;
    }

    public async Task<OrderPlaceResult> PlaceOrderAsync(
        int userId,
        IReadOnlyList<OrderLineInput> lines,
        CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0)
            return OrderPlaceResult.Fail("Order must contain at least one line.");

        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var lineDetails = new List<(int ProductId, int Quantity, decimal Price)>();

            foreach (var line in lines)
            {
                if (line.Quantity <= 0)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return OrderPlaceResult.Fail("Each line must have quantity greater than zero.");
                }

                var snapshot = await _context.Products
                    .AsNoTracking()
                    .Where(p => p.Id == line.ProductId && !p.IsDeleted)
                    .Select(p => new { p.Price, p.Stock })
                    .FirstOrDefaultAsync(cancellationToken);

                if (snapshot == null)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return OrderPlaceResult.Fail($"Product {line.ProductId} was not found.");
                }

                var rows = await _context.Products
                    .Where(p => p.Id == line.ProductId && !p.IsDeleted && p.Stock >= line.Quantity)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(p => p.Stock, p => p.Stock - line.Quantity),
                        cancellationToken);

                if (rows == 0)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return OrderPlaceResult.Fail($"Insufficient stock for product {line.ProductId}.");
                }

                lineDetails.Add((line.ProductId, line.Quantity, snapshot.Price));
            }

            var total = lineDetails.Sum(x => x.Price * x.Quantity);
            var order = new Order
            {
                UserId = userId,
                TotalAmount = total,
                Status = OrderStatus.Pending,
                PaymentStatus = PaymentStatus.NotPaid,
                CreatedAt = DateTime.UtcNow
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var (productId, quantity, price) in lineDetails)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = productId,
                    Quantity = quantity,
                    PriceAtPurchase = price
                });
            }

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return OrderPlaceResult.Ok(order.Id, total, order.CreatedAt);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Order?> GetByIdForUserWithItemsAndProductsAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);
    }

    public async Task<IReadOnlyList<Order>> ListForUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> TrySetStripeCheckoutSessionIdAsync(
        int orderId,
        int userId,
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(
                o => o.Id == orderId
                    && o.UserId == userId
                    && o.Status == OrderStatus.Pending
                    && o.PaymentStatus != PaymentStatus.Succeeded,
                cancellationToken);

        if (order == null)
            return false;

        order.StripeCheckoutSessionId = sessionId;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryMarkPaidByStripeSessionAsync(
        string stripeCheckoutSessionId,
        string? paymentIntentId,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.StripeCheckoutSessionId == stripeCheckoutSessionId, cancellationToken);

        if (order == null)
            return false;

        if (order.Status == OrderStatus.Paid)
            return true;

        if (order.Status != OrderStatus.Pending)
            return false;

        order.Status = OrderStatus.Paid;
        order.PaymentStatus = PaymentStatus.Succeeded;
        order.PaidAt = DateTime.UtcNow;
        order.LastPaymentError = null;
        if (!string.IsNullOrEmpty(paymentIntentId))
            order.StripePaymentIntentId = paymentIntentId;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> TryMarkPaymentFailedByStripeSessionAsync(
        string stripeCheckoutSessionId,
        string? lastPaymentError,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.StripeCheckoutSessionId == stripeCheckoutSessionId, cancellationToken);

        if (order == null)
            return false;

        return await TryApplyPaymentFailedAsync(order, lastPaymentError, cancellationToken);
    }

    public async Task<bool> TryMarkPaymentFailedByOrderIdAsync(
        int orderId,
        string? lastPaymentError,
        CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order == null)
            return false;

        return await TryApplyPaymentFailedAsync(order, lastPaymentError, cancellationToken);
    }

    private async Task<bool> TryApplyPaymentFailedAsync(Order order, string? lastPaymentError, CancellationToken cancellationToken)
    {
        if (order.Status != OrderStatus.Pending)
            return false;

        if (order.PaymentStatus == PaymentStatus.Succeeded)
            return false;

        if (order.PaymentStatus == PaymentStatus.Refunded || order.PaymentStatus == PaymentStatus.Cancelled)
            return false;

        var msg = TruncateLastPaymentError(lastPaymentError);

        if (order.PaymentStatus == PaymentStatus.Failed)
        {
            if (!string.IsNullOrEmpty(msg))
                order.LastPaymentError = msg;
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }

        order.PaymentStatus = PaymentStatus.Failed;
        order.LastPaymentError = msg;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string? TruncateLastPaymentError(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return null;
        s = s.Trim();
        const int max = 2000;
        return s.Length <= max ? s : s[..max];
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> GetAllPagedAsync(
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders.AsNoTracking().OrderByDescending(o => o.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<Order?> GetByIdWithItemsAndProductsAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .AsNoTracking()
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public async Task<(IReadOnlyList<Order> Items, int TotalCount)> ListForUserPagedAsync(
        int userId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Orders
            .AsNoTracking()
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Include(o => o.OrderItems)
            .ThenInclude(i => i.Product)
            .ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<Order?> GetByIdTrackedAsync(int orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

   
    public async Task<OrderCancelResult> TryCancelOrderByUserAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            // Phải tìm đúng đơn của User đó
        var order = await _context.Orders
            .Include(o => o.OrderItems)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);

        if (order == null)
            return OrderCancelResult.Fail(OrderCancelFailure.NotFound);

        // Logic User: Chỉ được hủy nếu chưa hoàn thành và chưa bị hủy trước đó
        if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.ReturnRequested || order.Status == OrderStatus.Shipping)
            return OrderCancelResult.Fail(OrderCancelFailure.NotCancellable);

        // Thực hiện logic hủy & hoàn kho
        await ExecuteInternalCancelLogicAsync(order, cancellationToken);

        await tx.CommitAsync(cancellationToken);

           return CreateCancelResult(order);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<OrderCancelResult> TryCancelOrderByAdminAsync(int orderId, CancellationToken ct)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            if (order == null)
                return OrderCancelResult.Fail(OrderCancelFailure.NotFound);

            // Admin có thể hủy hầu hết các trạng thái trừ đơn đã hủy rồi
              if (order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.ReturnRequested || order.Status == OrderStatus.Shipping)
                  return OrderCancelResult.Fail(OrderCancelFailure.NotCancellable);
            // Thực hiện logic hủy & hoàn kho
            await ExecuteInternalCancelLogicAsync(order, ct);

            await tx.CommitAsync(ct);

            return CreateCancelResult(order);
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }
    public async Task<OrderCancelResult> TryProcessReturnByAdminAsync(int orderId, CancellationToken ct)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(ct);
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId, ct);

            // Chỉ xử lý nếu đơn đang ở trạng thái Yêu cầu trả hàng
            if (order == null || order.Status != OrderStatus.ReturnRequested)
                return OrderCancelResult.Fail(OrderCancelFailure.NotCancellable);

            await ExecuteInternalCancelLogicAsync(order, ct);

            await tx.CommitAsync(ct);

            return CreateCancelResult(order); 
        }
        catch { await tx.RollbackAsync(ct); throw; }
    }

    public async Task<OrderReturnRequestResult> TryRequestReturnByUserAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);

        if (order == null)
            return OrderReturnRequestResult.Fail(OrderReturnRequestFailure.NotFound);

        if (order.Status == OrderStatus.ReturnRequested)
            return OrderReturnRequestResult.Fail(OrderReturnRequestFailure.AlreadyRequested);

        if (order.Status != OrderStatus.Completed || order.PaymentStatus != PaymentStatus.Succeeded)
            return OrderReturnRequestResult.Fail(OrderReturnRequestFailure.NotEligible);

        order.Status = OrderStatus.ReturnRequested;
        await _context.SaveChangesAsync(cancellationToken);
        
        await tx.CommitAsync(cancellationToken);
        return OrderReturnRequestResult.Ok();
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
    public async Task<bool> TryUpdateStatusByAdminAsync(int orderId, OrderStatus newStatus, CancellationToken ct)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order == null) return false;

        // chỉ cho phép đổi nếu Đã thanh toán (Succeeded) (Paid)
        // Và không cho phép đổi ngược lại trạng thái thấp hơn (Ví dụ: từ Completed về Shipping)
        if (order.PaymentStatus != PaymentStatus.Succeeded) return false;
        
        // Logic kiểm tra thứ tự trạng thái (tùy chọn)
        if (newStatus <= order.Status) return false; 

        order.Status = newStatus;
        // Nếu là Completed, có thể cập nhật thêm ngày hoàn thành
        // if (newStatus == OrderStatus.Completed) order.CompletedAt = DateTime.UtcNow;

        return await _context.SaveChangesAsync(ct) > 0;
    }

    private async Task RevertStockAsync(IEnumerable<OrderItem> items, CancellationToken ct)
    {
        foreach (var item in items)
        {
            await _context.Products
                .Where(p => p.Id == item.ProductId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Stock, p => p.Stock + item.Quantity), ct);
        }
    
    }
    private async Task ExecuteInternalCancelLogicAsync(Order order, CancellationToken ct)
    {
        // 1. Hoàn lại kho hàng
        await RevertStockAsync(order.OrderItems, ct);

        // 2. Cập nhật PaymentStatus dựa trên trạng thái hiện tại
        if (order.PaymentStatus == PaymentStatus.Succeeded)
        {
            // Nếu đã trả tiền rồi thì đánh dấu là sẽ/đã hoàn tiền
            order.PaymentStatus = PaymentStatus.Refunded;
        }
        else
        {
            // Nếu chưa trả tiền hoặc lỗi thì đánh dấu hủy thanh toán
            order.PaymentStatus = PaymentStatus.Cancelled;
        }

        // 3. Đổi trạng thái đơn hàng
        order.Status = OrderStatus.Cancelled;

        await _context.SaveChangesAsync(ct);
    }
    private OrderCancelResult CreateCancelResult(Order order) =>
        OrderCancelResult.Ok(order.Id, order.UserId, order.TotalAmount, order.CreatedAt, order.PaidAt, order.PaymentStatus);
}
