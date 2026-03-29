using Ecommerce.Application.Common.Auth;
using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Interfaces;

namespace Ecommerce.Application.Services.Implementations
{
    public class SessionValidationService : ISessionValidationService
    {
        private readonly ICacheService _cache;
        private readonly IUserRepo _userRepo;

        public SessionValidationService(ICacheService cache, IUserRepo userRepo)
        {
            _cache = cache;
            _userRepo = userRepo;
        }

        public async Task EnsureAccessTokenSessionValidAsync(
            int userId,
            string? sidClaim,
            string? svClaim,
            string? fpClaim,
            string currentFingerprint,
            CancellationToken cancellationToken = default)
        {
            if (!Guid.TryParse(sidClaim, out var sid)
                || !int.TryParse(svClaim, out var sv)
                || string.IsNullOrEmpty(fpClaim))
            {
                throw new UnauthorizedException("Invalid token session");
            }

            if (currentFingerprint != fpClaim)
                throw new UnauthorizedException("Invalid session (fingerprint mismatch)");

            var redisKey = CacheKeyGenerator.AuthSession(userId);
            var cached = await _cache.GetAsync<UserSessionState>(redisKey);

            if (cached != null)
            {
                // sid and fp must match exactly; sv in JWT must not be older than Redis sv
                if (cached.SessionId != sid || cached.FingerprintHash != fpClaim)
                    throw new UnauthorizedException("Invalid session");

                if (sv < cached.SessionVersion)
                    throw new UnauthorizedException("Access token is outdated. Please refresh.");

                return;
            }

            // Redis miss: hydrate from DB only when a valid session exists
            var db = await _userRepo.GetUserAuthStateAsync(userId);
            if (db == null)
                throw new UnauthorizedException("Invalid session");

            // Session is considered dead if RT was cleared (logout / invalidate)
            if (string.IsNullOrEmpty(db.RefreshTokenHash)
                || db.RefreshTokenExpiresAtUtc == null
                || db.RefreshTokenExpiresAtUtc <= DateTime.UtcNow)
            {
                throw new UnauthorizedException("Session expired. Please log in again.");
            }

            if (sv < db.SessionVersion)
                throw new UnauthorizedException("Access token is outdated. Please refresh.");

            if (db.CurrentSessionId != sid || db.SessionVersion != sv || db.LastFingerprintHash != fpClaim)
                throw new UnauthorizedException("Invalid session");

            // Re-hydrate Redis for subsequent requests
            var ttl = db.RefreshTokenExpiresAtUtc.Value - DateTime.UtcNow;
            await _cache.SetAsync(redisKey, new UserSessionState
            {
                SessionId = sid,
                SessionVersion = sv,
                FingerprintHash = fpClaim
            }, ttl);
        }
    }
}
