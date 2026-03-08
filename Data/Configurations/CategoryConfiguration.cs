using Ecommerce_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Reflection.Emit;

namespace Ecommerce_API.Data.Configurations
{
    public class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("Categories");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(x => x.Description)
                .HasMaxLength(500);

            builder.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(100);

            builder.HasIndex(x => x.Slug)
                .IsUnique();

            //create category data
            builder.HasData(
             new Category
             {
                 Id = 1,
                 Name = "Điện thoại",
                 Description = "Các loại smartphone mới nhất",
                 Slug = "dien-thoai",
                 IsDeleted = false,
                 IsActive = true,
                 CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
             },
             new Category
             {
                 Id = 2,
                 Name = "Laptop",
                 Description = "Máy tính xách tay làm việc và chơi game",
                 Slug = "laptop",
                 IsDeleted = false,
                 IsActive = true,
                 CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
             },
             new Category
             {
                 Id = 3,
                 Name = "Phụ kiện",
                 Description = "Tai nghe, sạc, cáp...",
                 Slug = "phu-kien",
                 IsDeleted = false,
                 IsActive = true,
                 CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
             }
         );
        }
    }
}
