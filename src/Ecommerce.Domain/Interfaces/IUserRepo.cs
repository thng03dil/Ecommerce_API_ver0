using Ecommerce.Application.Common.Pagination;
using Ecommerce.Domain.Common;
using Ecommerce.Domain.Entities;

namespace Ecommerce.Domain.Interfaces
{
    public interface IUserRepo
    {
        Task<(IEnumerable<User>, int totalCount)> GetAllAsync(PaginationDto pagedto);
        Task<User?> GetByIdAsync(int id);
        Task<User?> GetByIdWithPermissionsAsync(int id);
        Task<User?> GetByIdForUpdateAsync(int id);
        Task<User?> GetByEmailAsync(string email);

        Task<UserAuthState?> GetUserAuthStateAsync(int userId);

        /// <summary>
        /// Reassigns all users with fromRoleId to toRoleId. Does not call SaveChanges (caller manages transaction).
        /// </summary>
        /// <returns>IDs of affected users (for cache invalidation).</returns>
        Task<IReadOnlyList<int>> ReassignUsersToRoleAsync(int fromRoleId, int toRoleId);

        /// <summary>User chưa xóa mềm đang gán role này.</summary>
        Task<IReadOnlyList<int>> GetActiveUserIdsByRoleIdAsync(int roleId, CancellationToken cancellationToken = default);

        Task AddAsync(User user);
        Task SaveChangesAsync();
    }
}
 