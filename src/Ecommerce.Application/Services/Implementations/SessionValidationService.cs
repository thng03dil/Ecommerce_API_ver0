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
        private readonly ISecurityFingerprintHelper _fingerprint;

        public SessionValidationService(
            ICacheService cache,
            IUserRepo userRepo,
            ISecurityFingerprintHelper fingerprint)
        {
            _cache = cache;
            _userRepo = userRepo;
            _fingerprint = fingerprint;
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

            var redisKey = CacheKeyGenerator.AuthSession(userId, sv);
            var cached = await _cache.GetAsync<UserSessionState>(redisKey);

            if (cached != null)
            {
                if (cached.SessionId != sid
                    || cached.SessionVersion != sv
                    || cached.FingerprintHash != fpClaim)
                {
                    throw new UnauthorizedException("Invalid session");
                }

                return;
            }

            var db = await _userRepo.GetUserAuthStateAsync(userId);
            if (db == null)
                throw new UnauthorizedException("Invalid session");

            if (db.CurrentSessionId != sid
                || db.SessionVersion != sv
                || db.LastFingerprintHash != fpClaim)
            {
                throw new UnauthorizedException("Invalid session");
            }
        }
    }
}
