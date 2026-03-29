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
            var paymentExpiresAt = DateTime.UtcNow.AddDays(1);
            var order = new Order
            {
                UserId = userId,
                TotalAmount = total,
                Status = OrderStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                PaymentExpiresAt = paymentExpiresAt
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

            return OrderPlaceResult.Ok(order.Id, total, order.CreatedAt, paymentExpiresAt);
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
                o => o.Id == orderId && o.UserId == userId && o.Status == OrderStatus.Pending,
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
        order.PaidAt = DateTime.UtcNow;
        if (!string.IsNullOrEmpty(paymentIntentId))
            order.StripePaymentIntentId = paymentIntentId;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> CancelExpiredPendingOrdersAndRestockAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var expiredIds = await _context.Orders
            .AsNoTracking()
            .Where(o =>
                o.Status == OrderStatus.Pending
                && o.PaymentExpiresAt != null
                && o.PaymentExpiresAt < now)
            .Select(o => o.Id)
            .ToListAsync(cancellationToken);

        var cancelled = 0;
        foreach (var orderId in expiredIds)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(
                        o => o.Id == orderId && o.Status == OrderStatus.Pending,
                        cancellationToken);

                if (order == null || order.PaymentExpiresAt == null || order.PaymentExpiresAt >= now)
                {
                    await tx.RollbackAsync(cancellationToken);
                    continue;
                }

                var restockOk = true;
                foreach (var line in order.OrderItems)
                {
                    // Restock even if the product was soft-deleted after the order was placed.
                    var rows = await _context.Products
                        .Where(p => p.Id == line.ProductId)
                        .ExecuteUpdateAsync(
                            s => s.SetProperty(p => p.Stock, p => p.Stock + line.Quantity),
                            cancellationToken);

                    if (rows == 0)
                    {
                        restockOk = false;
                        break;
                    }
                }

                if (!restockOk)
                {
                    await tx.RollbackAsync(cancellationToken);
                    continue;
                }

                order.Status = OrderStatus.Cancelled;
                await _context.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);
                cancelled++;
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        }

        return cancelled;
    }
}
