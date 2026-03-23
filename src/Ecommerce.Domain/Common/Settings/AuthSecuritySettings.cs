namespace Ecommerce.Domain.Common.Settings
{
    /// Security settings for fingerprinting. FingerprintSecret must be configured via User Secrets or environment (AuthSecurity__FingerprintSecret).
    public class AuthSecuritySettings
    {
        public string FingerprintSecret { get; set; } = string.Empty;
    }
}
