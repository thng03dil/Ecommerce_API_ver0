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
    private readonly Mock<IUserRepo> _userRepo;
    private readonly Mock<ICacheService> _cacheService;
    private readonly UserSessionInvalidationService _sut;

    public UserSessionInvalidationServiceTests()
    {
        _userRepo = new Mock<IUserRepo>(MockBehavior.Loose);
        _cacheService = new Mock<ICacheService>(MockBehavior.Loose);
        _sut = new UserSessionInvalidationService(_userRepo.Object, _cacheService.Object);
    }

    [Fact]
    public async Task InvalidateAsync_UserNotFound_ShouldNotSaveChanges()
    {
        var userId = 60101;
        _userRepo.Setup(x => x.GetByIdForUpdateAsync(userId)).ReturnsAsync((User?)null);

        await _sut.InvalidateAsync(userId);

        _userRepo.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    [Fact]
    public async Task InvalidateAsync_UserExists_ShouldSaveAndRemoveCache()
    {
        var userId = 60102;
        var oldSv = 7;
        var user = TestDataMother.CreateUser(userId, sessionVersion: oldSv, currentSessionId: Guid.NewGuid());
        _userRepo.Setup(x => x.GetByIdForUpdateAsync(userId)).ReturnsAsync(user);

        await _sut.InvalidateAsync(userId);

        user.SessionVersion.Should().Be(oldSv + 1);
        _userRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _cacheService.Verify(x => x.RemoveAsync(CacheKeyGenerator.AuthSession(userId)), Times.Once);
    }

    [Fact]
    public async Task InvalidateAsync_UserExists_ShouldClearAllSessionFields()
    {
        var userId = 60103;
        var user = TestDataMother.CreateUser(userId, sessionVersion: 2, currentSessionId: Guid.NewGuid(),
            refreshTokenHash: "some-hash", refreshTokenExpiresAtUtc: DateTime.UtcNow.AddDays(3));
        user.LastDeviceId = "dev";
        user.LastFingerprintHash = "fp";
        _userRepo.Setup(x => x.GetByIdForUpdateAsync(userId)).ReturnsAsync(user);

        await _sut.InvalidateAsync(userId);

        user.CurrentSessionId.Should().BeNull();
        user.LastDeviceId.Should().BeNull();
        user.LastFingerprintHash.Should().BeNull();
        user.RefreshTokenHash.Should().BeNull();
        user.RefreshTokenExpiresAtUtc.Should().BeNull();
    }
}
