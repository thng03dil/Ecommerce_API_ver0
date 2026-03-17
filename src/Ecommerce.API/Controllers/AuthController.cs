using Azure.Core;
using Ecommerce.Application.Services.Interfaces;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Ecommerce.Application.DTOs.Auth;
using Ecommerce.Application.DTOs.Common;

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

            return ApiSuccess("Register success");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDto request)
        {
            var result = await _authService.LoginAsync(request);

            return ApiSuccess(result);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh(
    RefreshTokenRequestDto request)
        {
            var result = await _authService.RefreshTokenAsync(request);

            return ApiSuccess(result);
        }
    }
}
