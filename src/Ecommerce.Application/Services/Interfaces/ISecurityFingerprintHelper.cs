namespace Ecommerce.Application.Services.Interfaces
{
    /// <summary>
    /// Computes and validates security fingerprint (hash of DeviceId + IP).
    /// </summary>
    public interface ISecurityFingerprintHelper
    {
        string GetClientIpAddress();
        /// <summary>HTTP User-Agent for audit (null if no HttpContext or header).</summary>
        string? GetUserAgent();
        string ComputeFingerprint(string deviceId);
    }
}
