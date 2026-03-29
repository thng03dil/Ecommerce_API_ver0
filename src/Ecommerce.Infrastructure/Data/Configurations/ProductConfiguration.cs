using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Data.Configurations
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

            builder.Property(x => x.CategoryId)
                .IsRequired();

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

            // Chỉ áp dụng cho bản ghi chưa xóa mềm — tránh trùng tên khi có nhiều request/ instance API.
            builder.HasIndex(x => x.Name)
                .IsUnique()
                .HasFilter("[IsDeleted] = 0");

            // No global query filter: OrderItem requires a consistent Product navigation for order history.
            // Exclude soft-deleted rows in ProductRepo / catalog queries with !p.IsDeleted.
        }
    }
}
