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
        if (!string.IsNullOrEmpty(paymentIntentId))
            order.StripePaymentIntentId = paymentIntentId;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
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

    public async Task<bool> TryUpdateStatusAsync(int orderId, OrderStatus newStatus, CancellationToken cancellationToken = default)
    {
        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order == null)
            return false;

        order.Status = newStatus;
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<OrderCancelResult> TryCancelPendingOrderForUserAsync(
        int orderId,
        int userId,
        CancellationToken cancellationToken = default)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.Id == orderId && o.UserId == userId, cancellationToken);

            if (order == null)
            {
                await tx.RollbackAsync(cancellationToken);
                return OrderCancelResult.Fail(OrderCancelFailure.NotFound);
            }

            if (order.Status != OrderStatus.Pending || order.PaymentStatus == PaymentStatus.Succeeded)
            {
                await tx.RollbackAsync(cancellationToken);
                return OrderCancelResult.Fail(OrderCancelFailure.NotCancellable);
            }

            foreach (var item in order.OrderItems)
            {
                await _context.Products
                    .Where(p => p.Id == item.ProductId)
                    .ExecuteUpdateAsync(
                        s => s.SetProperty(p => p.Stock, p => p.Stock + item.Quantity),
                        cancellationToken);
            }

            order.Status = OrderStatus.Cancelled;
            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return OrderCancelResult.Ok(
                order.Id,
                order.UserId,
                order.TotalAmount,
                order.CreatedAt,
                order.PaidAt,
                order.PaymentStatus);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }
}
