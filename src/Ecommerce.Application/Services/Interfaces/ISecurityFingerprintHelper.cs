namespace Ecommerce.Application.Services.Interfaces
{
    public interface ISecurityFingerprintHelper
    {
        string GetClientIpAddress();
                string? GetUserAgent();
        string ComputeFingerprint(string deviceId);
        string ComputeDeviceBinding(string deviceId);
    }
}
