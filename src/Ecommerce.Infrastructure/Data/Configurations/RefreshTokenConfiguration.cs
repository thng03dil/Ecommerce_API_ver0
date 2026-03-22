using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ecommerce.Infrastructure.Data.Configurations
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("RefreshTokens");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.TokenHash)
                   .IsRequired()
                   .HasMaxLength(500);

            builder.HasIndex(x => x.TokenHash); 

            builder.Property(x => x.ExpiryDate)
                   .IsRequired();

            builder.Property(x => x.IsRevoked)
                   .IsRequired();

            builder.Property(x => x.SessionId)
                .IsRequired();

            builder.Property(x => x.FamilyId)
                .IsRequired();

            builder.Property(x => x.DeviceId)
                .IsRequired()
                .HasMaxLength(256);

            builder.Property(x => x.IpAddress)
                .HasMaxLength(100)
                .IsRequired(false);

            builder.Property(x => x.UserAgent)
                .HasMaxLength(500)
                .IsRequired(false);
        }
    }
}
