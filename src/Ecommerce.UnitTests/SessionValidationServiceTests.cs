using Ecommerce.Application.Common.Auth;
using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common;
using Ecommerce.Domain.Interfaces;
using Ecommerce.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace Ecommerce.UnitTests;

public class SessionValidationServiceTests
{
    private readonly Mock<ICacheService> _cache;
    private readonly Mock<IUserRepo> _userRepo;
    private readonly SessionValidationService _sut;

    public SessionValidationServiceTests()
    {
        _cache = new Mock<ICacheService>(MockBehavior.Loose);
        _userRepo = new Mock<IUserRepo>(MockBehavior.Loose);
        _sut = new SessionValidationService(_cache.Object, _userRepo.Object);
    }

    [Fact]
    public async Task EnsureAccessTokenSessionValidAsync_InvalidSid_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = 50101;
        var sid = "not-a-guid";
        var sv = "1";
        var fp = "fp";

        // Act
        var act = async () => await _sut.EnsureAccessTokenSessionValidAsync(userId, sid, sv, fp, fp);

        // Assert
        (await act.Should().ThrowAsync<UnauthorizedException>())
            .WithMessage("Invalid token session");
    }

    [Fact]
    public async Task EnsureAccessTokenSessionValidAsync_InvalidSv_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = 50102;
        var sid = Guid.NewGuid().ToString();
        var sv = "x";
        var fp = "fp";

        // Act
        var act = async () => await _sut.EnsureAccessTokenSessionValidAsync(userId, sid, sv, fp, fp);

        // Assert
        (await act.Should().ThrowAsync<UnauthorizedException>())
            .WithMessage("Invalid token session");
    }

    [Fact]
    public async Task EnsureAccessTokenSessionValidAsync_EmptyFingerprintClaim_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = 50103;
        var sid = Guid.NewGuid().ToString();
        var sv = "1";
        string? fp = null;

        // Act
        var act = async () => await _sut.EnsureAccessTokenSessionValidAsync(userId, sid, sv, fp!, "any");

        // Assert
        (await act.Should().ThrowAsync<UnauthorizedException>())
            .WithMessage("Invalid token session");
    }

    [Fact]
    public async Task EnsureAccessTokenSessionValidAsync_FingerprintMismatch_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = 50104;
        var sid = Guid.NewGuid().ToString();
        var sv = "1";
        var fpClaim = "claim-fp";

        // Act
        var act = async () =>
            await _sut.EnsureAccessTokenSessionValidAsync(userId, sid, sv, fpClaim, "current-fp");

        // Assert
        (await act.Should().ThrowAsync<UnauthorizedException>())
            .WithMessage("Invalid session (fingerprint mismatch)");
    }

    [Fact]
    public async Task EnsureAccessTokenSessionValidAsync_CacheHit_Valid_ShouldComplete()
    {
        // Arrange
        var userId = 50105;
        var sid = Guid.NewGuid();
        var sv = 3;
        var fp = "fp-match";
        var state = TestDataMother.CreateUserSessionState(sid, sv, fp);
        _cache
            .Setup(c => c.GetAsync<UserSessionState>(It.IsAny<string>()))
            .ReturnsAsync(state);

        // Act
        await _sut.EnsureAccessTokenSessionValidAsync(userId, sid.ToString(), sv.ToString(), fp, fp);

        // Assert
        _userRepo.Verify(x => x.GetUserAuthStateAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task EnsureAccessTokenSessionValidAsync_CacheHit_StaleSessionId_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = 50106;
        var sid = Guid.NewGuid();
        var sv = 2;
        var fp = "fp";
        var state = TestDataMother.CreateUserSessionState(Guid.NewGuid(), sv, fp);
        _cache.Setup(c => c.GetAsync<UserSessionState>(It.IsAny<string>())).ReturnsAsync(state);

        // Act
        var act = async () =>
            await _sut.EnsureAccessTokenSessionValidAsync(userId, sid.ToString(), sv.ToString(), fp, fp);

        // Assert
        (await act.Should().ThrowAsync<UnauthorizedException>())
            .WithMessage("Invalid session");
    }

    [Fact]
    public async Task EnsureAccessTokenSessionValidAsync_CacheHit_StaleSessionVersion_ShouldThrowUnauthorizedException()
    {
        // Arrange: jwt.sv=1, redis.sv=99 → 1 < 99 → outdated AT after refresh
        var userId = 50107;
        var sid = Guid.NewGuid();
        var fp = "fp";
        var state = TestDataMother.CreateUserSessionState(sid, 99, fp);
        _cache.Setup(c => c.GetAsync<UserSessionState>(It.IsAny<string>())).ReturnsAsync(state);

        // Act
        var act = async () =>
            await _sut.EnsureAccessTokenSessionValidAsync(userId, sid.ToString(), "1", fp, fp);

        // Assert — new message for jwt.sv < redis.sv path
        (await act.Should().ThrowAsync<UnauthorizedException>())
            .WithMessage("Access token is outdated. Please refresh.");
    }

    [Fact]
    public async Task EnsureAccessTokenSessionValidAsync_CacheHit_StaleFingerprint_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = 50108;
        var sid = Guid.NewGuid();
        var sv = 1;
        var state = TestDataMother.CreateUserSessionState(sid, sv, "other");
        _cache.Setup(c => c.GetAsync<UserSessionState>(It.IsAny<string>())).ReturnsAsync(state);

        // Act
        var act = async () =>
            await _sut.EnsureAccessTokenSessionValidAsync(userId, sid.ToString(), sv.ToString(), "expected", "expected");

        // Assert
        (await act.Should().ThrowAsync<UnauthorizedException>())
            .WithMessage("Invalid session");
    }

    [Fact]
    public async Task EnsureAccessTokenSessionValidAsync_CacheMiss_DbValid_ShouldComplete()
    {
        // Arrange
        var userId = 50109;
        var sid = Guid.NewGuid();
        var sv = 4;
        var fp = "fp-db";
        var db = TestDataMother.CreateUserAuthState(sv, sid, fp);
        _cache.Setup(c => c.GetAsync<UserSessionState>(It.IsAny<string>())).ReturnsAsync((UserSessionState?)null);
        _userRepo.Setup(x => x.GetUserAuthStateAsync(userId)).ReturnsAsync(db);

        // Act
        await _sut.EnsureAccessTokenSessionValidAsync(userId, sid.ToString(), sv.ToString(), fp, fp);

        // Assert — single key (no sv suffix)
        _cache.Verify(x => x.GetAsync<UserSessionState>(CacheKeyGenerator.AuthSession(userId)), Times.AtLeastOnce);
        _userRepo.Verify(x => x.GetUserAuthStateAsync(userId), Times.Once);
    }

    [Fact]
    public async Task EnsureAccessTokenSessionValidAsync_CacheMiss_DbNull_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = 50110;
        var sid = Guid.NewGuid();
        var sv = 1;
        var fp = "fp";
        _cache.Setup(c => c.GetAsync<UserSessionState>(It.IsAny<string>())).ReturnsAsync((UserSessionState?)null);
        _userRepo.Setup(x => x.GetUserAuthStateAsync(userId)).ReturnsAsync((UserAuthState?)null);

        // Act
        var act = async () =>
            await _sut.EnsureAccessTokenSessionValidAsync(userId, sid.ToString(), sv.ToString(), fp, fp);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task EnsureAccessTokenSessionValidAsync_CacheMiss_DbMismatch_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = 50111;
        var sid = Guid.NewGuid();
        var sv = 1;
        var fp = "fp";
        var db = TestDataMother.CreateUserAuthState(sv, Guid.NewGuid(), fp);
        _cache.Setup(c => c.GetAsync<UserSessionState>(It.IsAny<string>())).ReturnsAsync((UserSessionState?)null);
        _userRepo.Setup(x => x.GetUserAuthStateAsync(userId)).ReturnsAsync(db);

        // Act
        var act = async () =>
            await _sut.EnsureAccessTokenSessionValidAsync(userId, sid.ToString(), sv.ToString(), fp, fp);

        // Assert
        (await act.Should().ThrowAsync<UnauthorizedException>())
            .WithMessage("Invalid session");
    }
}
