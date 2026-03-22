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
                await HandleExceptionAsync(context, ex);
            }
        }
        private async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning("The response has already started, the exception middleware will not be executed.");
                return;
            }
            var statusCode = StatusCodes.Status500InternalServerError;
            var errorCode = "INTERNAL_SERVER_ERROR";
            var message = "An unexpected error occurred.";

            if (exception is BaseException baseEx)
            {
                statusCode = baseEx.StatusCode;
                errorCode = baseEx.ErrorCode;
                message = baseEx.Message;
            }

            _logger.LogError(
                 exception,
                 "Request failed {Method} {Path} TraceId:{TraceId}",
                 context.Request.Method,
                 context.Request.Path,
                 context.TraceIdentifier
             );

            var response = new ErrorResponseDto
            {
                StatusCode = statusCode,
                Success = false,
                ErrorCode = errorCode,
                Message = message,
                Path = context.Request.Path,
                TraceId = context.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            };

            context.Response.StatusCode = statusCode;
            context.Response.ContentType = "application/json";

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }; 

            await context.Response.WriteAsync(
                JsonSerializer.Serialize(response,options)
            );
        }
    }
}

