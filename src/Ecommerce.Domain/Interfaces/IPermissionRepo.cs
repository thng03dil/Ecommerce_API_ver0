using Ecommerce.Application.Common.Pagination;
using Ecommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Interfaces
{
    public interface IPermissionRepo
    {
        Task<(IEnumerable<Permission>, int totalCount)> GetAllAsync(PaginationDto pagedto);
        Task<Permission?> GetByIdAsync(int id);
        Task<bool> ExistsByEntityActionAsync(string entity, string action, int? excludePermissionId = null);
        Task<bool> IsAssignedToAnyRoleAsync(int permissionId);
        Task<bool> IsAssignedToAnyNonAdminRoleAsync(int permissionId);
        Task HardDeleteRolePermissionsByPermissionIdAsync(int permissionId);
        Task<bool> AllIdsExistAsync(List<int> ids);
        Task<bool> IsPermissionIdExistAsync(int id);
        Task AddAsync(Permission permission);
        Task UpdateAsync(Permission permission);
        Task<bool> RolePermissionExistsAsync(int roleId, int permissionId);
        Task AddRolePermissionAsync(RolePermission rolePermission);
        Task SaveChangesAsync();
    }
}
