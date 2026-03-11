using System.Net;

namespace Ecommerce_API.Exceptions
{
    public class NotFoundException : BaseException
    {
        public NotFoundException( string message) 
            : base((int)HttpStatusCode.NotFound, "NOT_FOUND",message)  
        { 
        }
    }
}
