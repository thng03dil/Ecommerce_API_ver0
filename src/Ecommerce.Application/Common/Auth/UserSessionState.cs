namespace Ecommerce.Application.Common.Auth
{
    public class UserSessionState
    {
        public Guid SessionId { get; set; }
        public int SessionVersion { get; set; }
        public string FingerprintHash { get; set; } = string.Empty;
        public string DeviceId { get; set; } = string.Empty;
        public string IpHash { get; set; } = string.Empty;
    }
}
