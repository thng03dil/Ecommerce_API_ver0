using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Ecommerce.UnitTests;

public class UserSessionInvalidationServiceTests
{
    private readonly Mock<IRefreshTokenRepo> _refreshTokenRepo;
    private readonly Mock<IUserRepo> _userRepo;
    private readonly Mock<ICacheService> _cacheService;
    private readonly UserSessionInvalidationService _sut;

    public UserSessionInvalidationServiceTests()
    {
        _refreshTokenRepo = new Mock<IRefreshTokenRepo>(MockBehavior.Loose);
        _userRepo = new Mock<IUserRepo>(MockBehavior.Loose);
        _cacheService = new Mock<ICacheService>(MockBehavior.Loose);
        _sut = new UserSessionInvalidationService(_refreshTokenRepo.Object, _userRepo.Object, _cacheService.Object);
    }

    [Fact]
    public async Task InvalidateAsync_UserNotFound_ShouldRevokeWithoutSaveChanges()
    {
        // Arrange
        var userId = 60101;
        _userRepo.Setup(x => x.GetByIdForUpdateAsync(userId)).ReturnsAsync((User?)null);

        // Act
        await _sut.InvalidateAsync(userId);

        // Assert
        _refreshTokenRepo.Verify(x => x.RevokeAllForUserAsync(userId), Times.Once);
        _userRepo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task InvalidateAsync_UserExists_ShouldRevokeSaveAndRemoveCache()
    {
        // Arrange
        var userId = 60102;
        var oldSv = 7;
        var user = TestDataMother.CreateUser(userId, sessionVersion: oldSv, currentSessionId: Guid.NewGuid());
        _userRepo.Setup(x => x.GetByIdForUpdateAsync(userId)).ReturnsAsync(user);

        // Act
        await _sut.InvalidateAsync(userId);

        // Assert
        user.SessionVersion.Should().Be(oldSv + 1);
        _userRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _cacheService.Verify(
            x => x.RemoveByPrefixAsync(CacheKeyGenerator.AuthSessionUserPrefix(userId)),
            Times.Once);
    }

    [Fact]
    public async Task InvalidateAsync_UserExists_ShouldClearSessionFields()
    {
        // Arrange
        var userId = 60103;
        var user = TestDataMother.CreateUser(
            userId,
            sessionVersion: 2,
            currentSessionId: Guid.NewGuid());
        user.LastLoginIpHash = "ip";
        user.LastDeviceId = "dev";
        _userRepo.Setup(x => x.GetByIdForUpdateAsync(userId)).ReturnsAsync(user);

        // Act
        await _sut.InvalidateAsync(userId);

        // Assert
        user.CurrentSessionId.Should().BeNull();
        user.LastLoginIpHash.Should().BeNull();
        user.LastDeviceId.Should().BeNull();
    }
}
