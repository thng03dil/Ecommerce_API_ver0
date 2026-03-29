using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace Ecommerce.Application.Authorization;

public class PermissionPolicyProvider : DefaultAuthorizationPolicyProvider
{
    private static readonly Regex PermissionStyleName = new(
        "^[a-z0-9]+(\\.[a-z0-9]+)+$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    public PermissionPolicyProvider(IOptions<AuthorizationOptions> options)
        : base(options)
    {
    }

    public override async Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith("Permission:", StringComparison.Ordinal))
        {
            var permission = policyName["Permission:".Length..];
            return BuildPermissionPolicy(permission);
        }

        var registered = await base.GetPolicyAsync(policyName);
        if (registered != null)
            return registered;

        if (IsPermissionStylePolicy(policyName))
            return BuildPermissionPolicy(policyName);

        return null;
    }

    private static bool IsPermissionStylePolicy(string policyName) =>
        !string.IsNullOrWhiteSpace(policyName) && PermissionStyleName.IsMatch(policyName);

    private static AuthorizationPolicy BuildPermissionPolicy(string permission)
    {
        var policy = new AuthorizationPolicyBuilder();
        policy.AddRequirements(new PermissionRequirement(permission));
        return policy.Build();
    }
}
