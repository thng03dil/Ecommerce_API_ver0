using System.Net;

namespace Ecommerce.Application.Exceptions
{
    public class BadRequestException : BaseException
    {
        public BadRequestException(string message)
            : base((int)HttpStatusCode.BadRequest, message, "BAD_REQUEST")
        {
        }
    }
}
