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

            builder.HasIndex(p => p.Name)
                .IsUnique();

            builder.Property(r => r.Description)
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

            builder.HasData(
                new Permission { Id = 1, Name = "product.view", Description = "View product list" },
                new Permission { Id = 2, Name = "product.viewbyid", Description = "View product details" },
                new Permission { Id = 3, Name = "product.create", Description = "Create new products" },
                new Permission { Id = 4, Name = "product.update", Description = "Update existing products" },
                new Permission { Id = 5, Name = "product.delete", Description = "Delete products" },

                new Permission { Id = 6, Name = "category.view", Description = "View category list" },
                new Permission { Id = 7, Name = "category.viewbyid", Description = "View category details" },
                new Permission { Id = 8, Name = "category.create", Description = "Create new categories" },
                new Permission { Id = 9, Name = "category.update", Description = "Update existing categories" },
                new Permission { Id = 10, Name = "categories.delete", Description = "Delete categories" },

                new Permission { Id = 11, Name = "user.view", Description = "View user list" },
                new Permission { Id = 12, Name = "user.viewbyid", Description = "View user details" },
                new Permission { Id = 13, Name = "user.update", Description = "Update user information" },
                new Permission { Id = 14, Name = "user.delete", Description = "Remove users" },

                new Permission { Id = 15, Name = "role.view", Description = "View role list" },
                new Permission { Id = 16, Name = "role.viewbyid", Description = "View role details" },
                new Permission { Id = 17, Name = "role.create", Description = "Create new roles" },
                new Permission { Id = 18, Name = "role.update", Description = "Update role permissions" },
                new Permission { Id = 19, Name = "role.delete", Description = "Delete roles" },

                new Permission { Id = 20, Name = "permission.view", Description = "View permission list" },
                new Permission { Id = 21, Name = "permission.viewbyid", Description = "View permission details" }
    );
        }
    }
}
