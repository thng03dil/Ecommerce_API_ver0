using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.DTOs.Common;
using Microsoft.AspNetCore.Identity.Data;

namespace Ecommerce.Application.Services.Interfaces
{
    public interface IAuthService
    {
        Task RegisterAsync(RegisterDto request);

        Task<AuthResponseDto> LoginAsync(LoginDto request);
        Task<AuthResponseDto> RefreshTokenAsync(RefreshTokenRequestDto request);

        /// <summary>
        /// RBAC check: does the user have the specified permission (e.g. "product.create")?
        /// </summary>
        Task<bool> HasPermissionAsync(int userId, string permission);

        /// <summary>
        /// Returns current user's profile (email, role, permissions) after login.
        /// </summary>
        Task<UserMeResponseDto> GetMeAsync(int userId);

        /// <summary>
        /// Server-side logout: revoke refresh token for the current user.
        /// </summary>
        Task LogoutAsync(int userId);
    }
}  