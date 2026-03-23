using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Services.Interfaces;

namespace Ecommerce.Infrastructure.Services
{
    public class TokenBlacklistService : ITokenBlacklistService
    {
        private readonly ICacheService _cache;

        public TokenBlacklistService(ICacheService cache)
        {
            _cache = cache;
        }
        // check is blacklisted by jti hash, if exists in cache then it's blacklisted
        public async Task<bool> IsBlacklistedAsync(string jtiHash, CancellationToken cancellationToken = default)
        {
            var key = CacheKeyGenerator.BlacklistToken(jtiHash);
            var value = await _cache.GetAsync<int?>(key);
            return value.HasValue;
        }
        // blacklist token by jti hash, store a value in cache with the jti hash as key and set expiration to ttl
        public async Task BlacklistAsync(string jtiHash, TimeSpan ttl, CancellationToken cancellationToken = default)
        {
            if (ttl <= TimeSpan.Zero)
                return;

            var key = CacheKeyGenerator.BlacklistToken(jtiHash);
            await _cache.SetAsync(key, 1, ttl);
        }
    }
}
