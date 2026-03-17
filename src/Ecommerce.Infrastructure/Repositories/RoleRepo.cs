using Ecommerce.Application.Common.Pagination;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Data;


using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Repositories
{
    public class RoleRepo : IRoleRepo
    {
        private readonly AppDbContext _context;
        public RoleRepo(AppDbContext context)
        {
            _context = context;
        }
        public async Task<(IEnumerable<Role>, int totalCount)> GetAllAsync(PaginationDto pagedto)
        {
            var query = _context.Roles
                   .AsNoTracking()
                   .Include(x => x.RolePermissions)
                   .Where(x => !x.IsDeleted);

            var totalItem = await query.CountAsync();

            var items = await query
                .OrderBy(c => c.Id)
                .Skip((pagedto.PageNumber - 1) * pagedto.PageSize)
                .Take(pagedto.PageSize)
                .ToListAsync();
            return (items, totalItem);

        }
        public async Task<Role?> GetByIdAsync(int id)
        {
            return await _context.Roles
                    .Include(c => c.RolePermissions)
                    .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);
        }
        public async Task<Role?> GetByIdWithPermissionsAsync(int id)
        {
            return await _context.Roles
                .Include(r => r.RolePermissions)
                .FirstOrDefaultAsync(r => r.Id == id);
        }
        public async Task<bool> IsRoleInUseAsync(int roleId)
        {
            return await _context.Users.AnyAsync(u => u.RoleId == roleId && !u.IsDeleted);
        }
        public async Task<Role?> GetByNameRoleAsync(string nameRole)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.Name == nameRole);
        }
        public async Task<bool> ExistsByNameAsync(string name)
        {
            return await _context.Roles
                .AnyAsync(r => r.Name.ToLower() == name.ToLower());
        }
        public async Task AddAsync(Role role)
        {
            await _context.Roles.AddAsync(role);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Role role)
        {
            _context.Roles.Update(role);
            await _context.SaveChangesAsync();
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
