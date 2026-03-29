using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Xunit;

namespace Ecommerce.UnitTests;

public class OrderRepoStripeAndExpiryTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task TryMarkPaidByStripeSessionAsync_Twice_IsIdempotent()
    {
        await using var ctx = CreateContext();
        ctx.Orders.Add(new Order
        {
            UserId = 1,
            TotalAmount = 10m,
            Status = OrderStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            StripeCheckoutSessionId = "cs_test_123"
        });
        await ctx.SaveChangesAsync();

        var repo = new OrderRepo(ctx);

        var first = await repo.TryMarkPaidByStripeSessionAsync("cs_test_123", "pi_test_1");
        var second = await repo.TryMarkPaidByStripeSessionAsync("cs_test_123", "pi_test_1");

        first.Should().BeTrue();
        second.Should().BeTrue();

        var order = await ctx.Orders.SingleAsync();
        order.Status.Should().Be(OrderStatus.Paid);
        order.PaidAt.Should().NotBeNull();
        order.StripePaymentIntentId.Should().Be("pi_test_1");
    }

    [Fact]
    public async Task TryMarkPaidByStripeSessionAsync_UnknownSession_ReturnsFalse()
    {
        await using var ctx = CreateContext();
        var repo = new OrderRepo(ctx);

        var ok = await repo.TryMarkPaidByStripeSessionAsync("cs_missing", null);

        ok.Should().BeFalse();
    }

    // CancelExpiredPendingOrdersAndRestockAsync uses ExecuteUpdateAsync (SQL Server); InMemory provider does not support it — verify against a real DB or integration test.
}
