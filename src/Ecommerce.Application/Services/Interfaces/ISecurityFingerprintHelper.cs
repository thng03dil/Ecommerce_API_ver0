namespace Ecommerce.Application.Services.Interfaces
{
    /// <summary>
    /// Computes and validates security fingerprint (hash of DeviceId + IP).
    /// </summary>
    public interface ISecurityFingerprintHelper
    {
        string GetClientIpAddress();
        string ComputeFingerprint(string deviceId);
    }
}
