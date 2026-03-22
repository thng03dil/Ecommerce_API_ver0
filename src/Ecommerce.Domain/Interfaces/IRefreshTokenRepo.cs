using Ecommerce.Domain.Entities;

namespace Ecommerce.Domain.Interfaces
{
    public interface IRefreshTokenRepo
    {
        Task AddAsync(RefreshToken token);

        /// <summary>Includes revoked rows (for reuse detection).</summary>
        Task<RefreshToken?> GetByTokenHashAnyAsync(string tokenHash);

        Task RevokeAllForUserAsync(int userId);

        Task RevokeByIdAsync(int id);

        Task SaveChangesAsync();
    }
}
