using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ecommerce.Application.Common.Auth;
using Ecommerce.Application.Common.Pagination;
using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.DTOs.Common;
using Ecommerce.Application.DTOs.ProductDtos;
using Ecommerce.Domain.Common;
using Ecommerce.Domain.Common.Filters;
using Ecommerce.Domain.Common.Settings;
using Ecommerce.Domain.Entities;
using Microsoft.Extensions.Options;

namespace Ecommerce.UnitTests.Helpers;

internal static class TestDataMother
{
    private const string DefaultJwtKey = "0123456789abcdef0123456789abcdef"; // 32 chars

    public static JwtSettings CreateJwtSettings(
        string? key = null,
        string? issuer = null,
        string? audience = null,
        int expiryMinutes = 15,
        int refreshTokenDays = 7)
    {
        return new JwtSettings
        {
            Key = key ?? DefaultJwtKey,
            Issuer = issuer ?? "test-issuer",
            Audience = audience ?? "test-audience",
            ExpiryMinutes = expiryMinutes,
            RefreshTokenDays = refreshTokenDays
        };
    }

    public static IOptions<JwtSettings> CreateJwtOptions(JwtSettings? settings = null) =>
        Options.Create(settings ?? CreateJwtSettings());

    public static Permission CreatePermission(string name, int id = 1) =>
        new() { Id = id, Name = name, Entity = "e", Action = "a" };

    public static Role CreateRole(string name = "User", int id = 1, IReadOnlyList<Permission>? permissions = null)
    {
        var role = new Role { Id = id, Name = name, IsSystem = true };
        if (permissions is null || permissions.Count == 0)
            return role;

        foreach (var p in permissions)
        {
            role.RolePermissions.Add(new RolePermission { Role = role, RoleId = role.Id, Permission = p, PermissionId = p.Id });
        }

        return role;
    }

    public static User CreateUser(
        int id = 1,
        string email = "user@test.com",
        string passwordHash = "hash",
        int roleId = 1,
        int sessionVersion = 1,
        Guid? currentSessionId = null,
        Role? role = null,
        string? refreshTokenHash = null,
        DateTime? refreshTokenExpiresAtUtc = null)
    {
        var r = role ?? CreateRole("User", roleId);
        return new User
        {
            Id = id,
            Email = email,
            PasswordHash = passwordHash,
            RoleId = roleId,
            Role = r,
            SessionVersion = sessionVersion,
            CurrentSessionId = currentSessionId,
            RefreshTokenHash = refreshTokenHash,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc
        };
    }

    public static UserSessionState CreateUserSessionState(Guid sessionId, int sessionVersion, string fingerprint) =>
        new()
        {
            SessionId = sessionId,
            SessionVersion = sessionVersion,
            FingerprintHash = fingerprint
        };

    public static UserAuthState CreateUserAuthState(
        int sessionVersion,
        Guid? currentSessionId,
        string fingerprintHash,
        string? refreshTokenHash = "rt-hash",
        DateTime? refreshTokenExpiresAtUtc = null) =>
        new(sessionVersion, currentSessionId, fingerprintHash,
            refreshTokenHash,
            refreshTokenExpiresAtUtc ?? DateTime.UtcNow.AddDays(7));

    public static Product CreateProduct(int id = 1, int categoryId = 1, string name = "P1", Category? category = null)
    {
        var cat = category ?? new Category { Id = categoryId, Name = "Cat" };
        return new Product
        {
            Id = id,
            Name = name,
            Price = 10m,
            Stock = 5,
            CategoryId = categoryId,
            Category = cat
        };
    }

    public static ProductCreateDto CreateProductCreateDto(int categoryId = 1) =>
        new()
        {
            Name = "New",
            Description = "d",
            Price = 9.99m,
            Stock = 3,
            CategoryId = categoryId
        };

    public static ProductUpdateDto CreateProductUpdateDto(int id = 1, int categoryId = 1) =>
        new()
        {
            Id = id,
            Name = "Upd",
            Description = "d",
            Price = 11m,
            Stock = 2,
            CategoryId = categoryId
        };

    public static ProductFilterDto CreateProductFilter() =>
        new() { Keyword = "k", CategoryId = 1, SortBy = "Id", SortOrder = "asc" };

    public static PaginationDto CreatePagination(int page = 1, int size = 10) =>
        new() { PageNumber = page, PageSize = size };

    public static RegisterDto CreateRegisterDto(string email = "a@b.com", string password = "secret12") =>
        new() { Email = email, Password = password };

    public static LoginDto CreateLoginDto(string email = "a@b.com", string password = "secret12", string? deviceId = "test-device") =>
        new() { Email = email, Password = password, DeviceId = deviceId };

    public static RefreshTokenRequestDto CreateRefreshRequest(string access = "at", string refresh = "rt") =>
        new() { AccessToken = access, RefreshToken = refresh };

    public static ClaimsPrincipal CreateClaimsPrincipal(
        int userId,
        string sid,
        string sv,
        string fp,
        string? jti = null)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("sid", sid),
            new("sv", sv),
            new("fp", fp)
        };
        if (jti != null)
            claims.Add(new Claim(JwtRegisteredClaimNames.Jti, jti));
        return new ClaimsPrincipal(new ClaimsIdentity(claims));
    }
}
