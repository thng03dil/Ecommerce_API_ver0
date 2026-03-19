
using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.DTOs.Common;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Services.Interfaces;
using Ecommerce.Domain.Common.Settings;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Microsoft.Extensions.Options;
using System.Security.Claims;

namespace Ecommerce.Application.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepo _userRepo;
        private readonly IRoleRepo _roleRepo;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;
        private readonly IPasswordHasher _passwordHasher;
        public AuthService(
            IUserRepo userRepo,
            IRoleRepo roleRepo,
            IJwtService jwtService,
            IOptions<JwtSettings> jwtSettings,
            IPasswordHasher passwordHasher
            )
        {
            _userRepo = userRepo; 
            _roleRepo = roleRepo;
            _jwtService = jwtService;
            _jwtSettings = jwtSettings.Value;
            _passwordHasher = passwordHasher;
        } 

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

            var user = await _userRepo.GetByEmailAsync(request.Email);

            if (user == null ||
                !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedException("Invalid email or password");
            }

            var userWithPermissions = await _userRepo.GetByIdWithPermissionsAsync(user.Id);

            if (userWithPermissions == null)
            {
                throw new NotFoundException("User not found");
            }


            var accessToken = _jwtService.GenerateAccessToken(userWithPermissions);
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

            var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);

            var email = principal.FindFirst(ClaimTypes.Email)?.Value;

            if (email == null)
                throw new UnauthorizedException("Invalid token");
            
            var idClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(idClaim, out var userId))
            {
                throw new UnauthorizedException("Invalid token");
            }

            var user = await _userRepo.GetByIdWithPermissionsAsync(userId);

            if (user == null) throw new NotFoundException("User not found");
            
            if (user.RefreshToken != request.RefreshToken)
            {
                throw new UnauthorizedException("Invalid refresh token");
            }

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                throw new UnauthorizedException("Refresh token expired");
            }


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
