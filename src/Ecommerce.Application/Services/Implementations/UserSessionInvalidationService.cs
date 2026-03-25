using Ecommerce.Application.Common.Auth;
using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Interfaces;

namespace Ecommerce.Application.Services.Implementations
{
    public class UserSessionInvalidationService : IUserSessionInvalidationService
    {
        private readonly IRefreshTokenRepo _refreshTokenRepo;
        private readonly IUserRepo _userRepo;
        private readonly ICacheService _cacheService;

        public UserSessionInvalidationService(
            IRefreshTokenRepo refreshTokenRepo,
            IUserRepo userRepo,
            ICacheService cacheService)
        {
            _refreshTokenRepo = refreshTokenRepo;
            _userRepo = userRepo;
            _cacheService = cacheService;
        }

        public async Task InvalidateAsync(int userId, CancellationToken cancellationToken = default)
        {
            var authLock = UserAuthLockRegistry.GetLock(userId);
            await authLock.WaitAsync(cancellationToken);
            try
            {
                await _refreshTokenRepo.RevokeAllForUserAsync(userId);

                var user = await _userRepo.GetByIdForUpdateAsync(userId);
                if (user == null)
                    return;

                var oldSessionVersion = user.SessionVersion;
                user.SessionVersion += 1;
                user.CurrentSessionId = null;
                user.LastLoginIpHash = null;
                user.LastDeviceId = null;

                await _userRepo.SaveChangesAsync();
                await _cacheService.RemoveByPrefixAsync(CacheKeyGenerator.AuthSessionUserPrefix(userId));
            }
            finally
            {
                authLock.Release();
            }
        }
    }
}
