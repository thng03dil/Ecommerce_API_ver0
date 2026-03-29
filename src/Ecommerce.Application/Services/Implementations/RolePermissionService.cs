using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Interfaces;

namespace Ecommerce.Application.Services.Implementations;

public class RolePermissionService : IRolePermissionService
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(45);

    private readonly ICacheService _cache;
    private readonly IRoleRepo _roleRepo;

    public RolePermissionService(ICacheService cache, IRoleRepo roleRepo)
    {
        _cache = cache;
        _roleRepo = roleRepo;
    }

    public async Task<bool> RoleHasPermissionAsync(int roleId, string permission, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(permission))
            return false;

        var key = CacheKeyGenerator.RolePermissionNames(roleId);
        var cached = await _cache.GetAsync<List<string>>(key);
        if (cached == null)
        {
            var names = await _roleRepo.GetPermissionNamesForRoleAsync(roleId, cancellationToken);
            cached = names.Select(n => n.Trim().ToLowerInvariant()).Distinct().ToList();
            await _cache.SetAsync(key, cached, CacheTtl);
        }

        var required = permission.Trim().ToLowerInvariant();
        return cached.Exists(p => string.Equals(p, required, StringComparison.Ordinal));
    }

    public Task InvalidateRoleCacheAsync(int roleId, CancellationToken cancellationToken = default) =>
        _cache.RemoveAsync(CacheKeyGenerator.RolePermissionNames(roleId));
}
