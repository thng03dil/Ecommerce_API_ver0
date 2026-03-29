using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.DTOs.Common;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Implementations;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Ecommerce.UnitTests;

public class AuthServiceTests
{
    private readonly Mock<IUserRepo> _userRepo = new();
    private readonly Mock<IRoleRepo> _roleRepo = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<IDeviceService> _deviceService = new();
    private readonly Mock<ISecurityFingerprintHelper> _fingerprint = new();
    private readonly Mock<ITokenBlacklistService> _tokenBlacklist = new();
    private readonly Mock<IUserSessionInvalidationService> _sessionInvalidation = new();
    private readonly Mock<IRolePermissionService> _rolePermissionService = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(
            _userRepo.Object,
            _roleRepo.Object,
            _jwtService.Object,
            TestDataMother.CreateJwtOptions(),
            _passwordHasher.Object,
            _cacheService.Object,
            _deviceService.Object,
            _fingerprint.Object,
            _tokenBlacklist.Object,
            _sessionInvalidation.Object,
            _rolePermissionService.Object);
    }

    // ──────────────────────────────────────────────────────
    // Register
    // ──────────────────────────────────────────────────────

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ShouldThrowConflictException()
    {
        var dto = TestDataMother.CreateRegisterDto();
        _userRepo.Setup(x => x.GetByEmailAsync(dto.Email))
                 .ReturnsAsync(new User { Id = 1, Email = dto.Email });

        Func<Task> act = () => _sut.RegisterAsync(dto);

        await act.Should().ThrowAsync<ConflictException>();
        _userRepo.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_MissingDefaultRole_ShouldThrowException()
    {
        var dto = TestDataMother.CreateRegisterDto();
        _userRepo.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _roleRepo.Setup(x => x.GetByNameRoleAsync("User")).ReturnsAsync((Role?)null);

        Func<Task> act = () => _sut.RegisterAsync(dto);

        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*default 'User' role is missing*");
    }

    [Fact]
    public async Task RegisterAsync_Valid_ShouldCallAddAsync()
    {
        var dto = TestDataMother.CreateRegisterDto("new@x.com", "password1");
        _userRepo.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _roleRepo.Setup(x => x.GetByNameRoleAsync("User")).ReturnsAsync(new Role { Id = 2, Name = "User" });
        _passwordHasher.Setup(x => x.Hash(dto.Password)).Returns("hashed!");

        await _sut.RegisterAsync(dto);

        _passwordHasher.Verify(x => x.Hash(dto.Password), Times.Once);
        _userRepo.Verify(
            x => x.AddAsync(It.Is<User>(u => u.Email == dto.Email && u.PasswordHash == "hashed!" && u.RoleId == 2)),
            Times.Once);
    }

    // ──────────────────────────────────────────────────────
    // Login
    // ──────────────────────────────────────────────────────

    [Fact]
    public async Task LoginAsync_UserNotFound_ShouldThrowUnauthorizedException()
    {
        var dto = TestDataMother.CreateLoginDto();
        _userRepo.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _cacheService.Setup(x => x.GetAsync<int?>(It.IsAny<string>())).ReturnsAsync((int?)null);

        Func<Task> act = () => _sut.LoginAsync(dto);

        await act.Should().ThrowAsync<UnauthorizedException>()
                 .WithMessage("Invalid email or password");
        _cacheService.Verify(x => x.SetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldThrowUnauthorizedException()
    {
        var dto = TestDataMother.CreateLoginDto();
        var user = TestDataMother.CreateUser(70302, dto.Email, "hashed-password");
        _userRepo.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync(user);
        _passwordHasher.Setup(x => x.Verify(dto.Password, user.PasswordHash)).Returns(false);
        _cacheService.Setup(x => x.GetAsync<int?>(It.IsAny<string>())).ReturnsAsync(2);

        Func<Task> act = () => _sut.LoginAsync(dto);

        await act.Should().ThrowAsync<UnauthorizedException>()
                 .WithMessage("Invalid email or password");
        _cacheService.Verify(x => x.SetAsync(It.IsAny<string>(), 3, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Fact]
    public async Task LoginAsync_TooManyFailures_ShouldThrowTooManyRequestsException()
    {
        var dto = TestDataMother.CreateLoginDto("lock@test.com", "bad");
        _userRepo.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _cacheService.Setup(x => x.GetAsync<int?>(It.IsAny<string>())).ReturnsAsync(5);

        var act = async () => await _sut.LoginAsync(dto);

        (await act.Should().ThrowAsync<TooManyRequestsException>())
            .Which.Message.Should().Be("Too many login attempts. Try again later.");
    }

    [Fact]
    public async Task LoginAsync_MissingDeviceId_ShouldThrowUnauthorizedException()
    {
        // Both body and header empty
        var dto = TestDataMother.CreateLoginDto(deviceId: null);
        _deviceService.Setup(x => x.GetDeviceId()).Returns("");

        var act = async () => await _sut.LoginAsync(dto);

        (await act.Should().ThrowAsync<UnauthorizedException>())
            .WithMessage("*DeviceId is required*");
        _userRepo.Verify(x => x.GetByEmailAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_Valid_ShouldReturnAuthResponseAndStoreRtOnUser()
    {
        var userId = 70306;
        var dto = TestDataMother.CreateLoginDto("ok@test.com", "pwd", deviceId: "body-device");
        var userForCred = TestDataMother.CreateUser(userId, dto.Email, "hash");
        var userForUpdate = TestDataMother.CreateUser(userId, dto.Email, "hash", sessionVersion: 3);

        _userRepo.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync(userForCred);
        _passwordHasher.Setup(x => x.Verify(dto.Password, userForCred.PasswordHash)).Returns(true);
        _fingerprint.Setup(x => x.ComputeFingerprint("body-device")).Returns("fp-device");
        _userRepo.Setup(x => x.GetByIdForUpdateAsync(userId)).ReturnsAsync(userForUpdate);
        _jwtService.Setup(x => x.GenerateRefreshToken()).Returns("plain-rt");
        _jwtService.Setup(x => x.HashToken("plain-rt")).Returns("hash-rt");
        _jwtService
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>(), It.IsAny<Guid>(), It.IsAny<int>(), "fp-device"))
            .Returns("access-token");

        var result = await _sut.LoginAsync(dto);

        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("plain-rt");

        // RT stored on user (not a separate repo)
        userForUpdate.RefreshTokenHash.Should().Be("hash-rt");
        userForUpdate.RefreshTokenExpiresAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddDays(7), TimeSpan.FromSeconds(5));
        userForUpdate.SessionVersion.Should().Be(4); // 3 + 1

        _userRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _cacheService.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            It.IsAny<object>(),
            It.IsAny<TimeSpan>()),
            Times.AtLeastOnce);
    }

    // ──────────────────────────────────────────────────────
    // Refresh
    // ──────────────────────────────────────────────────────

    [Fact]
    public async Task RefreshTokenAsync_InvalidUserIdClaim_ShouldThrowUnauthorizedException()
    {
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sid", Guid.NewGuid().ToString()),
            new Claim("sv", "1"),
            new Claim("fp", "fp")
        }));
        _jwtService.Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns(principal);
        var req = TestDataMother.CreateRefreshRequest();

        var act = async () => await _sut.RefreshTokenAsync(req);

        (await act.Should().ThrowAsync<UnauthorizedException>())
            .WithMessage("Invalid token");
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidSvClaim_ShouldThrowUnauthorizedException()
    {
        var userId = 70308;
        var sid = Guid.NewGuid().ToString();
        var principal = TestDataMother.CreateClaimsPrincipal(userId, sid, "not-int", "fp");
        _jwtService.Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns(principal);
        var req = TestDataMother.CreateRefreshRequest();

        var act = async () => await _sut.RefreshTokenAsync(req);

        (await act.Should().ThrowAsync<UnauthorizedException>())
            .WithMessage("Invalid token");
    }

    [Fact]
    public async Task RefreshTokenAsync_FingerprintMismatch_ShouldThrow()
    {
        var userId = 70309;
        var sid = Guid.NewGuid();
        var principal = TestDataMother.CreateClaimsPrincipal(userId, sid.ToString(), "3", "fp");
        _jwtService.Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns(principal);
        _deviceService.Setup(x => x.GetDeviceId()).Returns("other-device");
        _fingerprint.Setup(x => x.ComputeFingerprint("other-device")).Returns("different-fp");

        var user = TestDataMother.CreateUser(userId, sessionVersion: 3, currentSessionId: sid,
            refreshTokenHash: "rt-h", refreshTokenExpiresAtUtc: DateTime.UtcNow.AddDays(5));
        user.LastFingerprintHash = "original-fp"; // stored fp from login
        _userRepo.Setup(x => x.GetByIdForUpdateAsync(userId)).ReturnsAsync(user);

        var req = TestDataMother.CreateRefreshRequest("at", "rt-plain");

        var act = async () => await _sut.RefreshTokenAsync(req);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("*fingerprint mismatch*");
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidRtHash_ShouldThrow()
    {
        var userId = 70310;
        var sid = Guid.NewGuid();
        var principal = TestDataMother.CreateClaimsPrincipal(userId, sid.ToString(), "2", "fp");
        _jwtService.Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns(principal);
        _jwtService.Setup(x => x.GetAccessTokenRemainingLifetime(It.IsAny<string>())).Returns((TimeSpan?)null);
        _deviceService.Setup(x => x.GetDeviceId()).Returns("dev");
        _fingerprint.Setup(x => x.ComputeFingerprint("dev")).Returns("fp");
        _jwtService.Setup(x => x.HashToken("wrong-rt")).Returns("wrong-hash");

        var user = TestDataMother.CreateUser(userId, sessionVersion: 2, currentSessionId: sid,
            refreshTokenHash: "correct-hash", refreshTokenExpiresAtUtc: DateTime.UtcNow.AddDays(5));
        user.LastFingerprintHash = "fp";
        _userRepo.Setup(x => x.GetByIdForUpdateAsync(userId)).ReturnsAsync(user);

        var req = TestDataMother.CreateRefreshRequest("at", "wrong-rt");

        var act = async () => await _sut.RefreshTokenAsync(req);

        await act.Should().ThrowAsync<UnauthorizedException>()
            .WithMessage("Invalid refresh token");
    }

    [Fact]
    public async Task RefreshTokenAsync_Valid_ShouldReturnNewAccessTokenAndIncrementSv()
    {
        var userId = 70311;
        var sid = Guid.NewGuid();
        var principal = TestDataMother.CreateClaimsPrincipal(userId, sid.ToString(), "4", "fp");
        _jwtService.Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns(principal);
        _jwtService.Setup(x => x.GetAccessTokenRemainingLifetime(It.IsAny<string>())).Returns((TimeSpan?)null);
        _deviceService.Setup(x => x.GetDeviceId()).Returns("dev");
        _fingerprint.Setup(x => x.ComputeFingerprint("dev")).Returns("fp");
        _jwtService.Setup(x => x.HashToken("old-rt")).Returns("rt-h");

        var user = TestDataMother.CreateUser(userId, sessionVersion: 4, currentSessionId: sid,
            refreshTokenHash: "rt-h", refreshTokenExpiresAtUtc: DateTime.UtcNow.AddDays(5));
        user.LastFingerprintHash = "fp";
        _userRepo.Setup(x => x.GetByIdForUpdateAsync(userId)).ReturnsAsync(user);
        _jwtService.Setup(x => x.GenerateAccessToken(It.IsAny<User>(), sid, 5, "fp")).Returns("new-access");

        var req = TestDataMother.CreateRefreshRequest("expired-at", "old-rt");

        var result = await _sut.RefreshTokenAsync(req);

        result.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("old-rt"); // RT unchanged
        user.SessionVersion.Should().Be(5);        // sv incremented
        _userRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    // ──────────────────────────────────────────────────────
    // Logout
    // ──────────────────────────────────────────────────────

    [Fact]
    public async Task LogoutAsync_WithValidAccessToken_ShouldBlacklistWhenRemainingPositive()
    {
        var userId = 70312;
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, "jid-1")
        }));
        _jwtService.Setup(x => x.GetPrincipalFromExpiredToken("at")).Returns(principal);
        _jwtService.Setup(x => x.GetAccessTokenRemainingLifetime("at")).Returns(TimeSpan.FromMinutes(3));
        _jwtService.Setup(x => x.HashToken("jid-1")).Returns("jti-hash");

        await _sut.LogoutAsync(userId, "at");

        _tokenBlacklist.Verify(
            x => x.BlacklistAsync("jti-hash", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _sessionInvalidation.Verify(x => x.InvalidateAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_ShouldCallInvalidateAsync()
    {
        var userId = 70313;

        await _sut.LogoutAsync(userId, string.Empty);

        _sessionInvalidation.Verify(x => x.InvalidateAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _tokenBlacklist.Verify(
            x => x.BlacklistAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ──────────────────────────────────────────────────────
    // HasPermission
    // ──────────────────────────────────────────────────────

    [Fact]
    public async Task HasPermissionAsync_WhenRoleIsAdmin_ReturnsTrue_WithoutCallingRolePermissionService()
    {
        _userRepo.Setup(x => x.GetRoleContextForAuthAsync(10, It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, "Admin"));

        var ok = await _sut.HasPermissionAsync(10, "any.permission");

        ok.Should().BeTrue();
        _rolePermissionService.Verify(
            x => x.RoleHasPermissionAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HasPermissionAsync_WhenUserHasPermission_ReturnsTrue()
    {
        _userRepo.Setup(x => x.GetRoleContextForAuthAsync(20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((3, "User"));
        _rolePermissionService
            .Setup(x => x.RoleHasPermissionAsync(3, "product.read", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var ok = await _sut.HasPermissionAsync(20, "product.read");

        ok.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_WhenUserLacksPermission_ReturnsFalse()
    {
        _userRepo.Setup(x => x.GetRoleContextForAuthAsync(21, It.IsAny<CancellationToken>()))
            .ReturnsAsync((3, "User"));
        _rolePermissionService
            .Setup(x => x.RoleHasPermissionAsync(3, "product.delete", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var ok = await _sut.HasPermissionAsync(21, "product.delete");

        ok.Should().BeFalse();
    }
}
