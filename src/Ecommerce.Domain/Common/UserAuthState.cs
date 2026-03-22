namespace Ecommerce.Domain.Common
{
    public record UserAuthState(
        int SessionVersion,
        Guid? CurrentSessionId,
        string? LastLoginIpHash,
        string? LastDeviceId,
        string? LastFingerprintHash);
}
