using Ecommerce.Application.Common.Pagination;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Domain.Interfaces
{
    public interface IPermissionRepo
    {
        Task<(IEnumerable<Permission>, int totalCount)> GetAllAsync(PaginationDto pagedto);
        Task<Permission?> GetByIdAsync(int id);
        Task<bool> ExistsByEntityActionAsync(string entity, string action, int? excludePermissionId = null);
        Task HardDeleteRolePermissionsByPermissionIdAsync(int permissionId);
        Task<bool> AllIdsExistAsync(List<int> ids);
        Task AddAsync(Permission permission);
        Task UpdateAsync(Permission permission);
        Task<bool> RolePermissionExistsAsync(int roleId, int permissionId);
        Task AddRolePermissionAsync(RolePermission rolePermission);
        Task SaveChangesAsync();
    }
}
