using Ecommerce.Application.Authorization;
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
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Ecommerce.Application.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private const int MaxLoginFailures = 5;
        private static readonly TimeSpan LoginFailureWindow = TimeSpan.FromMinutes(15);

        private readonly IUserRepo _userRepo;
        private readonly IRoleRepo _roleRepo;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ICacheService _cacheService;
        private readonly IDeviceService _deviceService;
        private readonly ISecurityFingerprintHelper _fingerprint;
        private readonly ITokenBlacklistService _tokenBlacklist;
        private readonly IUserSessionInvalidationService _sessionInvalidation;
        private readonly IRolePermissionService _rolePermissionService;
        private readonly IUnitOfWork _unitOfWork;

        public AuthService(
            IUserRepo userRepo,
            IRoleRepo roleRepo,
            IJwtService jwtService,
            IOptions<JwtSettings> jwtSettings,
            IPasswordHasher passwordHasher,
            ICacheService cacheService,
            IDeviceService deviceService,
            ISecurityFingerprintHelper fingerprint,
            ITokenBlacklistService tokenBlacklist,
            IUserSessionInvalidationService sessionInvalidation,
            IRolePermissionService rolePermissionService,
            IUnitOfWork unitOfWork)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _jwtService = jwtService;
            _jwtSettings = jwtSettings.Value;
            _passwordHasher = passwordHasher;
            _cacheService = cacheService;
            _deviceService = deviceService;
            _fingerprint = fingerprint;
            _tokenBlacklist = tokenBlacklist;
            _sessionInvalidation = sessionInvalidation;
            _rolePermissionService = rolePermissionService;
            _unitOfWork = unitOfWork;
        }

        public async Task RegisterAsync(RegisterDto request)
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var exist = await _userRepo.GetByEmailAsync(request.Email);
                if (exist != null) throw new ConflictException("Email already exists");

                var defaultRole = await _roleRepo.GetByNameRoleAsync("User");
                if (defaultRole == null)
                    throw new Exception("System role configuration error: default 'User' role is missing. Check database seed.");

                var user = new User
                {
                    Email = request.Email,
                    PasswordHash = _passwordHasher.Hash(request.Password),
                    RoleId = defaultRole.Id,
                    CreatedAt = DateTime.UtcNow
                };

                await _userRepo.AddAsync(user);
                return true;
            });
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto request)
        {
            // DeviceId: body first (Swagger/Postman), then header fallback (mobile/web)
            var deviceId = !string.IsNullOrWhiteSpace(request.DeviceId)
                ? request.DeviceId
                : _deviceService.GetDeviceId();

            if (string.IsNullOrWhiteSpace(deviceId))
                throw new UnauthorizedException("DeviceId is required (provide in body or X-Device-Id header)");

            var emailKey = request.Email.Trim().ToLowerInvariant();
            var failKey = CacheKeyGenerator.LoginFailure(emailKey);

            var userForCred = await _userRepo.GetByEmailAsync(request.Email);

            if (userForCred == null || !_passwordHasher.Verify(request.Password, userForCred.PasswordHash))
            {
                var failCount = await _cacheService.GetAsync<int?>(failKey) ?? 0;
                failCount++;
                await _cacheService.SetAsync(failKey, failCount, LoginFailureWindow);
                if (failCount > MaxLoginFailures)
                    throw new TooManyRequestsException("Too many login attempts. Try again later.");

                throw new UnauthorizedException("Invalid email or password");
            }

            await _cacheService.RemoveAsync(failKey);

            var authLock = UserAuthLockRegistry.GetLock(userForCred.Id);
            await authLock.WaitAsync();
            try
            {
                var (user, sessionId, refreshPlain, fingerprintHash) = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var u = await _userRepo.GetByIdForUpdateAsync(userForCred.Id);
                    if (u == null)
                        throw new NotFoundException("User not found");

                    var fp = _fingerprint.ComputeFingerprint(deviceId);
                    var sid = Guid.NewGuid();
                    u.SessionVersion += 1;
                    u.CurrentSessionId = sid;
                    u.LastDeviceIdHash = _fingerprint.ComputeDeviceBinding(deviceId);
                    u.LastFingerprintHash = fp;

                    var refreshPlaintext = _jwtService.GenerateRefreshToken();
                    var refreshHash = _jwtService.HashToken(refreshPlaintext);
                    u.RefreshTokenHash = refreshHash;
                    u.RefreshTokenExpiresAtUtc = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

                    return (u, sid, refreshPlaintext, fp);
                });

                var accessToken = _jwtService.GenerateAccessToken(user, sessionId, user.SessionVersion, fingerprintHash);

                await _cacheService.SetAsync(
                    CacheKeyGenerator.AuthSession(user.Id),
                    new UserSessionState
                    {
                        SessionId = sessionId,
                        SessionVersion = user.SessionVersion,
                        FingerprintHash = fingerprintHash
                    },
                    TimeSpan.FromDays(_jwtSettings.RefreshTokenDays));

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
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

            if (!Guid.TryParse(sid, out var sessionId))
                throw new UnauthorizedException("Invalid token");

            if (!int.TryParse(sv, out var sessionVersionClaim))
                throw new UnauthorizedException("Invalid token");

            // DeviceId comes from header for all non-login requests
            var deviceId = _deviceService.GetDeviceId();
            var currentFingerprint = _fingerprint.ComputeFingerprint(deviceId ?? string.Empty);

            var authLock = UserAuthLockRegistry.GetLock(userId);
            await authLock.WaitAsync();
            try
            {
                var user = await _unitOfWork.ExecuteInTransactionAsync(async () =>
                {
                    var u = await _userRepo.GetByIdForUpdateAsync(userId);
                    if (u == null)
                        throw new NotFoundException("User not found");

                    if (u.LastFingerprintHash != currentFingerprint)
                        throw new UnauthorizedException("Session fingerprint mismatch. Please log in again.");

                    if (u.SessionVersion != sessionVersionClaim)
                        throw new UnauthorizedException("Access token is outdated. Please log in again.");

                    if (u.CurrentSessionId != sessionId)
                        throw new UnauthorizedException("Invalid session.");

                    var rtHash = _jwtService.HashToken(request.RefreshToken);

                    if (u.RefreshTokenHash != rtHash)
                        throw new UnauthorizedException("Invalid refresh token");

                    if (u.RefreshTokenExpiresAtUtc == null || u.RefreshTokenExpiresAtUtc <= DateTime.UtcNow)
                        throw new UnauthorizedException("Refresh token expired. Please log in again.");

                    u.SessionVersion += 1;
                    u.LastDeviceIdHash = _fingerprint.ComputeDeviceBinding(deviceId ?? string.Empty);
                    u.LastFingerprintHash = currentFingerprint;

                    return u;
                });

                if (!string.IsNullOrEmpty(jti))
                {
                    var remaining = _jwtService.GetAccessTokenRemainingLifetime(request.AccessToken);
                    if (remaining.HasValue && remaining.Value > TimeSpan.Zero)
                        await _tokenBlacklist.BlacklistAsync(_jwtService.HashToken(jti), remaining.Value);
                }

                var newAccessToken = _jwtService.GenerateAccessToken(user, sessionId, user.SessionVersion, currentFingerprint);

                await _cacheService.SetAsync(
                    CacheKeyGenerator.AuthSession(userId),
                    new UserSessionState
                    {
                        SessionId = sessionId,
                        SessionVersion = user.SessionVersion,
                        FingerprintHash = currentFingerprint
                    },
                    user.RefreshTokenExpiresAtUtc!.Value - DateTime.UtcNow);

                return new AuthResponseDto
                {
                    AccessToken = newAccessToken,
                    RefreshToken = request.RefreshToken,
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

            await _sessionInvalidation.InvalidateAsync(userId);
        }

        public async Task<bool> HasPermissionAsync(int userId, string permission)
        {
            if (userId <= 0) return false;
            if (string.IsNullOrWhiteSpace(permission)) return false;

            var ctx = await _userRepo.GetRoleContextForAuthAsync(userId);
            if (ctx == null) return false;

            if (PermissionAuthConstants.IsSupremeRole(ctx.Value.RoleId, ctx.Value.RoleName))
                return true;

            return await _rolePermissionService.RoleHasPermissionAsync(ctx.Value.RoleId, permission);
        }

        public async Task<UserMeResponseDto> GetMeAsync(int userId)
        {
            if (userId <= 0) throw new UnauthorizedException("Unauthorized");

            var user = await _userRepo.GetByIdWithPermissionsAsync(userId);
            if (user == null) throw new NotFoundException("User not found");

            var role = user.Role;
            var fullAccess = role != null
                && PermissionAuthConstants.IsSupremeRole(role.Id, role.Name);

            List<string> permissions;
            if (fullAccess)
                permissions = new List<string>();
            else
                permissions = role?.RolePermissions?
                    .Where(rp => rp.Permission != null)
                    .Select(rp => rp.Permission!.Name)
                    .Distinct()
                    .OrderBy(x => x)
                    .ToList() ?? new List<string>();

            return new UserMeResponseDto
            {
                Id = user.Id,
                Email = user.Email,
                Role = role?.Name ?? string.Empty,
                FullAccess = fullAccess,
                Permissions = permissions,
                CreatedAt = user.CreatedAt
            };
        }
    }
}
