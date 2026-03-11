using Ecommerce_API.Middleware;

namespace Ecommerce_API.Exceptions
{
    public abstract class BaseException : Exception
    {
        public string ErrorCode { get; set; }
        public int StatusCode { get; set; }

        public List<ValidationError>? Errors { get; set; }

        protected BaseException(int statusCode, string errorCode, string message, List<ValidationError>? errors = null) : base(message)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            Errors = errors;
        }
    }
}
