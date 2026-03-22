using Ecommerce.Application.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.DTOs.Common;
using Ecommerce.Application.Extensions;
using Microsoft.AspNetCore.Authorization;

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

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(
    RefreshTokenRequestDto request)
        {
            var result = await _authService.RefreshTokenAsync(request);

            return OkResponse(result);
        }

        //[Authorize]
        //[HttpGet("me")]
        //public async Task<IActionResult> Me()
        //{
        //    var userId = User.GetUserId();
        //    var result = await _authService.GetMeAsync(userId);
        //    return OkResponse(result);
        //}

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var userId = User.GetUserId();
            var authHeader = Request.Headers["Authorization"].ToString();
            var accessToken = authHeader.Replace("Bearer ", "").Trim();

            if (string.IsNullOrEmpty(accessToken))
            {
                return BadRequest(new { message = "Invalid token" });
            }

            await _authService.LogoutAsync(userId, accessToken);
            return OkResponse("Logged out successfully");
        }
    }
}
