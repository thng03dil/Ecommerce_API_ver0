using System.Net;

namespace Ecommerce_API.Exceptions
{
    public class ConflictException : BaseException
    {
        public ConflictException( string message)
           : base((int)HttpStatusCode.Conflict, "CONFLICT_ERROR", message)
        {

        }
    }
}
