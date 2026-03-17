
using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.DTOs.Common;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Settings;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Linq;

namespace Ecommerce.Application.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepo _userRepo;
        private readonly IRoleRepo _roleRepo;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;
        private readonly IPasswordHasher _passwordHasher;
        private readonly ILogger<AuthService> _logger;
        public AuthService(
            IUserRepo userRepo,
            IRoleRepo roleRepo,
            IJwtService jwtService,
            IOptions<JwtSettings> jwtSettings,
            IPasswordHasher passwordHasher,
            ILogger<AuthService> logger
            )
        {
            _userRepo = userRepo; 
            _roleRepo = roleRepo;
            _jwtService = jwtService;
            _jwtSettings = jwtSettings.Value;
            _passwordHasher = passwordHasher;
            _logger = logger;
        } 

        public async Task RegisterAsync(RegisterDto request) 
        {
            _logger.LogInformation("Register attempt for {Email}", request.Email);
            var exist = await _userRepo.GetByEmailAsync(request.Email);

            if (exist != null)
            {
                _logger.LogWarning("Registration failed: Email {Email} already exists", request.Email);
                throw new ConflictException("Email already exists");
            }

            var defaultRole = await _roleRepo.GetByNameRoleAsync("User");
            if (defaultRole == null)
            {
                _logger.LogError("Default Role 'User' not found in database.");
                throw new Exception("System role configuration error");
            }

            var user = new User
            {
                Email = request.Email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                RoleId = defaultRole.Id,
                CreatedAt = DateTime.UtcNow
            };

            await _userRepo.AddAsync(user);

            _logger.LogInformation("User registered successfully {Email}", request.Email);
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto request)
        {
            _logger.LogInformation("User login attempt: {Email}", request.Email);
            var user = await _userRepo.GetByEmailAsync(request.Email);

            if (user == null ||
                !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Login failed for {Email}", request.Email);
                throw new UnauthorizedException("Invalid email or password");
            }

            _logger.LogInformation("User login success: {Email}", request.Email);

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime =
                DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

            await _userRepo.UpdateAsync(user);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresIn = _jwtSettings.ExpiryMinutes * 60
            };
        }

        public async Task<AuthResponseDto> RefreshTokenAsync(
            RefreshTokenRequestDto request)
        {
            _logger.LogInformation("Refresh token attempt");

            var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);

            var email = principal.FindFirst(ClaimTypes.Email)?.Value;

            if (email == null)
            {
                _logger.LogWarning("Refresh token failed - email not found in token");
                throw new UnauthorizedException("Invalid token");
            }
            var user = await _userRepo.GetByEmailAsync(email);

            if (user == null)
            {
                _logger.LogWarning("Refresh token failed - user not found {Email}", email);
                throw new NotFoundException("User not found");
            }
            if (user.RefreshToken != request.RefreshToken)
            {
                _logger.LogWarning("Refresh token mismatch for {Email}", email);
                throw new UnauthorizedException("Invalid refresh token");
            }
            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                _logger.LogWarning("Refresh token mismatch for {Email}", email);
                throw new UnauthorizedException("Refresh token expired");
            }


            _logger.LogInformation("Refresh token success for {Email}", email);

            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime =
                DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays);

            await _userRepo.UpdateAsync(user);

            return new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                ExpiresIn = _jwtSettings.ExpiryMinutes * 60
            };
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

        public async Task LogoutAsync(int userId)
        {
            if (userId <= 0) throw new UnauthorizedException("Unauthorized");

            var user = await _userRepo.GetByIdForUpdateAsync(userId);
            if (user == null) throw new NotFoundException("User not found");

         
            user.RefreshToken = null;
            user.RefreshTokenExpiryTime = null;
            user.UpdatedAt = DateTime.UtcNow;

            await _userRepo.UpdateAsync(user);
        }
    }
}
