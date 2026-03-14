using Azure.Core;
using Ecommerce_API.Data;
using Ecommerce_API.Models;
using Ecommerce_API.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;


namespace Ecommerce_API.Repositories.Implementations
{
    public class UserRepo : IUserRepo
    {
        private readonly AppDbContext _context;
        public UserRepo(AppDbContext context)
        {
            _context = context;
        }
        public async Task<User?> GetByIdAsync(int id)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.Email == email);
        }
        public async Task<bool> EmailExistingAsync(string email)
        {
            return await _context.Users.AnyAsync(x => x.Email == email);
        }

        public async Task<User?> GetByRefreshTokenAsync(string refreshToken)
        {
            return await _context.Users
                .FirstOrDefaultAsync(x => x.RefreshToken == refreshToken);
        }

        public async Task AddAsync(User user)
        {
            await _context.Users.AddAsync(user);

        }

        public async Task UpdateAsync(User user)
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
