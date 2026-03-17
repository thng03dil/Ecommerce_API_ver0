using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public abstract class BaseController : ControllerBase
    {
        // Lazy-resolved logger to avoid polluting child controllers' constructors
        protected ILogger<BaseController> Logger => HttpContext.RequestServices.GetRequiredService<ILogger<BaseController>>();

        // Current user id (stored as int in JWT NameIdentifier claim)
        protected int CurrentUserId
        {
            get
            {
                var idClaim = User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                return int.TryParse(idClaim, out var id) ? id : 0;
            }
        }

        // Current user role from JWT Role claim
        protected string CurrentUserRole => User?.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty;

        // Standardized success response
        protected IActionResult ApiSuccess<T>(T data, string message = "Success")
        {
            return Ok(new
            {
                success = true,
                message,
                data,
                timestamp = DateTime.UtcNow
            });
        }

        // Standardized error response
        protected IActionResult ApiError(string message, int statusCode = 400)
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
