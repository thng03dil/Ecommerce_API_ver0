using System.Security.Claims;
using System.Threading;
using Ecommerce.Application.Authorization;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Interfaces;
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

    private static DefaultHttpContext CreateHttpContext(IUserRepo userRepo, IRolePermissionService rolePerm)
    {
        var services = new ServiceCollection();
        services.AddSingleton(userRepo);
        services.AddSingleton(rolePerm);
        return new DefaultHttpContext { RequestServices = services.BuildServiceProvider() };
    }

    [Fact]
    public async Task Jwt_role_id_1_Succeeds_WithoutDbOrRolePermissionService()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "99"),
                new Claim(PermissionAuthConstants.RoleIdClaimType, "1")
            },
            authenticationType: "Bearer"));
        var context = CreateContext(user);
        var userRepo = new Mock<IUserRepo>(MockBehavior.Strict);
        var rolePerm = new Mock<IRolePermissionService>(MockBehavior.Strict);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(CreateHttpContext(userRepo.Object, rolePerm.Object));

        var sut = new PermissionAuthorizationHandler(accessor.Object);
        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task Jwt_RoleClaim_Admin_Succeeds_WithoutDb()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "7"),
                new Claim(ClaimTypes.Role, "Admin")
            },
            authenticationType: "Bearer"));
        var context = CreateContext(user);
        var userRepo = new Mock<IUserRepo>(MockBehavior.Strict);
        var rolePerm = new Mock<IRolePermissionService>(MockBehavior.Strict);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(CreateHttpContext(userRepo.Object, rolePerm.Object));

        var sut = new PermissionAuthorizationHandler(accessor.Object);
        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task NonSupreme_WhenRoleHasPermission_Succeeds()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "12"),
                new Claim(PermissionAuthConstants.RoleIdClaimType, "3")
            },
            authenticationType: "Bearer"));
        var context = CreateContext(user);
        var userRepo = new Mock<IUserRepo>();
        userRepo.Setup(x => x.GetRoleContextForAuthAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync((3, "Editor"));
        var rolePerm = new Mock<IRolePermissionService>();
        rolePerm.Setup(x => x.RoleHasPermissionAsync(3, "product.read", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(CreateHttpContext(userRepo.Object, rolePerm.Object));

        var sut = new PermissionAuthorizationHandler(accessor.Object);
        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }

    [Fact]
    public async Task NonSupreme_WhenRoleLacksPermission_DoesNotSucceed()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "3"),
                new Claim(PermissionAuthConstants.RoleIdClaimType, "5")
            },
            authenticationType: "Bearer"));
        var context = CreateContext(user);
        var userRepo = new Mock<IUserRepo>();
        userRepo.Setup(x => x.GetRoleContextForAuthAsync(3, It.IsAny<CancellationToken>()))
            .ReturnsAsync((5, "User"));
        var rolePerm = new Mock<IRolePermissionService>();
        rolePerm.Setup(x => x.RoleHasPermissionAsync(5, "product.read", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(CreateHttpContext(userRepo.Object, rolePerm.Object));

        var sut = new PermissionAuthorizationHandler(accessor.Object);
        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task MissingUserId_DoesNotSucceed()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            Array.Empty<Claim>(),
            authenticationType: "Bearer"));
        var context = CreateContext(user);
        var userRepo = new Mock<IUserRepo>(MockBehavior.Strict);
        var rolePerm = new Mock<IRolePermissionService>(MockBehavior.Strict);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(CreateHttpContext(userRepo.Object, rolePerm.Object));

        var sut = new PermissionAuthorizationHandler(accessor.Object);
        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task UserNotFound_DoesNotSucceed()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "8"),
                new Claim(PermissionAuthConstants.RoleIdClaimType, "2")
            },
            authenticationType: "Bearer"));
        var context = CreateContext(user);
        var userRepo = new Mock<IUserRepo>();
        userRepo.Setup(x => x.GetRoleContextForAuthAsync(8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(((int, string)?)null);
        var rolePerm = new Mock<IRolePermissionService>(MockBehavior.Strict);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(CreateHttpContext(userRepo.Object, rolePerm.Object));

        var sut = new PermissionAuthorizationHandler(accessor.Object);
        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeFalse();
    }

    [Fact]
    public async Task DbSaysSupreme_Succeeds_WithoutRolePermissionCheck()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "4"),
                new Claim(PermissionAuthConstants.RoleIdClaimType, "2")
            },
            authenticationType: "Bearer"));
        var context = CreateContext(user);
        var userRepo = new Mock<IUserRepo>();
        userRepo.Setup(x => x.GetRoleContextForAuthAsync(4, It.IsAny<CancellationToken>()))
            .ReturnsAsync((1, "Admin"));
        var rolePerm = new Mock<IRolePermissionService>(MockBehavior.Strict);
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(x => x.HttpContext).Returns(CreateHttpContext(userRepo.Object, rolePerm.Object));

        var sut = new PermissionAuthorizationHandler(accessor.Object);
        await sut.HandleAsync(context);

        context.HasSucceeded.Should().BeTrue();
    }
}
