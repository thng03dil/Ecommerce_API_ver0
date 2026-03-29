using Ecommerce.Application.Common.Pagination;
using Ecommerce.Domain.Common;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace Ecommerce.Infrastructure.Repositories
{
    public class UserRepo : IUserRepo
    {
        private readonly AppDbContext _context;
        public UserRepo(AppDbContext context)
        {
            _context = context;
        }
        public async Task<(IEnumerable<User>, int totalCount)> GetAllAsync(PaginationDto pagedto)
        {
            var query = _context.Users
                    .AsNoTracking()
                    .Include(c => c.Role)
                    .Where(x => !x.IsDeleted);

            var totalItem = await query.CountAsync();

            var items = await query
                .OrderBy(c => c.Id)
                .Skip((pagedto.PageNumber - 1) * pagedto.PageSize)
                .Take(pagedto.PageSize)
                .ToListAsync();
            return (items, totalItem);
        }
        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .AsNoTracking()
                .Include(c => c.Role)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public async Task<User?> GetByIdWithPermissionsAsync(int id)
        {
            return await _context.Users
                .AsNoTracking()
                .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }
        public async Task<User?> GetByIdForUpdateAsync(int id)
        {
            return await _context.Users
                .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }

        public async Task<UserAuthState?> GetUserAuthStateAsync(int userId)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(x => x.Id == userId && !x.IsDeleted)
                .Select(x => new UserAuthState(
                    x.SessionVersion,
                    x.CurrentSessionId,
                    x.LastFingerprintHash,
                    x.RefreshTokenHash,
                    x.RefreshTokenExpiresAtUtc))
                .FirstOrDefaultAsync();
        }

        public async Task<(int RoleId, string RoleName)?> GetRoleContextForAuthAsync(
            int userId,
            CancellationToken cancellationToken = default)
        {
            var row = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId && !u.IsDeleted)
                .Join(
                    _context.Roles.Where(r => !r.IsDeleted),
                    u => u.RoleId,
                    r => r.Id,
                    (u, r) => new { r.Id, r.Name })
                .FirstOrDefaultAsync(cancellationToken);

            return row == null ? null : (row.Id, row.Name);
        }
        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(x => x.Email == email && !x.IsDeleted);
        }
        
        public async Task<IReadOnlyList<int>> ReassignUsersToRoleAsync(int fromRoleId, int toRoleId)
        {
            var users = await _context.Users
                .Where(u => u.RoleId == fromRoleId && !u.IsDeleted)
                .ToListAsync();

            var ids = users.Select(u => u.Id).ToList();

            foreach (var user in users)
            {
                user.RoleId = toRoleId;
                user.UpdatedAt = DateTime.UtcNow;
            }

            return ids;
        }

        public async Task<IReadOnlyList<int>> GetActiveUserIdsByRoleIdAsync(int roleId, CancellationToken cancellationToken = default)
        {
            return await _context.Users
                .AsNoTracking()
                .Where(u => u.RoleId == roleId && !u.IsDeleted)
                .Select(u => u.Id)
                .ToListAsync(cancellationToken);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

        }
        public async Task UpdateAsync(User user)
        {
            user.UpdatedAt = DateTime.UtcNow;
            _context.Users.Update(user);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
