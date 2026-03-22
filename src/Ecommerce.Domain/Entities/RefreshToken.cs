namespace Ecommerce.Domain.Entities
{
    public class RefreshToken : BaseEntity
    {
        public int UserId { get; private set; }
        public User User { get; private set; } = null!;
        public string TokenHash { get; private set; } = null!;
        public DateTime ExpiryDate { get; private set; }
        public bool IsRevoked { get; private set; }
        public string DeviceId { get; private set; } = null!;
        public Guid SessionId { get; private set; }
        public Guid FamilyId { get; private set; }

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }

        private RefreshToken() { }

        public RefreshToken(
            int userId,
            string tokenHash,
            DateTime expiryDate,
            string deviceId,
            Guid sessionId,
            Guid familyId)
        {
            UserId = userId;
            TokenHash = tokenHash;
            ExpiryDate = expiryDate;
            DeviceId = deviceId;
            SessionId = sessionId;
            FamilyId = familyId;
            IsRevoked = false;
        }

        public void Revoke()
        {
            IsRevoked = true;
        }
    }
}
