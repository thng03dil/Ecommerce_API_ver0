using System.Security.Claims;

namespace Ecommerce.Application.Authorization;

public static class PermissionAuthConstants
{
    public const int AdminRoleId = 1;
    public const string AdminRoleName = "Admin";

    public const string RoleIdClaimType = "role_id";

    public static bool IsSupremeRole(int roleId, string? roleName) =>
        roleId == AdminRoleId
        || string.Equals(roleName, AdminRoleName, StringComparison.OrdinalIgnoreCase);

    public static bool IsSupremeFromJwtClaims(string? roleIdClaim, string? roleNameClaim)
    {
        if (int.TryParse(roleIdClaim, out var rid) && rid == AdminRoleId)
            return true;
        if (!string.IsNullOrEmpty(roleNameClaim)
            && string.Equals(roleNameClaim, AdminRoleName, StringComparison.OrdinalIgnoreCase))
            return true;
        return false;
    }

    public static string? GetRoleIdClaim(ClaimsPrincipal user) =>
        user.FindFirst(RoleIdClaimType)?.Value;

    public static string? GetRoleNameClaim(ClaimsPrincipal user) =>
        user.FindFirst(ClaimTypes.Role)?.Value;
}
