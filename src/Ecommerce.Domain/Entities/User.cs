namespace Ecommerce.Domain.Entities
{
    public class User : BaseEntity
    {
        public string Email { get; set; } = string.Empty;

        public string PasswordHash { get; set; } = string.Empty;

        public int RoleId { get; set; }

        public Role Role { get; set; } = null!;

        public int SessionVersion { get; set; }

        public Guid? CurrentSessionId { get; set; }

        public string? LastFingerprintHash { get; set; }

        public string? LastDeviceId { get; set; }

        /// <summary>HMAC-SHA256 hash of the active refresh token plaintext.</summary>
        public string? RefreshTokenHash { get; set; }

        /// <summary>UTC expiry of the active refresh token. Null = no active session.</summary>
        public DateTime? RefreshTokenExpiresAtUtc { get; set; }
    }
}

