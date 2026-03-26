using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.DTOs.OrderDtos;
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
    private readonly IOrderService _sut;

    public OrderServiceTests()
    {
        _sut = new OrderService(_orderRepo.Object, _cacheService.Object);
    }

    [Fact]
    public async Task PlaceOrderAsync_WhenUserIdInvalid_ShouldReturnErrorWithoutCallingRepo()
    {
        var dto = new CreateOrderDto { Items = [new OrderLineDto { ProductId = 1, Quantity = 1 }] };

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
        var dto = new CreateOrderDto { Items = [new OrderLineDto { ProductId = 1, Quantity = 2 }] };
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
        var dto = new CreateOrderDto { Items = [new OrderLineDto { ProductId = 10, Quantity = 1 }] };
        _orderRepo
            .Setup(x => x.PlaceOrderAsync(3, It.IsAny<IReadOnlyList<OrderLineInput>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(OrderPlaceResult.Ok(99, 25.5m, created));

        var result = await _sut.PlaceOrderAsync(3, dto);

        result.Success.Should().BeTrue();
        result.Data!.Id.Should().Be(99);
        result.Data.TotalAmount.Should().Be(25.5m);
        result.Data.CreatedAt.Should().Be(created);
        _cacheService.Verify(x => x.IncrementAsync(CacheKeyGenerator.CategoryVersionKey()), Times.Once);
    }
}
