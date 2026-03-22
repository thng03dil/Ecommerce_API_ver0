using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Ecommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController : ControllerBase
    {
        protected ILogger<BaseController> Logger => HttpContext.RequestServices.GetRequiredService<ILogger<BaseController>>();

        protected int CurrentUserId
        {
            get
            {
                var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(idClaim, out var id) ? id : 0;
            }
        }

        protected string CurrentUserRole => User?.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        protected IActionResult OkResponse<T>(T data, string message = "Success")
        {
            return Ok(new
            {
                success = true,
                message,
                data,
                timestamp = DateTime.UtcNow
            });
        }

        protected IActionResult ErrorResponseDto(string message, int statusCode = 400)
        {
            return StatusCode(statusCode, new
            {
                success = false,
                message,
                timestamp = DateTime.UtcNow
            });
        }
    }
}
