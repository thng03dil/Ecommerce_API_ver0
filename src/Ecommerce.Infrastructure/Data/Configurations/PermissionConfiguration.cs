using Microsoft.EntityFrameworkCore;
using Ecommerce.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Ecommerce.Domain.Constants;

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
            builder.HasData(
                new Permission { Id = 1, Name =Permissions.ViewProduct, Description = "View product list" },
                new Permission { Id = 2, Name =Permissions.ViewByIdProduct, Description = "View product details" },
                new Permission { Id = 3, Name =Permissions.CreateProduct, Description = "Create new products" },
                new Permission { Id = 4, Name =Permissions.UpdateProduct, Description = "Update existing products" },
                new Permission { Id = 5, Name =Permissions.DeleteProduct, Description = "Delete products" },

                new Permission { Id = 6, Name =Permissions.ViewCategory, Description = "View category list" },
                new Permission { Id = 7, Name =Permissions.ViewByIdCategory, Description = "View category details" },
                new Permission { Id = 8, Name =Permissions.CreateCategory, Description = "Create new categories" },
                new Permission { Id = 9, Name =Permissions.UpdateCategory, Description = "Update existing categories" },
                new Permission { Id = 10, Name =Permissions.DeleteCategory, Description = "Delete categories" },

                new Permission { Id = 11, Name =Permissions.ViewUser, Description = "View user list" },
                new Permission { Id = 12, Name =Permissions.ViewByIdUser, Description = "View user details" },
                new Permission { Id = 13, Name =Permissions.UpdateUser, Description = "Update user information" },
                new Permission { Id = 14, Name =Permissions.DeleteUser, Description = "Remove users" },

                new Permission { Id = 15, Name =Permissions.ViewRole, Description = "View role list" },
                new Permission { Id = 16, Name =Permissions.ViewByIdRole, Description = "View role details" },
                new Permission { Id = 17, Name =Permissions.CreateRole, Description = "Create new roles" },
                new Permission { Id = 18, Name =Permissions.UpdateRole, Description = "Update role permissions" },
                new Permission { Id = 19, Name =Permissions.DeleteRole, Description = "Delete roles" }
    );
        }
    }
}
