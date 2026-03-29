namespace Ecommerce.Application.Services.Interfaces;

/// <summary>Role → permission names with Redis cache; invalidated when role permissions change.</summary>
public interface IRolePermissionService
{
    Task<bool> RoleHasPermissionAsync(int roleId, string permission, CancellationToken cancellationToken = default);

    Task InvalidateRoleCacheAsync(int roleId, CancellationToken cancellationToken = default);
}
