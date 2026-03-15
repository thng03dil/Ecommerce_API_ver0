using Microsoft.AspNetCore.Http;
using System.Net;

namespace Ecommerce.Application.Exceptions
{
    public class NotFoundException : BaseException
    {
        public NotFoundException( string message) 
            : base((int)HttpStatusCode.NotFound, "NOT_FOUND",message)  
        { 
        }
    }
}
  