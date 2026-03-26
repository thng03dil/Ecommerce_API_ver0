using Ecommerce.Application.Common.Responses;
using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.DTOs.Common;
using Ecommerce.Application.Extensions;
using Ecommerce.Application.Services.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : BaseController
    {
        private readonly IAuthService _authService;
        public AuthController(IAuthService authService) 
        {
            _authService = authService;
        }
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            await _authService.RegisterAsync(dto);

            return OkResponse("Register success");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            var result = await _authService.LoginAsync(request);

            return OkResponse(result);
        }

        /// <summary>
        /// Trả về access token mới. Refresh token trong body phản hồi trùng request (không đổi) — client cứ lưu một RT cho tới khi hết hạn hoặc logout.
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(RefreshTokenRequestDto request)
        {
            var result = await _authService.RefreshTokenAsync(request);

            return OkResponse(result);
        }

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.GetUserId();
            var accessToken = await HttpContext.GetTokenAsync("access_token");

            if (string.IsNullOrEmpty(accessToken))
            {
                string? authHeader = Request.Headers["Authorization"];
                if (authHeader != null && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                {
                    accessToken = authHeader["Bearer ".Length..].Trim();
                }
            }
            if (string.IsNullOrEmpty(accessToken))
            {
                return BadRequest(ApiResponse<string>.ErrorResponse("Access token is missing"));
            }

            await _authService.LogoutAsync(userId, accessToken);
            return OkResponse("Logged out successfully");
        }
    }
}
