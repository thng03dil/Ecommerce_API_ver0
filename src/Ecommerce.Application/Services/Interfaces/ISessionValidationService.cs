namespace Ecommerce.Application.Services.Interfaces
{
    public interface ISessionValidationService
    {
        /// <summary>
        /// Ensures access token session claims match Redis/DB and current request fingerprint.
        /// Redis first; fallback to DB if Redis unavailable.
        /// </summary>
        Task EnsureAccessTokenSessionValidAsync(
            int userId,
            string? sidClaim,
            string? svClaim,
            string? fpClaim,
            string currentFingerprint,
            CancellationToken cancellationToken = default);
    }
}
