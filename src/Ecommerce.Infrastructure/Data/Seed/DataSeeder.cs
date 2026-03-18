using Ecommerce.Application.DTOs;
using Ecommerce.Domain.Entities;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.Infrastructure.Data.Seed
{
    public static class DataSeeder
    {
        public static async Task SeedAdminAsync(AppDbContext context)
        {
            // Assign full permissions to Admin 
            var allPermissionIds = await context.Permissions.Select(p => p.Id).ToListAsync();
            var currentAdminPIds = await context.RolePermissions
                .Where(rp => rp.RoleId == 1) 
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var missingAdminPIds = allPermissionIds.Except(currentAdminPIds).ToList();
            foreach (var pId in missingAdminPIds)
            {
                context.RolePermissions.Add(new RolePermission { RoleId = 1, PermissionId = pId });
            }

            // Assign read permissions to User 
            var userPermNames = new List<string>
            {
                "product.read",
                "category.read"
            };

            var userPermIds = await context.Permissions
                .Where(p => userPermNames.Contains(p.Name))
                .Select(p => p.Id)
                .ToListAsync();

            var currentUserPIds = await context.RolePermissions
                .Where(rp => rp.RoleId == 2) 
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var missingUserPIds = userPermIds.Except(currentUserPIds).ToList();
            foreach (var pId in missingUserPIds)
            {
                context.RolePermissions.Add(new RolePermission { RoleId = 2, PermissionId = pId });
            }

            await context.SaveChangesAsync();

            // Seed admin user (for local/dev usage)
            if (!await context.Users.AnyAsync(u => u.Email == "admin@example.com"))
            {
                context.Users.Add(new User
                {
                    Email = "admin@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin123!"),
                    RoleId = 1, // Admin
                    CreatedAt = DateTime.UtcNow,
                    IsDeleted = false
                });
                await context.SaveChangesAsync();
            }
        }
    }
}