using Ecommerce_API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce_API.Data.Configurations
{
    public class ProductConfiguration : IEntityTypeConfiguration<Product>
    {
        public void Configure(EntityTypeBuilder<Product> builder)
        {
            builder.ToTable("Products");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(150);

            builder.Property(x => x.Description)
                .HasMaxLength(1000);

            builder.Property(x => x.Price)
                .HasColumnType("decimal(18,2)");

            builder.Property(x => x.Stock)
                .IsRequired()
                .HasColumnType("int")       
                .HasDefaultValue(0);
            builder.ToTable(t => t.HasCheckConstraint(
                "CK_Product_Stock_Min", 
                "[Stock] >= 0"
                ));

            builder.Property(x => x.CreatedAt)
              .IsRequired()
              .HasDefaultValueSql("GETDATE()");

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.HasOne(x => x.Category)
                .WithMany(x => x.Products)
                .HasForeignKey(x => x.CategoryId);
            builder.HasQueryFilter(p => !p.IsDeleted);

            //create product data
            builder.HasData(
        new Product
        {
            Id = 1,
            Name = "iPhone 15 Pro",
            Description = "Chip A17 Pro mạnh mẽ",
            Price = 25000000m,
            Stock = 20,
            CategoryId = 1,
            IsDeleted = false,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new Product
        {
            Id = 2,
            Name = "Samsung Galaxy S24",
            Description = "Flagship AI 2024",
            Price = 22000000m,
            Stock = 15,
            CategoryId = 1,
            IsDeleted = false,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new Product
        {
            Id = 3,
            Name = "MacBook Air M2",
            Description = "Siêu mỏng nhẹ",
            Price = 28000000m,
            Stock = 10,
            CategoryId = 2,
            IsDeleted = false,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new Product
        {
            Id = 4,
            Name = "Dell XPS 13",
            Description = "Màn hình vô cực",
            Price = 32000000m,
            Stock = 8,
            CategoryId = 2,
            IsDeleted = false,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new Product
        {
            Id = 5,
            Name = "AirPods Pro 2",
            Description = "Chống ồn chủ động",
            Price = 6000000m,
            Stock = 30,
            CategoryId = 3,
            IsDeleted = false,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        },
        new Product
        {
            Id = 6,
            Name = "Sạc nhanh 65W",
            Description = "Cổng Type-C tiện lợi",
            Price = 500000m,
            Stock = 50,
            CategoryId = 3,
            IsDeleted = false,
            CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        }
    );
        }
    }
}
