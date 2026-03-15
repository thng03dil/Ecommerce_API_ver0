using System.Net;

namespace Ecommerce.Application.Exceptions
{
    public class ForbiddenException : BaseException
    {
        public ForbiddenException(string message)
            : base((int)HttpStatusCode.Forbidden, "FORBIDDEN", message )
        {
        }
    }
}
  