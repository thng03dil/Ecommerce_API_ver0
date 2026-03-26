using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Ecommerce.Domain.Common.Settings;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.SecurityHelpers;
using Ecommerce.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace Ecommerce.UnitTests;

public class JwtServiceTests
{
    private readonly JwtSettings _settings;

    public JwtServiceTests()
    {
        _settings = TestDataMother.CreateJwtSettings();
    }

    [Fact]
    public void Ctor_EmptyKey_ShouldThrowException()
    {
        // Arrange
        var bad = TestDataMother.CreateJwtSettings(key: "   ");

        // Act
        var act = () => new JwtService(TestDataMother.CreateJwtOptions(bad), NullLogger<JwtService>.Instance);

        // Assert
        act.Should().Throw<Exception>().WithMessage("*JWT Key is missing*");
    }

    [Fact]
    public void Ctor_ShortKey_ShouldThrowException()
    {
        // Arrange
        var bad = TestDataMother.CreateJwtSettings(key: "1234567890123456789012345678901"); // 31

        // Act
        var act = () => new JwtService(TestDataMother.CreateJwtOptions(bad), NullLogger<JwtService>.Instance);

        // Assert
        act.Should().Throw<Exception>().WithMessage("*at least 32*");
    }

    [Fact]
    public void GenerateAccessToken_RoleNull_ShouldStillIssueToken()
    {
        var sut = new JwtService(TestDataMother.CreateJwtOptions(_settings), NullLogger<JwtService>.Instance);
        var user = new User { Id = 1, Email = "a@b.com", RoleId = 1, Role = null! };

        var token = sut.GenerateAccessToken(user, Guid.NewGuid(), 1, "fp");

        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
        var jwt = handler.ReadJwtToken(token);
        jwt.Subject.Should().Be("1");
        jwt.Claims.Should().NotContain(c => c.Type == ClaimTypes.Role);
        jwt.Claims.Should().NotContain(c => c.Type == ClaimTypes.Email);
    }

    [Fact]
    public void GenerateAccessToken_ValidUser_ShouldReturnParsableJwt()
    {
        // Arrange
        var sut = new JwtService(TestDataMother.CreateJwtOptions(_settings), NullLogger<JwtService>.Instance);
        var perm = TestDataMother.CreatePermission("products.read");
        var role = TestDataMother.CreateRole("Admin", 2, new[] { perm });
        var user = TestDataMother.CreateUser(5, roleId: 2, role: role);
        var sid = Guid.NewGuid();

        // Act
        var token = sut.GenerateAccessToken(user, sid, 9, "fphash");

        // Assert
        var handler = new JwtSecurityTokenHandler();
        handler.CanReadToken(token).Should().BeTrue();
        var jwt = handler.ReadJwtToken(token);
        jwt.Subject.Should().Be("5");
        jwt.Claims.Should().Contain(c => c.Type == "sid" && c.Value == sid.ToString());
        jwt.Claims.Should().Contain(c => c.Type == "sv" && c.Value == "9");
        jwt.Claims.Should().Contain(c => c.Type == "fp" && c.Value == "fphash");
        jwt.Claims.Should().NotContain(c => c.Type == ClaimTypes.Role);
        jwt.Claims.Should().NotContain(c => c.Type == ClaimTypes.Email);
        jwt.Claims.Should().NotContain(c => c.Type == "permissions");
        jwt.Claims.Should().Contain(c => c.Type == JwtRegisteredClaimNames.Jti);
    }

    [Fact]
    public void GenerateRefreshToken_ShouldReturnNonEmptyString()
    {
        // Arrange
        var sut = new JwtService(TestDataMother.CreateJwtOptions(_settings), NullLogger<JwtService>.Instance);

        // Act
        var rt = sut.GenerateRefreshToken();

        // Assert
        rt.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_ValidSignedToken_ShouldReturnClaims()
    {
        // Arrange
        var sut = new JwtService(TestDataMother.CreateJwtOptions(_settings), NullLogger<JwtService>.Instance);
        var role = TestDataMother.CreateRole();
        var user = TestDataMother.CreateUser(3, roleId: role.Id, role: role);
        var sid = Guid.NewGuid();
        var jwt = sut.GenerateAccessToken(user, sid, 2, "fp");

        // Act
        var principal = sut.GetPrincipalFromExpiredToken(jwt);

        // Assert
        principal.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value.Should().Be("3");
        principal.FindFirst("sid")!.Value.Should().Be(sid.ToString());
    }

    [Fact]
    public void GetPrincipalFromExpiredToken_MalformedToken_ShouldThrow()
    {
        // Arrange
        var sut = new JwtService(TestDataMother.CreateJwtOptions(_settings), NullLogger<JwtService>.Instance);

        // Act
        var act = () => sut.GetPrincipalFromExpiredToken("not-a-valid-jwt");

        // Assert
        act.Should().Throw<SecurityTokenMalformedException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void HashToken_NullOrEmpty_ShouldReturnEmpty(string? token)
    {
        // Arrange
        var sut = new JwtService(TestDataMother.CreateJwtOptions(_settings), NullLogger<JwtService>.Instance);

        // Act
        var hash = sut.HashToken(token!);

        // Assert
        hash.Should().BeEmpty();
    }

    [Fact]
    public void GetAccessTokenRemainingLifetime_InvalidToken_ShouldReturnNull()
    {
        // Arrange
        var sut = new JwtService(TestDataMother.CreateJwtOptions(_settings), NullLogger<JwtService>.Instance);

        // Act
        var rem = sut.GetAccessTokenRemainingLifetime("%%%");

        // Assert
        rem.Should().BeNull();
    }
}
