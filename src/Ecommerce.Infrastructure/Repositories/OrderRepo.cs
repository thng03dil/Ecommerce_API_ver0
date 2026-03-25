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
}
