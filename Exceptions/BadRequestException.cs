using System.Net;

namespace Ecommerce_API.Exceptions
{
    public class BadRequestException : BaseException
    {
        public BadRequestException( string message)
            : base((int)HttpStatusCode.BadRequest, "BAD_REQUEST", message)
        {

        }
    }
}
