using System.Net;

namespace Ecommerce.Application.Exceptions
{
    public class BusinessException : BaseException
    {
        public BusinessException( string message)
            : base((int)HttpStatusCode.BadRequest, "BUSINESS_ERROR", message)
        {

        }
    }
}
  