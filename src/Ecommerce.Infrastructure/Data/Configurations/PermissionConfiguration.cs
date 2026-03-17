using Microsoft.EntityFrameworkCore;
using Ecommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Data.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(Microsoft.EntityFrameworkCore.Metadata.Builders.EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permissions"); 

            builder.HasKey(r => r.Id);

            builder.Property(r => r.Name)
                .IsRequired()
                .HasMaxLength(100);

            // Soft delete friendly uniqueness
            builder.HasIndex(p => p.Name)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.Property(p => p.Entity)
                .IsRequired()
                .HasMaxLength(50);

            builder.Property(p => p.Action)
                .IsRequired()
                .HasMaxLength(50);

            builder.HasIndex(p => new { p.Entity, p.Action })
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            builder.Property(p => p.Description)
               .IsRequired()
               .HasMaxLength(200);

            builder.Property(p => p.IsSystem)
                .IsRequired()
                .HasDefaultValue(false);

            builder.HasMany(p => p.RolePermissions)
                .WithOne(rp => rp.Permission)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.CreatedAt)
             .IsRequired()
             .HasDefaultValueSql("GETDATE()");

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            var staticDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            builder.HasData(
                // Products
                new Permission { Id = 1, Entity = "product", Action = "read", Name = "product.read", Description = "Read products", IsSystem = true, CreatedAt = staticDate },
                new Permission { Id = 2, Entity = "product", Action = "create", Name = "product.create", Description = "Create products", IsSystem = true, CreatedAt = staticDate },
                new Permission { Id = 3, Entity = "product", Action = "update", Name = "product.update", Description = "Update products", IsSystem = true, CreatedAt = staticDate },
                new Permission { Id = 4, Entity = "product", Action = "delete", Name = "product.delete", Description = "Delete products", IsSystem = true, CreatedAt = staticDate },

                // Categories
                new Permission { Id = 5, Entity = "category", Action = "read", Name = "category.read", Description = "Read categories", IsSystem = true, CreatedAt = staticDate  },
                new Permission { Id = 6, Entity = "category", Action = "create", Name = "category.create", Description = "Create categories", IsSystem = true, CreatedAt = staticDate },
                new Permission { Id = 7, Entity = "category", Action = "update", Name = "category.update", Description = "Update categories", IsSystem = true, CreatedAt = staticDate },
                new Permission { Id = 8, Entity = "category", Action = "delete", Name = "category.delete", Description = "Delete categories", IsSystem = true, CreatedAt = staticDate },

                // Users
                new Permission { Id = 9, Entity = "user", Action = "read", Name = "user.read", Description = "Read users", IsSystem = true, CreatedAt = staticDate },
                new Permission { Id = 10, Entity = "user", Action = "update", Name = "user.update", Description = "Update users", IsSystem = true, CreatedAt = staticDate },
                new Permission { Id = 11, Entity = "user", Action = "delete", Name = "user.delete", Description = "Delete users", IsSystem = true, CreatedAt = staticDate },

                // Roles
                new Permission { Id = 12, Entity = "role", Action = "read", Name = "role.read", Description = "Read roles", IsSystem = true, CreatedAt = staticDate },
                new Permission { Id = 13, Entity = "role", Action = "create", Name = "role.create", Description = "Create roles", IsSystem = true, CreatedAt = staticDate },
                new Permission { Id = 14, Entity = "role", Action = "update", Name = "role.update", Description = "Update role permissions", IsSystem = true, CreatedAt = staticDate },
                new Permission { Id = 15, Entity = "role", Action = "delete", Name = "role.delete", Description = "Delete roles", IsSystem = true, CreatedAt = staticDate },

                // Permissions
                new Permission { Id = 16, Entity = "permission", Action = "read", Name = "permission.read", Description = "Read permissions", IsSystem = true, CreatedAt = staticDate },
                new Permission { Id = 17, Entity = "permission", Action = "create", Name = "permission.create", Description = "Create permissions", IsSystem = true, CreatedAt = staticDate },
                new Permission { Id = 18, Entity = "permission", Action = "update", Name = "permission.update", Description = "Update permissions", IsSystem = true, CreatedAt = staticDate },
                new Permission { Id = 19, Entity = "permission", Action = "delete", Name = "permission.delete", Description = "Delete permissions", IsSystem = true, CreatedAt = staticDate }
    );
        }
    }
}
