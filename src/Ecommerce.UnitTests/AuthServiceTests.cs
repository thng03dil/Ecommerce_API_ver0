using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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
    //mock depenqdencies for AuthService 
    private readonly Mock<IUserRepo> _userRepo = new();
    private readonly Mock<IRoleRepo> _roleRepo = new();
    private readonly Mock<IJwtService> _jwtService = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IRefreshTokenRepo> _refreshTokenRepo = new();
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<IDeviceService> _deviceService = new();
    private readonly Mock<ISecurityFingerprintHelper> _fingerprint = new();
    private readonly Mock<ISessionValidationService> _sessionValidation = new();
    private readonly Mock<ITokenBlacklistService> _tokenBlacklist = new();
    private readonly Mock<IUserSessionInvalidationService> _sessionInvalidation = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _sut = new AuthService(
            _userRepo.Object,
            _roleRepo.Object,
            _jwtService.Object,
            TestDataMother.CreateJwtOptions(),
            _passwordHasher.Object,
            _refreshTokenRepo.Object,
            _cacheService.Object,
            _deviceService.Object,
            _fingerprint.Object,
            _sessionValidation.Object,
            _tokenBlacklist.Object,
            _sessionInvalidation.Object);
    }

    [Fact]
    public async Task RegisterAsync_DuplicateEmail_ShouldThrowConflictException()
    {
        // Arrange
        var dto = TestDataMother.CreateRegisterDto();
        _userRepo.Setup(x => x.GetByEmailAsync(dto.Email))
                 .ReturnsAsync(new User { Id = 1, Email = dto.Email });

        // Act
        Func<Task> act = () => _sut.RegisterAsync(dto);

        // Assert
        await act.Should().ThrowAsync<ConflictException>() 
                 .WithMessage("CONFLICT_ERROR");

        _userRepo.Verify(x => x.AddAsync(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task RegisterAsync_MissingDefaultRole_ShouldThrowException()
    {
        // Arrange
        var dto = TestDataMother.CreateRegisterDto();
        _userRepo.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _roleRepo.Setup(x => x.GetByNameRoleAsync("User")).ReturnsAsync((Role?)null);

        // Act
        Func<Task> act = () => _sut.RegisterAsync(dto);

        // Assert
        await act.Should().ThrowAsync<Exception>()
            .WithMessage("*default 'User' role is missing*");
    }

    [Fact]
    public async Task RegisterAsync_Valid_ShouldCallAddAsync()
    {
        // Arrange
        var dto = TestDataMother.CreateRegisterDto("new@x.com", "password1");
        _userRepo.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _roleRepo.Setup(x => x.GetByNameRoleAsync("User")).ReturnsAsync(new Role { Id = 2, Name = "User" });
        _passwordHasher.Setup(x => x.Hash(dto.Password)).Returns("hashed!");

        // Act
        await _sut.RegisterAsync(dto);

        // Assert
        _passwordHasher.Verify(x => x.Hash(dto.Password), Times.Once);
        _roleRepo.Verify(x => x.GetByNameRoleAsync("User"), Times.Once);
        _userRepo.Verify(
            x => x.AddAsync(It.Is<User>(
                u => u.Email == dto.Email &&
                u.PasswordHash == "hashed!" &&
                u.RoleId == 2
                )),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_UserNotFound_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var dto = TestDataMother.CreateLoginDto();
        _deviceService.Setup(x => x.GetDeviceId()).Returns("device-login");
        _userRepo.Setup(x => x.GetByEmailAsync(dto.Email))
                 .ReturnsAsync((User?)null);

        // Giả lập chưa có lần fail nào trước đó
        _cacheService.Setup(x => x.GetAsync<int?>(It.IsAny<string>()))
                     .ReturnsAsync((int?)null);

        // Act
        Func<Task> act = () => _sut.LoginAsync(dto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
                 .WithMessage("Invalid email or password");

        // Verify: Phải tăng số lần fail trong cache
        _cacheService.Verify(x => x.SetAsync(
        It.IsAny<string>(),
        It.IsAny<int>(), 
        It.IsAny<TimeSpan>()),
        Times.Once);
    }

    [Fact]
    public async Task LoginAsync_WrongPassword_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var dto = TestDataMother.CreateLoginDto();
        var user = TestDataMother.CreateUser(70302, dto.Email, "hashed-password");

        _deviceService.Setup(x => x.GetDeviceId()).Returns("device-login");
        _userRepo.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync(user);

        // Giả lập verify mật khẩu trả về false
        _passwordHasher.Setup(x => x.Verify(dto.Password, user.PasswordHash))
                       .Returns(false);

        // Giả lập đã fail 2 lần trước đó
        _cacheService.Setup(x => x.GetAsync<int?>(It.IsAny<string>()))
                     .ReturnsAsync(2);

        // Act
        Func<Task> act = () => _sut.LoginAsync(dto);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>()
                 .WithMessage("Invalid email or password");

        // Verify verify password đã được gọi
        _passwordHasher.Verify(x => x.Verify(dto.Password, user.PasswordHash), Times.Once);

        // Verify số lần fail tăng từ 2 lên 3
        _cacheService.Verify(x => x.SetAsync(
            It.IsAny<string>(),
            3,
            It.IsAny<TimeSpan>()),
            Times.Once);
    }

    [Fact]
    public async Task LoginAsync_TooManyFailures_ShouldThrowTooManyRequestsException()
    {
        // Arrange
        var dto = TestDataMother.CreateLoginDto("lock@test.com", "bad");
        _deviceService.Setup(x => x.GetDeviceId()).Returns("device-login");
        _userRepo.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync((User?)null);
        _cacheService.Setup(x => x.GetAsync<int?>(It.IsAny<string>())).ReturnsAsync(5);

        // Act
        var act = async () => await _sut.LoginAsync(dto);

        // Assert — after increment, failCount 6 > MaxLoginFailures (5)
        (await act.Should().ThrowAsync<TooManyRequestsException>())
            .Which.Message.Should().Be("Too many login attempts. Try again later.");
    }

    [Fact]
    public async Task LoginAsync_MissingDeviceId_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var dto = TestDataMother.CreateLoginDto();
        _deviceService.Setup(x => x.GetDeviceId()).Returns("");

        // Act
        var act = async () => await _sut.LoginAsync(dto);

        // Assert
        (await act.Should().ThrowAsync<UnauthorizedException>())
            .WithMessage("X-Device-Id header is required for login");
        _userRepo.Verify(x => x.GetByEmailAsync(It.IsAny<string>()), Times.Never);
        _passwordHasher.Verify(x => x.Verify(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_Valid_ShouldReturnAuthResponse()
    {
        // Arrange
        var userId = 70306;
        var dto = TestDataMother.CreateLoginDto("ok@test.com", "pwd");
        var userForCred = TestDataMother.CreateUser(userId, dto.Email, "hash");
        var userForUpdate = TestDataMother.CreateUser(userId, dto.Email, "hash", sessionVersion: 3);
        _userRepo.Setup(x => x.GetByEmailAsync(dto.Email)).ReturnsAsync(userForCred);
        _passwordHasher.Setup(x => x.Verify(dto.Password, userForCred.PasswordHash)).Returns(true);
        _deviceService.Setup(x => x.GetDeviceId()).Returns("device-70306");
        _fingerprint.Setup(x => x.ComputeFingerprint("device-70306")).Returns("fp-70306");
        _fingerprint.Setup(x => x.GetClientIpAddress()).Returns("ip-70306");
        _userRepo.Setup(x => x.GetByIdForUpdateAsync(userId)).ReturnsAsync(userForUpdate);
        _jwtService.Setup(x => x.GenerateRefreshToken()).Returns("plain-rt");
        _jwtService.Setup(x => x.HashToken("plain-rt")).Returns("hash-rt");
        _jwtService
            .Setup(x => x.GenerateAccessToken(
                It.IsAny<User>(),
                It.IsAny<Guid>(),
                It.IsAny<int>(),
                "fp-70306"))
            .Returns("access-token");

        // Act
        var result = await _sut.LoginAsync(dto);

        // Assert
        result.AccessToken.Should().Be("access-token");
        result.RefreshToken.Should().Be("plain-rt");
        _refreshTokenRepo.Verify(x => x.RevokeAllForUserAsync(userId), Times.Once);
        _refreshTokenRepo.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Once);
        _userRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _jwtService.Verify(
            x => x.GenerateAccessToken(
                It.Is<User>(u => u.Id == userId),
                userForUpdate.CurrentSessionId!.Value,
                userForUpdate.SessionVersion,
                "fp-70306"),
            Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidUserIdClaim_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("sid", Guid.NewGuid().ToString()),
            new Claim("sv", "1"),
            new Claim("fp", "fp")
        }));
        _jwtService.Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns(principal);
        var req = TestDataMother.CreateRefreshRequest();

        // Act
        var act = async () => await _sut.RefreshTokenAsync(req);

        // Assert
        (await act.Should().ThrowAsync<UnauthorizedException>())
            .WithMessage("Invalid token");
    }

    [Fact]
    public async Task RefreshTokenAsync_InvalidSvClaim_ShouldThrowUnauthorizedException()
    {
        // Arrange
        var userId = 70308;
        var sid = Guid.NewGuid().ToString();
        var principal = TestDataMother.CreateClaimsPrincipal(userId, sid, "not-int", "fp");
        _jwtService.Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns(principal);
        var req = TestDataMother.CreateRefreshRequest();

        // Act
        var act = async () => await _sut.RefreshTokenAsync(req);

        // Assert
        (await act.Should().ThrowAsync<UnauthorizedException>())
            .WithMessage("Invalid token");
    }

    [Fact]
    public async Task RefreshTokenAsync_RefreshTokenRevoked_ShouldInvalidateAndThrow()
    {
        // Arrange
        var userId = 70309;
        var sid = Guid.NewGuid();
        var principal = TestDataMother.CreateClaimsPrincipal(userId, sid.ToString(), "1", "fp");
        _jwtService.Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns(principal);
        _deviceService.Setup(x => x.GetDeviceId()).Returns("d");
        _fingerprint.Setup(x => x.ComputeFingerprint("d")).Returns("fp");
        _sessionValidation
            .Setup(x => x.EnsureAccessTokenSessionValidAsync(
                userId,
                sid.ToString(),
                "1",
                "fp",
                "fp",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _jwtService.Setup(x => x.HashToken("rt-plain")).Returns("h1");
        var rt = new RefreshToken(userId, "h1", DateTime.UtcNow.AddDays(1), "d", sid, Guid.NewGuid());
        rt.Revoke();
        _refreshTokenRepo.Setup(x => x.GetByTokenHashAnyAsync("h1")).ReturnsAsync(rt);
        var req = TestDataMother.CreateRefreshRequest("at", "rt-plain");

        // Act
        var act = async () => await _sut.RefreshTokenAsync(req);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedException>();
        _sessionInvalidation.Verify(x => x.InvalidateAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshTokenAsync_Valid_ShouldReturnNewAccessTokenAndSameRefreshTokenWithoutRotation()
    {
        // Arrange
        var userId = 70310;
        var sid = Guid.NewGuid();
        var principal = TestDataMother.CreateClaimsPrincipal(userId, sid.ToString(), "4", "fp");
        _jwtService.Setup(x => x.GetPrincipalFromExpiredToken(It.IsAny<string>())).Returns(principal);
        _jwtService.Setup(x => x.GetAccessTokenRemainingLifetime(It.IsAny<string>())).Returns((TimeSpan?)null);
        _deviceService.Setup(x => x.GetDeviceId()).Returns("dev");
        _fingerprint.Setup(x => x.ComputeFingerprint("dev")).Returns("fp");
        _fingerprint.Setup(x => x.GetClientIpAddress()).Returns("ip");
        _sessionValidation
            .Setup(x => x.EnsureAccessTokenSessionValidAsync(
                userId,
                sid.ToString(),
                "4",
                "fp",
                "fp",
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _jwtService.Setup(x => x.HashToken("old-rt")).Returns("rt-h");
        var stored = new RefreshToken(userId, "rt-h", DateTime.UtcNow.AddDays(1), "dev", sid, Guid.NewGuid());
        stored.Id = 900;
        _refreshTokenRepo.Setup(x => x.GetByTokenHashAnyAsync("rt-h")).ReturnsAsync(stored);
        var user = TestDataMother.CreateUser(userId, sessionVersion: 4);
        _userRepo.Setup(x => x.GetByIdForUpdateAsync(userId)).ReturnsAsync(user);
        _jwtService
            .Setup(x => x.GenerateAccessToken(It.IsAny<User>(), sid, 4, "fp"))
            .Returns("new-access");

        var req = TestDataMother.CreateRefreshRequest("expired-at", "old-rt");

        // Act
        var result = await _sut.RefreshTokenAsync(req);

        // Assert
        result.AccessToken.Should().Be("new-access");
        result.RefreshToken.Should().Be("old-rt");
        _refreshTokenRepo.Verify(x => x.RevokeByIdAsync(It.IsAny<int>()), Times.Never);
        _refreshTokenRepo.Verify(x => x.AddAsync(It.IsAny<RefreshToken>()), Times.Never);
        _userRepo.Verify(x => x.SaveChangesAsync(), Times.Once);
        _cacheService.Verify(
            x => x.RemoveByPrefixAsync(CacheKeyGenerator.AuthSessionUserPrefix(userId)),
            Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_WithValidAccessToken_ShouldBlacklistWhenRemainingPositive()
    {
        // Arrange
        var userId = 70311;
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, "jid-1")
        }));
        _jwtService.Setup(x => x.GetPrincipalFromExpiredToken("at")).Returns(principal);
        _jwtService.Setup(x => x.GetAccessTokenRemainingLifetime("at")).Returns(TimeSpan.FromMinutes(3));
        _jwtService.Setup(x => x.HashToken("jid-1")).Returns("jti-hash");

        // Act
        await _sut.LogoutAsync(userId, "at");

        // Assert
        _tokenBlacklist.Verify(
            x => x.BlacklistAsync("jti-hash", It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _sessionInvalidation.Verify(x => x.InvalidateAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task LogoutAsync_ShouldCallInvalidateAsync()
    {
        // Arrange
        var userId = 70312;

        // Act
        await _sut.LogoutAsync(userId, string.Empty);

        // Assert
        _sessionInvalidation.Verify(x => x.InvalidateAsync(userId, It.IsAny<CancellationToken>()), Times.Once);
        _tokenBlacklist.Verify(
            x => x.BlacklistAsync(It.IsAny<string>(), It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HasPermissionAsync_WhenRoleIsAdmin_ReturnsTrue_WithoutCheckingRolePermissions()
    {
        var adminRole = new Role { Id = 1, Name = "Admin", RolePermissions = [] };
        var user = new User { Id = 10, RoleId = 1, Role = adminRole };
        _userRepo.Setup(x => x.GetByIdWithPermissionsAsync(10)).ReturnsAsync(user);

        var ok = await _sut.HasPermissionAsync(10, "any.permission");

        ok.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_WhenUserHasPermission_ReturnsTrue()
    {
        var perm = new Permission { Id = 2, Name = "product.read", Entity = "product", Action = "read" };
        var role = new Role
        {
            Id = 3,
            Name = "User",
            RolePermissions = [new RolePermission { Permission = perm, PermissionId = perm.Id, RoleId = 3 }]
        };
        var user = new User { Id = 20, RoleId = 3, Role = role };
        _userRepo.Setup(x => x.GetByIdWithPermissionsAsync(20)).ReturnsAsync(user);

        var ok = await _sut.HasPermissionAsync(20, "product.read");

        ok.Should().BeTrue();
    }

    [Fact]
    public async Task HasPermissionAsync_WhenUserLacksPermission_ReturnsFalse()
    {
        var role = new Role { Id = 3, Name = "User", RolePermissions = [] };
        var user = new User { Id = 21, RoleId = 3, Role = role };
        _userRepo.Setup(x => x.GetByIdWithPermissionsAsync(21)).ReturnsAsync(user);

        var ok = await _sut.HasPermissionAsync(21, "product.delete");

        ok.Should().BeFalse();
    }
}
