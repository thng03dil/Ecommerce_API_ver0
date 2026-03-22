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

        public string? LastLoginIpHash { get; set; }

        public string? LastDeviceId { get; set; }

        public string? LastFingerprintHash { get; set; }

        public ICollection<RefreshToken> RefreshTokens { get; private set; }
             = new List<RefreshToken>();

        public void AddRefreshToken(RefreshToken token)
        {
            RefreshTokens.Add(token);
        }

        public void RevokeAllTokens()
        {
            foreach (var token in RefreshTokens)
            {
                token.Revoke();
            }
        }
    }
}

