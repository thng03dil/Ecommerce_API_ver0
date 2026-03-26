using System.Text.RegularExpressions;

namespace Ecommerce.Application.Authorization;

/// <summary>
/// Dynamic policies named like <c>entity.action</c> (e.g. <c>category.read</c>) map to <see cref="PermissionRequirement"/>.
/// </summary>
internal static class PermissionPolicyNames
{
    private static readonly Regex PermissionStyleName = new(
        "^[a-z0-9]+(\\.[a-z0-9]+)+$",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(250));

    internal static bool IsPermissionStyle(string policyName) =>
        !string.IsNullOrWhiteSpace(policyName) && PermissionStyleName.IsMatch(policyName);
}
