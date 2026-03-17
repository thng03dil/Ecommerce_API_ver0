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
            // assign full permissions to Admin role
            var allPermissionIds = await context.Permissions.Select(p => p.Id).ToListAsync();
            var currentAdminPIds = await context.RolePermissions
                .Where(rp => rp.RoleId == 1) // 1 là Admin
                .Select(rp => rp.PermissionId)
                .ToListAsync();

            var missingAdminPIds = allPermissionIds.Except(currentAdminPIds).ToList();
            foreach (var pId in missingAdminPIds)
            {
                context.RolePermissions.Add(new RolePermission { RoleId = 1, PermissionId = pId });
            }

            // assign basic permissions to User role
            var userPermNames = new List<string>
            {
                "product.view",
               "product.viewbyid",
                "category.view",
                "category.viewbyid"
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

            //sedd admin user
            if (!await context.Users.AnyAsync(u => u.Email == "admin@shop.com"))
            {
                context.Users.Add(new User
                {
                    Email = "admin@shop.com",
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