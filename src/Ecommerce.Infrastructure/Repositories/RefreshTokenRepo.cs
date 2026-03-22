using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Repositories
{
    public class RefreshTokenRepo : IRefreshTokenRepo
    {
        private readonly AppDbContext _context;

        public RefreshTokenRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(RefreshToken token)
        {
            await _context.RefreshTokens.AddAsync(token);
        }

        public async Task<RefreshToken?> GetByTokenHashAnyAsync(string tokenHash)
        {
            return await _context.RefreshTokens
                .Include(x => x.User)
                .ThenInclude(u => u.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash);
        }

        public async Task RevokeAllForUserAsync(int userId)
        {
            await _context.RefreshTokens
                .Where(x => x.UserId == userId && !x.IsRevoked)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true));
        }

        public async Task RevokeByIdAsync(int id)
        {
            await _context.RefreshTokens
                .Where(x => x.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.IsRevoked, true));
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
