using Ecommerce.Application.Exceptions;

namespace Ecommerce.Application.Common.Responses

{
    public class ErrorResponseDto
    {
        public int StatusCode { get; set; }

        public bool Success { get; set; } = false;
        public string ErrorCode { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string? Path { get; set; }

        public string? TraceId { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public object? Errors { get; set; }
    }
}
  