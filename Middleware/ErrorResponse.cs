using Ecommerce_API.Exceptions;

namespace Ecommerce_API.Middleware
{
    public class ErrorResponse
    {
        public bool Success { get; set; } = false;
        public int StatusCode { get; set; }
        public string ErrorCode { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public List<ValidationError>? Errors { get; set; }

        public string? Path { get; set; }

        public string? TraceId { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
