using Ecommerce.Application.Common.Pagination;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Linq;

namespace Ecommerce.Infrastructure.Repositories
{
    public class PermissionRepo : IPermissionRepo
    {
        private readonly AppDbContext _context;

        public PermissionRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(IEnumerable<Permission>, int totalCount)> GetAllAsync(PaginationDto pagedto)
        {
            var query = _context.Permissions
                  .AsNoTracking();

            var totalItem = await query.CountAsync();

            var items = await query
                .OrderBy(p => p.Id)
                .Skip((pagedto.PageNumber - 1) * pagedto.PageSize)
                .Take(pagedto.PageSize)
                .ToListAsync();

            return (items, totalItem);
        }

        public async Task<Permission?> GetByIdAsync(int id)
        {
            return await _context.Permissions.FirstOrDefaultAsync(x => x.Id == id);
        }

        public async Task<bool> ExistsByEntityActionAsync(string entity, string action, int? excludePermissionId = null)
        {
            entity = entity.Trim().ToLowerInvariant();
            action = action.Trim().ToLowerInvariant();

            var query = _context.Permissions.AsQueryable();

            if (excludePermissionId.HasValue)
            {
                query = query.Where(p => p.Id != excludePermissionId.Value);
            }

            return await query.AnyAsync(p => p.Entity == entity && p.Action == action);
        }

        public async Task<bool> IsAssignedToAnyRoleAsync(int permissionId)
        {
            return await _context.RolePermissions.AnyAsync(rp => rp.PermissionId == permissionId);
        }

        public async Task<bool> IsAssignedToAnyNonAdminRoleAsync(int permissionId)
        {
            return await _context.RolePermissions
                .Where(rp => rp.PermissionId == permissionId)
                .AnyAsync(rp =>
                    rp.Role.Name != "Admin" &&
                    !rp.Role.IsDeleted);
        }

        public async Task HardDeleteRolePermissionsByPermissionIdAsync(int permissionId)
        {
            var rows = await _context.RolePermissions
                .Where(rp => rp.PermissionId == permissionId)
                .ToListAsync();

            if (rows.Count == 0) return;

            _context.RolePermissions.RemoveRange(rows);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> AllIdsExistAsync(List<int> ids)
        {
            if (ids == null || !ids.Any()) return true;

            // count existed permission ids in the database that match the provided ids
            var count = await _context.Permissions
                .Where(p => ids.Contains(p.Id))
                .CountAsync();

            return count == ids.Count;
        }
        public async Task<bool> IsPermissionIdExistAsync(int id) 
        {
            return await _context.Permissions.AnyAsync(p => p.Id == id);
            
        }
        public async Task AddAsync(Permission permission)
        {
            await _context.Permissions.AddAsync(permission);
            await _context.SaveChangesAsync();
        }
        public async Task UpdateAsync(Permission permission)
        {
            _context.Permissions.Update(permission);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> RolePermissionExistsAsync(int roleId, int permissionId)
        {
            return await _context.RolePermissions.AnyAsync(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
        }

        public async Task AddRolePermissionAsync(RolePermission rolePermission)
        {
            await _context.RolePermissions.AddAsync(rolePermission);
            await _context.SaveChangesAsync();
        }
        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
