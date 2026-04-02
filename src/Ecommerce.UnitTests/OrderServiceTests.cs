using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.DTOs.OrderDtos;
using Ecommerce.Application.Exceptions;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common;
using Ecommerce.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace Ecommerce.UnitTests;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepo> _orderRepo = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<IOrderPaymentService> _paymentService = new();
    private readonly IOrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(_orderRepo.Object, _cacheService.Object, _paymentService.Object);
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenUserIdInvalid_ShouldReturnErrorWithoutCallingRepo()
    {
        var dto = new CreateOrderDto { Items = [new OrderItemRequestDto { ProductId = 1, Quantity = 1 }] };

        var result = await _sut.PlaceOrderAsync(0, dto);

        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(401);
        _orderRepo.Verify(
            x => x.PlaceOrderAsync(It.IsAny<int>(), It.IsAny<IReadOnlyList<OrderLineInput>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenRepoFails_ShouldNotBumpCache()
    {
        var dto = new CreateOrderDto { Items = [new OrderItemRequestDto { ProductId = 1, Quantity = 2 }] };
        _orderRepo
            .Setup(x => x.PlaceOrderAsync(5, It.IsAny<IReadOnlyList<OrderLineInput>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderPlaceResult.Fail("Insufficient stock for product 1."));

        var result = await _sut.PlaceOrderAsync(5, dto);

        result.Success.Should().BeFalse();
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.CategoryVersionKey()), Times.Never);
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenRepoSucceeds_ShouldBumpCategoryVersionOnly()
    {
        var created = new DateTime(2026, 3, 24, 12, 0, 0, DateTimeKind.Utc);
        var dto = new CreateOrderDto { Items = [new OrderItemRequestDto { ProductId = 10, Quantity = 1 }] };
        _orderRepo
            .Setup(x => x.PlaceOrderAsync(3, It.IsAny<IReadOnlyList<OrderLineInput>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderPlaceResult.Ok(99, 25.5m, created));

        var placed = new Order
        {
            Id = 99,
            UserId = 3,
            TotalAmount = 25.5m,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.NotPaid,
            CreatedAt = created,
            OrderItems = new List<OrderItem>
            {
                new()
                {
                    ProductId = 10,
                    Quantity = 1,
                    PriceAtPurchase = 25.5m,
                    Product = new Product { Name = "TestProduct" }
                }
            }
        };
        _orderRepo
            .Setup(x => x.GetByIdForUserWithItemsAndProductsAsync(99, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(placed);

        var result = await _sut.PlaceOrderAsync(3, dto);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(99);
        result.Data.TotalAmount.Should().Be(25.5m);
        result.Data.CreatedAt.Should().Be(created);
        result.Data.PaymentStatus.Should().Be(PaymentStatus.NotPaid);
        result.Data.ItemCount.Should().Be(1);
        result.Data.Items.Should().ContainSingle(i => i.ProductName == "TestProduct" && i.Quantity == 1);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.CategoryVersionKey()), Times.Once);
    }

    [Fact]
    public async Task CancelPendingOrderAsync_WhenUserIdInvalid_ShouldThrowUnauthorized()
    {
        var act = async () => await _sut.CancelPendingOrderAsync(0, 1);

        await act.Should().ThrowAsync<UnauthorizedException>();
        _orderRepo.Verify(
            x => x.GetByIdForUserWithItemsAndProductsAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _orderRepo.Verify(
            x => x.TryCancelOrderByUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task CancelPendingOrderAsync_WhenNotFound_ShouldThrowNotFound()
    {
        _orderRepo
            .Setup(x => x.GetByIdForUserWithItemsAndProductsAsync(7, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var act = async () => await _sut.CancelPendingOrderAsync(3, 7);

        await act.Should().ThrowAsync<NotFoundException>();
        _orderRepo.Verify(
            x => x.TryCancelOrderByUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()),
            Times.Never);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.CategoryVersionKey()), Times.Never);
    }

    [Fact]
    public async Task CancelPendingOrderAsync_WhenNotCancellable_ShouldThrowConflict()
    {
        var existing = new Order
        {
            Id = 7,
            UserId = 3,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.NotPaid
        };
        _orderRepo
            .Setup(x => x.GetByIdForUserWithItemsAndProductsAsync(7, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        _orderRepo
            .Setup(x => x.TryCancelOrderByUserAsync(7, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderCancelResult.Fail(OrderCancelFailure.NotCancellable));

        var act = async () => await _sut.CancelPendingOrderAsync(3, 7);

        await act.Should().ThrowAsync<ConflictException>();
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.CategoryVersionKey()), Times.Never);
    }

    [Fact]
    public async Task CancelPendingOrderAsync_WhenOk_ShouldBumpCacheAndReturnCancelled()
    {
        var created = new DateTime(2026, 3, 24, 12, 0, 0, DateTimeKind.Utc);
        var beforeCancel = new Order
        {
            Id = 7,
            UserId = 3,
            TotalAmount = 42m,
            CreatedAt = created,
            Status = OrderStatus.Pending,
            PaymentStatus = PaymentStatus.NotPaid,
            OrderItems = new List<OrderItem>()
        };
        var cancelled = new Order
        {
            Id = 7,
            UserId = 3,
            TotalAmount = 42m,
            CreatedAt = created,
            Status = OrderStatus.Cancelled,
            PaymentStatus = PaymentStatus.Cancelled,
            OrderItems = new List<OrderItem>()
        };
        _orderRepo
            .SetupSequence(x => x.GetByIdForUserWithItemsAndProductsAsync(7, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(beforeCancel)
            .ReturnsAsync(cancelled);
        _orderRepo
            .Setup(x => x.TryCancelOrderByUserAsync(7, 3, It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                OrderCancelResult.Ok(7, 3, 42m, created, null, PaymentStatus.Cancelled));

        var dto = await _sut.CancelPendingOrderAsync(3, 7);

        dto.Id.Should().Be(7);
        dto.Status.Should().Be(OrderStatus.Cancelled);
        dto.PaymentStatus.Should().Be(PaymentStatus.Cancelled);
        dto.TotalAmount.Should().Be(42m);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.CategoryVersionKey()), Times.Once);
    }
}
