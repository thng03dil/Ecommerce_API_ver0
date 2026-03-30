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
                new Permission { Id = 1, Entity = "product", Action = "read", Name = "product.read", Description = "Read products", CreatedAt = staticDate },
                new Permission { Id = 2, Entity = "product", Action = "create", Name = "product.create", Description = "Create products", CreatedAt = staticDate },
                new Permission { Id = 3, Entity = "product", Action = "update", Name = "product.update", Description = "Update products", CreatedAt = staticDate },
                new Permission { Id = 4, Entity = "product", Action = "delete", Name = "product.delete", Description = "Delete products", CreatedAt = staticDate },
                new Permission { Id = 5, Entity = "category", Action = "read", Name = "category.read", Description = "Read categories", CreatedAt = staticDate },
                new Permission { Id = 6, Entity = "category", Action = "create", Name = "category.create", Description = "Create categories", CreatedAt = staticDate },
                new Permission { Id = 7, Entity = "category", Action = "update", Name = "category.update", Description = "Update categories", CreatedAt = staticDate },
                new Permission { Id = 8, Entity = "category", Action = "delete", Name = "category.delete", Description = "Delete categories", CreatedAt = staticDate },
                new Permission { Id = 9, Entity = "user", Action = "read", Name = "user.read", Description = "Read users", CreatedAt = staticDate },
                new Permission { Id = 10, Entity = "user", Action = "update", Name = "user.update", Description = "Update users", CreatedAt = staticDate },
                new Permission { Id = 11, Entity = "user", Action = "delete", Name = "user.delete", Description = "Delete users", CreatedAt = staticDate },
                new Permission { Id = 12, Entity = "role", Action = "read", Name = "role.read", Description = "Read roles", CreatedAt = staticDate },
                new Permission { Id = 13, Entity = "role", Action = "create", Name = "role.create", Description = "Create roles", CreatedAt = staticDate },
                new Permission { Id = 14, Entity = "role", Action = "update", Name = "role.update", Description = "Update role permissions", CreatedAt = staticDate },
                new Permission { Id = 15, Entity = "role", Action = "delete", Name = "role.delete", Description = "Delete roles", CreatedAt = staticDate },
                new Permission { Id = 16, Entity = "permission", Action = "read", Name = "permission.read", Description = "Read permissions", CreatedAt = staticDate },
                new Permission { Id = 17, Entity = "permission", Action = "create", Name = "permission.create", Description = "Create permissions", CreatedAt = staticDate },
                new Permission { Id = 18, Entity = "permission", Action = "update", Name = "permission.update", Description = "Update permissions", CreatedAt = staticDate },
                new Permission { Id = 19, Entity = "permission", Action = "delete", Name = "permission.delete", Description = "Delete permissions", CreatedAt = staticDate },
                new Permission { Id = 20, Entity = "order", Action = "manage_read", Name = "order.manage.read", Description = "Admin: list and view orders", CreatedAt = staticDate },
                new Permission { Id = 21, Entity = "order", Action = "manage_update", Name = "order.manage.update", Description = "Admin: update order status", CreatedAt = staticDate },
                new Permission { Id = 22, Entity = "order", Action = "cancel", Name = "order.cancel", Description = "Cancel order", CreatedAt = staticDate },
                new Permission { Id = 23, Entity = "order", Action = "create", Name = "order.create", Description = "Create order", CreatedAt = staticDate },
                new Permission { Id = 24, Entity = "order", Action = "read", Name = "order.read", Description = "Read order", CreatedAt = staticDate },
                new Permission { Id = 25, Entity = "order", Action = "checkout", Name = "order.checkout", Description = "Checkout order", CreatedAt = staticDate }
    );
        }
    }
}
