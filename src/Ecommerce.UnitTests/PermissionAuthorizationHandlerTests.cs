using System.Security.Claims;
using Ecommerce.Application.Authorization;
using Ecommerce.Application.Services.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace Ecommerce.UnitTests;

public class PermissionAuthorizationHandlerTests
{
    private static AuthorizationHandlerContext CreateContext(ClaimsPrincipal user)
    {
        return new AuthorizationHandlerContext(
            new[] { new PermissionRequirement("product.read") },
            user,
            resource: null);
    }

    private static DefaultHttpContext CreateHttpContext(IAuthService authService)
    {
        var services = new ServiceCollection();
        services.AddSingleton(authService);
        var httpContext = new DefaultHttpContext
        {
            RequestServices = services.BuildServiceProvider()
        };
        return httpContext;
    }

    [Fact]
    public async Task WhenHasPermissionAsyncTrue_Succeeds_ViaAuthService()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "5") },
            authenticationType: "Bearer"));
        var context = CreateContext(user);
        var authMock = new Mock<IAuthService>();
        authMock.Setup(x => x.HasPermissionAsync(5, "product.read")).ReturnsAsync(true);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(CreateHttpContext(authMock.Object));

        var sut = new PermissionAuthorizationHandler(accessor.Object);
        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
        authMock.Verify(x => x.HasPermissionAsync(5, "product.read"), Times.Once);
    }

    [Fact]
    public async Task NonAdmin_WhenHasPermissionInDb_Succeeds()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "12") },
            authenticationType: "Bearer"));
        var context = CreateContext(user);
        var authMock = new Mock<IAuthService>();
        authMock.Setup(x => x.HasPermissionAsync(12, "product.read")).ReturnsAsync(true);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(CreateHttpContext(authMock.Object));

        var sut = new PermissionAuthorizationHandler(accessor.Object);
        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task NonAdmin_WhenMissingUserId_DoesNotSucceed()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            Array.Empty<Claim>(),
            authenticationType: "Bearer"));
        var context = CreateContext(user);
        var authMock = new Mock<IAuthService>(MockBehavior.Strict);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(CreateHttpContext(authMock.Object));

        var sut = new PermissionAuthorizationHandler(accessor.Object);
        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task NonAdmin_WhenHasPermissionFalse_DoesNotSucceed()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[] { new Claim(ClaimTypes.NameIdentifier, "3") },
            authenticationType: "Bearer"));
        var context = CreateContext(user);
        var authMock = new Mock<IAuthService>();
        authMock.Setup(x => x.HasPermissionAsync(3, "product.read")).ReturnsAsync(false);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(CreateHttpContext(authMock.Object));

        var sut = new PermissionAuthorizationHandler(accessor.Object);
        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }
}
