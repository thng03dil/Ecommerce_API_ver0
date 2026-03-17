using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Ecommerce.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Repositories
{
    public class PermissionRepo : IPermissionRepo
    {
        private readonly AppDbContext _context;

        public PermissionRepo(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Permission>> GetAllAsync()
        {
            // Permission thường là dữ liệu tĩnh, ít thay đổi
            return await _context.Permissions
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .ToListAsync();
        }

        public async Task<Permission?> GetByIdAsync(int id)
        {
            return await _context.Permissions.FindAsync(id);
        }

        public async Task<bool> AllIdsExistAsync(List<int> ids)
        {
            if (ids == null || !ids.Any()) return true;

            // Đếm số lượng ID tồn tại trong DB so với số lượng ID gửi lên
            var count = await _context.Permissions
                .Where(p => ids.Contains(p.Id))
                .CountAsync();

            return count == ids.Count;
        }
    }
}
