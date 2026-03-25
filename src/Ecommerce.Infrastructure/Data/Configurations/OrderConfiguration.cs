using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Enums;

namespace Ecommerce.Infrastructure.Data.Configurations
{
    public class OrderConfiguration : IEntityTypeConfiguration<Order>
    {
        public void Configure(EntityTypeBuilder<Order> builder)
        {
            // Tên bảng
            builder.ToTable("Orders");

            // Khóa chính
            builder.HasKey(o => o.Id);

            // Cấu hình các thuộc tính
            builder.Property(o => o.TotalAmount)
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            builder.Property(o => o.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            // QUAN TRỌNG: Lưu Enum dưới dạng String trong Database
            // Giúp bạn dễ dàng đọc hiểu data khi truy vấn trực tiếp bằng SQL
            builder.Property(o => o.Status)
                .HasConversion(
                    v => v.ToString(),
                    v => ParseOrderStatusSafe(v))
                .HasMaxLength(20)
                .IsRequired();

            // when delete order, delete all related order items
            builder.HasMany(o => o.OrderItems)
             .WithOne(i => i.Order)
             .HasForeignKey(i => i.OrderId)
             .OnDelete(DeleteBehavior.Cascade);
        }

        private static OrderStatus ParseOrderStatusSafe(string value) =>
            Enum.TryParse<OrderStatus>(value, ignoreCase: true, out var parsed)
                ? parsed
                : OrderStatus.Pending;
    }
}
