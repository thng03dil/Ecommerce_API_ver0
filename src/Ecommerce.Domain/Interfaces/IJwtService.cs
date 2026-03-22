using Ecommerce.Domain.Entities;
using System.Security.Claims;

namespace Ecommerce.Domain.Interfaces
{
    public interface IJwtService
    {
        string GenerateAccessToken(
            User user,
            Guid sessionId,
            int sessionVersion,
            string fingerprint);

        string GenerateRefreshToken();

        ClaimsPrincipal GetPrincipalFromExpiredToken(string token);

        string HashToken(string token);

        /// <summary>Returns remaining lifetime until token expiry, or null if token cannot be read.</summary>
        TimeSpan? GetAccessTokenRemainingLifetime(string token);
    }
}
