using Ecommerce.Application.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Ecommerce.UnitTests;

public class PermissionPolicyProviderTests
{
    private static PermissionPolicyProvider CreateSut()
    {
        var services = new ServiceCollection();
        services.AddAuthorization();
        var sp = services.BuildServiceProvider();
        var options = sp.GetRequiredService<IOptions<AuthorizationOptions>>();
        return new PermissionPolicyProvider(options);
    }

    [Fact]
    public async Task GetPolicyAsync_ProductRead_ShouldReturnPolicyWithRequirement()
    {
        var sut = CreateSut();

        var policy = await sut.GetPolicyAsync("product.read");

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>().Should().ContainSingle()
            .Which.Permission.Should().Be("product.read");
    }

    [Fact]
    public async Task GetPolicyAsync_LegacyPermissionPrefix_ShouldStillWork()
    {
        var sut = CreateSut();

        var policy = await sut.GetPolicyAsync("Permission:category.read");

        policy.Should().NotBeNull();
        policy!.Requirements.OfType<PermissionRequirement>().Should().ContainSingle()
            .Which.Permission.Should().Be("category.read");
    }

    [Fact]
    public async Task GetPolicyAsync_RandomName_ShouldReturnNull()
    {
        var sut = CreateSut();

        var policy = await sut.GetPolicyAsync("NotARegisteredPolicyName");

        policy.Should().BeNull();
    }
}
