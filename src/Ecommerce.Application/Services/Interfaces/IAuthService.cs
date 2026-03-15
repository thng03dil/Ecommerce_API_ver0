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
    }
}  