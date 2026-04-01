using Ecommerce.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ecommerce.Infrastructure.Data.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("Users");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(256);

            builder.HasIndex(x => x.Email)
                .IsUnique();

            builder.Property(x => x.PasswordHash)
                .IsRequired()
                .HasMaxLength(255);

            builder.Property(x => x.RoleId)
            .IsRequired();

            builder.Property(x => x.CreatedAt)
              .IsRequired()
              .HasDefaultValueSql("GETDATE()");

            builder.Property(x => x.UpdatedAt)
                .IsRequired(false);

            builder.Property(x => x.SessionVersion)
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.CurrentSessionId)
                .IsRequired(false);

            builder.Property(x => x.LastDeviceIdHash)
                .HasMaxLength(256)
                .IsRequired(false);

            builder.Property(x => x.LastFingerprintHash)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(x => x.RefreshTokenHash)
                .HasMaxLength(500)
                .IsRequired(false);

            builder.Property(x => x.RefreshTokenExpiresAtUtc)
                .IsRequired(false);

            builder.HasOne(u => u.Role)
                 .WithMany(r => r.Users)
                 .HasForeignKey(u => u.RoleId)
                 .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
