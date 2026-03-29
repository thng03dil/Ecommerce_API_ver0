namespace Ecommerce.Domain.Common
{
    public record UserAuthState(
        int SessionVersion,
        Guid? CurrentSessionId,
        string? LastFingerprintHash,
        string? RefreshTokenHash,
        DateTime? RefreshTokenExpiresAtUtc);
}
