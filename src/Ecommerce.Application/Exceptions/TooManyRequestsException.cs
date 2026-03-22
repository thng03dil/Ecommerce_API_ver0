using Microsoft.AspNetCore.Http;

namespace Ecommerce.Application.Exceptions
{
    public class TooManyRequestsException : BaseException
    {
        public TooManyRequestsException(string message = "Too many requests. Try again later.")
            : base(StatusCodes.Status429TooManyRequests, message, "TOO_MANY_REQUESTS")
        {
        }
    }
}
