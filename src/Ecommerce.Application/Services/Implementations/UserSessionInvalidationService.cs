using Ecommerce.Application.Common.Auth;
using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Interfaces;

namespace Ecommerce.Application.Services.Implementations
{
    public class UserSessionInvalidationService : IUserSessionInvalidationService
    {
        private readonly IUserRepo _userRepo;
        private readonly ICacheService _cacheService;

        public UserSessionInvalidationService(IUserRepo userRepo, ICacheService cacheService)
        {
            _userRepo = userRepo;
            _cacheService = cacheService;
        }

        public async Task InvalidateAsync(int userId, CancellationToken cancellationToken = default)
        {
            var authLock = UserAuthLockRegistry.GetLock(userId);
            await authLock.WaitAsync(cancellationToken);
            try
            {
                var user = await _userRepo.GetByIdForUpdateAsync(userId);
                if (user == null)
                    return;

                user.SessionVersion += 1;
                user.CurrentSessionId = null;
                user.LastDeviceId = null;
                user.LastFingerprintHash = null;
                user.RefreshTokenHash = null;
                user.RefreshTokenExpiresAtUtc = null;

                await _userRepo.SaveChangesAsync();
                await _cacheService.RemoveAsync(CacheKeyGenerator.AuthSession(userId));
            }
            finally
            {
                authLock.Release();
            }
        }
    }
}
