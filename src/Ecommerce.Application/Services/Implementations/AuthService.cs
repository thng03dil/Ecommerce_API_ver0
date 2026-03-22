using Ecommerce.Application.Common.Auth;
using Ecommerce.Application.Common.Caching;
using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.DTOs.Common;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Settings;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;

namespace Ecommerce.Application.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private const int MaxLoginFailures = 5;
        private static readonly TimeSpan LoginFailureWindow = TimeSpan.FromMinutes(15);

        private static readonly ConcurrentDictionary<int, SemaphoreSlim> _userAuthLocks = new();

        private readonly IUserRepo _userRepo;
        private readonly IRoleRepo _roleRepo;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;
        private readonly IPasswordHasher _passwordHasher;
        private readonly IRefreshTokenRepo _refreshTokenRepo;
        private readonly ICacheService _cacheService;
        private readonly IDeviceService _deviceService;
        private readonly ISecurityFingerprintHelper _fingerprint;
        private readonly ISessionValidationService _sessionValidation;
        private readonly ITokenBlacklistService _tokenBlacklist;

        public AuthService(
            IUserRepo userRepo,
            IRoleRepo roleRepo,
            IJwtService jwtService,
            IOptions<JwtSettings> jwtSettings,
            IPasswordHasher passwordHasher,
            IRefreshTokenRepo refreshTokenRepo,
            ICacheService cacheService,
            IDeviceService deviceService,
            ISecurityFingerprintHelper fingerprint,
            ISessionValidationService sessionValidation,
            ITokenBlacklistService tokenBlacklist)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _jwtService = jwtService;
            _jwtSettings = jwtSettings.Value;
            _passwordHasher = passwordHasher;
            _refreshTokenRepo = refreshTokenRepo;
            _cacheService = cacheService;
            _deviceService = deviceService;
            _fingerprint = fingerprint;
            _sessionValidation = sessionValidation;
            _tokenBlacklist = tokenBlacklist;
        }

        private static SemaphoreSlim GetUserAuthLock(int userId) =>
            _userAuthLocks.GetOrAdd(userId, _ => new SemaphoreSlim(1, 1));

        public async Task RegisterAsync(RegisterDto request)
        {
            var exist = await _userRepo.GetByEmailAsync(request.Email);

            if (exist != null) throw new ConflictException("Email already exists");

            var defaultRole = await _roleRepo.GetByNameRoleAsync("User");

            if (defaultRole == null)
                throw new Exception("System role configuration error");

            var user = new User
            {
                Email = request.Email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                RoleId = defaultRole.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepo.AddAsync(user);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto request)
        {
            var emailKey = request.Email.Trim().ToLowerInvariant();
            var failKey = CacheKeyGenerator.LoginFailure(emailKey);

            var userForCred = await _userRepo.GetByEmailAsync(request.Email);

            if (userForCred == null ||
                !_passwordHasher.Verify(request.Password, userForCred.PasswordHash))
            {
                var failCount = await _cacheService.GetAsync<int?>(failKey) ?? 0;
                failCount++;
                await _cacheService.SetAsync(failKey, failCount, LoginFailureWindow);
                if (failCount > MaxLoginFailures)
                    throw new TooManyRequestsException();

                throw new UnauthorizedException("Invalid email or password");
            }

            await _cacheService.RemoveAsync(failKey);

            var deviceId = _deviceService.GetDeviceId();
            if (string.IsNullOrWhiteSpace(deviceId))
                throw new UnauthorizedException("X-Device-Id header is required for login");

            var authLock = GetUserAuthLock(userForCred.Id);
            await authLock.WaitAsync();
            try
            {
                var user = await _userRepo.GetByIdForUpdateAsync(userForCred.Id);
                if (user == null)
                    throw new NotFoundException("User not found");

                await _refreshTokenRepo.RevokeAllForUserAsync(user.Id);

                var fingerprintHash = _fingerprint.ComputeFingerprint(deviceId);
                var sessionId = Guid.NewGuid();
                user.SessionVersion += 1;
                user.CurrentSessionId = sessionId;
                user.LastLoginIpHash = _fingerprint.GetClientIpAddress();
                user.LastDeviceId = deviceId;
                user.LastFingerprintHash = fingerprintHash;

                var familyId = Guid.NewGuid();
                var refreshPlain = _jwtService.GenerateRefreshToken();
                var refreshHash = _jwtService.HashToken(refreshPlain);
                var expiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

                var refreshEntity = new RefreshToken(
                    user.Id,
                    refreshHash,
                    expiry,
                    deviceId,
                    sessionId,
                    familyId);

                await _refreshTokenRepo.AddAsync(refreshEntity);
                await _userRepo.SaveChangesAsync();

                var accessToken = _jwtService.GenerateAccessToken(
                    user,
                    sessionId,
                    user.SessionVersion,
                    fingerprintHash);

                await CacheSessionAsync(
                    user.Id,
                    sessionId,
                    user.SessionVersion,
                    fingerprintHash,
                    deviceId);

                return new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = refreshPlain,
                    ExpiresIn = _jwtSettings.ExpiryMinutes * 60
                };
            }
            finally
            {
                authLock.Release();
            }
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request)
        {
            var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);

            if (!int.TryParse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
                throw new UnauthorizedException("Invalid token");

            var sid = principal.FindFirst("sid")?.Value;
            var sv = principal.FindFirst("sv")?.Value;
            var fp = principal.FindFirst("fp")?.Value;
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (!int.TryParse(sv, out var sessionVersionClaim))
                throw new UnauthorizedException("Invalid token");

            var deviceId = _deviceService.GetDeviceId();
            var currentFingerprint = _fingerprint.ComputeFingerprint(deviceId ?? string.Empty);

            await _sessionValidation.EnsureAccessTokenSessionValidAsync(
                userId,
                sid,
                sv,
                fp,
                currentFingerprint);

            if (!Guid.TryParse(sid, out var sessionId))
                throw new UnauthorizedException("Invalid token");

            var authLock = GetUserAuthLock(userId);
            await authLock.WaitAsync();
            try
            {
                var rtHash = _jwtService.HashToken(request.RefreshToken);
                var storedRt = await _refreshTokenRepo.GetByTokenHashAnyAsync(rtHash);

                if (storedRt == null)
                    throw new UnauthorizedException("Invalid refresh token");

                if (storedRt.IsRevoked)
                {
                    await InvalidateAllUserSessionsAsync(userId);
                    throw new UnauthorizedException("Refresh token reuse detected");
                }

                if (storedRt.UserId != userId || storedRt.SessionId != sessionId)
                    throw new UnauthorizedException("Invalid refresh token");

                if (storedRt.ExpiryDate <= DateTime.UtcNow)
                    throw new UnauthorizedException("Refresh token expired");

                if (!string.IsNullOrEmpty(jti))
                {
                    var remaining = _jwtService.GetAccessTokenRemainingLifetime(request.AccessToken);
                    if (remaining.HasValue && remaining.Value > TimeSpan.Zero)
                        await _tokenBlacklist.BlacklistAsync(_jwtService.HashToken(jti), remaining.Value);
                }

                await _cacheService.RemoveAsync(CacheKeyGenerator.AuthSession(userId, sessionVersionClaim));

                await _refreshTokenRepo.RevokeByIdAsync(storedRt.Id);

                var user = await _userRepo.GetByIdForUpdateAsync(userId);
                if (user == null)
                    throw new NotFoundException("User not found");

                user.LastLoginIpHash = _fingerprint.GetClientIpAddress();
                user.LastDeviceId = deviceId ?? string.Empty;
                user.LastFingerprintHash = currentFingerprint;

                var newRefreshPlain = _jwtService.GenerateRefreshToken();
                var newHash = _jwtService.HashToken(newRefreshPlain);
                var newExpiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

                var newRt = new RefreshToken(
                    user.Id,
                    newHash,
                    newExpiry,
                    deviceId ?? string.Empty,
                    sessionId,
                    storedRt.FamilyId);

                await _refreshTokenRepo.AddAsync(newRt);
                await _userRepo.SaveChangesAsync();

                var accessToken = _jwtService.GenerateAccessToken(
                    user,
                    sessionId,
                    user.SessionVersion,
                    currentFingerprint);

                await CacheSessionAsync(
                    user.Id,
                    sessionId,
                    user.SessionVersion,
                    currentFingerprint,
                    deviceId ?? string.Empty);

                return new AuthResponseDto
                {
                    AccessToken = accessToken,
                    RefreshToken = newRefreshPlain,
                    ExpiresIn = _jwtSettings.ExpiryMinutes * 60
                };
            }
            finally
            {
                authLock.Release();
            }
        }

        public async Task LogoutAsync(int userId, string accessToken)
        {
            if (!string.IsNullOrEmpty(accessToken))
            {
                try
                {
                    var principal = _jwtService.GetPrincipalFromExpiredToken(accessToken);
                    var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
                    if (!string.IsNullOrEmpty(jti))
                    {
                        var remaining = _jwtService.GetAccessTokenRemainingLifetime(accessToken);
                        if (remaining.HasValue && remaining.Value > TimeSpan.Zero)
                            await _tokenBlacklist.BlacklistAsync(_jwtService.HashToken(jti), remaining.Value);
                    }
                }
                catch
                {
                    // ignore malformed token on logout
                }
            }

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
            await _cacheService.RemoveAsync(CacheKeyGenerator.AuthSession(userId, oldSessionVersion));
        }

        public async Task<bool> HasPermissionAsync(int userId, string permission)
        {
            if (userId <= 0) return false;
            if (string.IsNullOrWhiteSpace(permission)) return false;

            var user = await _userRepo.GetByIdWithPermissionsAsync(userId);
            if (user?.Role?.RolePermissions == null) return false;

            var normalized = permission.Trim().ToLowerInvariant();
            return user.Role.RolePermissions.Any(rp => rp.Permission.Name == normalized);
        }

        public async Task<UserMeResponseDto> GetMeAsync(int userId)
        {
            if (userId <= 0) throw new UnauthorizedException("Unauthorized");

            var user = await _userRepo.GetByIdWithPermissionsAsync(userId);
            if (user == null) throw new NotFoundException("User not found");

            var permissions = user.Role?.RolePermissions?
                .Select(rp => rp.Permission.Name)
                .Distinct()
                .OrderBy(x => x)
                .ToList() ?? new List<string>();

            return new UserMeResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = user.Role?.Name ?? string.Empty,
                Permissions = permissions,
                CreatedAt = user.CreatedAt
            };
        }

        private async Task CacheSessionAsync(
            int userId,
            Guid sessionId,
            int sessionVersion,
            string fingerprintHash,
            string deviceId)
        {
            var state = new UserSessionState
            {
                SessionId = sessionId,
                SessionVersion = sessionVersion,
                FingerprintHash = fingerprintHash,
                DeviceId = deviceId,
                IpHash = _fingerprint.GetClientIpAddress()
            };

            await _cacheService.SetAsync(
                CacheKeyGenerator.AuthSession(userId, sessionVersion),
                state,
                TimeSpan.FromDays(_jwtSettings.RefreshTokenDays));
        }

        private async Task InvalidateAllUserSessionsAsync(int userId)
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
            await _cacheService.RemoveAsync(CacheKeyGenerator.AuthSession(userId, oldSessionVersion));
        }
    }
}
