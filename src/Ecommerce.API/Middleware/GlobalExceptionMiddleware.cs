using System.Net;
using System.Text.Json;
using Ecommerce.Application.Exceptions;
using Ecommerce.Application.Common.Responses;
namespace Ecommerce.API.Middleware
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred: {Message}", ex.Message); 

                await HandleExceptionAsync(context, ex);
            }
        }
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var statusCode = StatusCodes.Status500InternalServerError;
            var errorCode = "INTERNAL_SERVER_ERROR";

            if (exception is BaseException baseEx)
            {
                statusCode = baseEx.StatusCode;
                errorCode = baseEx.ErrorCode;
            }

            _logger.LogError(exception, exception.Message);

            var response = new ErrorResponse
            {
                StatusCode = statusCode,
                Success = false,
                ErrorCode = errorCode,
                Message = exception.Message,
                Path = context.Request.Path,
                TraceId = context.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response)
            );
        }
    }
}

