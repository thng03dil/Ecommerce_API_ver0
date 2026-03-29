namespace Ecommerce.Application.Common.Auth
{
    /// <summary>
    /// Cached in Redis at key auth:session:user:{uid}. Contains only the fields
    /// required for per-request session validation.
    /// </summary>
    public class UserSessionState
    {
        public Guid SessionId { get; set; }
        public int SessionVersion { get; set; }
        public string FingerprintHash { get; set; } = string.Empty;
    }
}
