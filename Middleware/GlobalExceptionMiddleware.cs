using System.Net;
using System.Text.Json;
using Ecommerce_API.Exceptions;
namespace Ecommerce_API.Middleware
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
        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var response = new ErrorResponse
            {
                Success = false,
                Path = context.Request.Path,
                TraceId = context.TraceIdentifier,
                Timestamp = DateTime.UtcNow
            };
            //custom errors
            if (exception is BaseException baseEx)
            {
                context.Response.StatusCode = baseEx.StatusCode;
                response.StatusCode = baseEx.StatusCode;
                response.ErrorCode = baseEx.ErrorCode;
                response.Message = baseEx.Message;
                // auto set List<ValidationError> from ValidationException
                response.Errors = baseEx.Errors;

            }
            // server errors (NullReference, SQL Error...)
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.StatusCode = 500;
                response.ErrorCode = "INTERNAL_SERVER_ERROR";
                response.Message = "An unexpected error occurred on the server.";
            }

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            return context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}

