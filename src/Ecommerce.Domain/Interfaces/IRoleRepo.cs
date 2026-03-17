using Ecommerce.Application.Common.Pagination;
using Ecommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Domain.Interfaces
{
   public interface IRoleRepo
    {
        Task<(IEnumerable<Role>, int totalCount)> GetAllAsync(PaginationDto pagedto);
        Task<Role?> GetByIdAsync(int id);
        Task<Role?> GetByIdWithPermissionsAsync(int id);
        Task<bool> IsRoleInUseAsync(int roleId);
        Task<bool> ExistsByNameAsync(string name);
        Task<Role?> GetByNameRoleAsync(string nameRole);
        Task AddAsync(Role role);
        Task UpdateAsync(Role role);
        Task SaveChangesAsync();

    }
}
