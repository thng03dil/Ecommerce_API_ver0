
using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.DTOs.Common;
using Ecommerce.Application.Exceptions;
using Ecommerce.Domain.Entities;
using Ecommerce.Domain.Interfaces;
using Ecommerce.Application.Services.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using Ecommerce.Domain.Common.Settings;

namespace Ecommerce.Application.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepo _userRepo;
        private readonly IJwtService _jwtService;
        private readonly JwtSettings _jwtSettings;
        private readonly IPasswordHasher _passwordHasher;
        public AuthService(
            IUserRepo userRepo,
            IJwtService jwtService,
            IOptions<JwtSettings> jwtSettings,
            IPasswordHasher passwordHasher)
        {
            _userRepo = userRepo;
            _jwtService = jwtService;
            _jwtSettings = jwtSettings.Value;
            _passwordHasher = passwordHasher;

        } 

        public async Task RegisterAsync(RegisterDto request) 
        {
            var exist = await _userRepo.GetByEmailAsync(request.Email);

            if (exist != null)
                throw new Exception("Email already exists");

            var user = new User
            {
                Email = request.Email,
                PasswordHash = _passwordHasher.Hash(request.Password),
                Role = "User"
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto request)
        {
            var user = await _userRepo.GetByEmailAsync(request.Email);

            if (user == null ||
                !_passwordHasher.Verify(request.Password, user.PasswordHash))
            {
                throw new UnauthorizedException("Invalid email or password");
            }

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
            var principal = _jwtService.GetPrincipalFromExpiredToken(request.AccessToken);

            var email = principal.FindFirst(ClaimTypes.Email)?.Value;

            if (email == null)
                throw new UnauthorizedException("Invalid token");

            var user = await _userRepo.GetByEmailAsync(email);

            if (user == null)
                throw new NotFoundException("User not found");

            if (user.RefreshToken != request.RefreshToken)
                throw new UnauthorizedException("Invalid refresh token");

            if (user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                throw new UnauthorizedException("Refresh token expired");

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
    }
}
