using Ecommerce.Application.Common.Pagination;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Domain.Interfaces
{
   public interface IRoleRepo
    {
        Task<(IEnumerable<Role>, int totalCount)> GetAllAsync(PaginationDto pagedto);
        Task<Role?> GetByIdAsync(int id);
        Task<Role?> GetByIdWithPermissionsAsync(int id);
        Task<bool> ExistsByNameAsync(string name);
        Task<Role?> GetByNameRoleAsync(string nameRole);
        Task AddAsync(Role role);
        Task UpdateAsync(Role role);
        Task SaveChangesAsync();

        Task<IReadOnlyList<string>> GetPermissionNamesForRoleAsync(int roleId, CancellationToken cancellationToken = default);
    }
}
