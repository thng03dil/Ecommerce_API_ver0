using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Data.Configurations
{
    public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
    {
        public void Configure(EntityTypeBuilder<OrderItem> builder)
        {
            builder.ToTable("OrderItems");

            builder.HasKey(od => od.Id);

            builder.Property(od => od.Quantity)
                .IsRequired();

            builder.Property(od => od.PriceAtPurchase)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            // Restrict to not delete Product when OrderItem is deleted
            builder.HasOne<Product>()
                .WithMany()
                .HasForeignKey(od => od.ProductId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
