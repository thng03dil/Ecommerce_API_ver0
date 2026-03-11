using Ecommerce_API.Middleware;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Ecommerce_API.Exceptions
{
    public class ValidationException :BaseException
    {
        private const int DefaultStatusCode = 400;
        private const string DefaultErrorCode = "VALIDATION_ERROR";

        public ValidationException(List<ValidationError> errors)
            : base(DefaultStatusCode, DefaultErrorCode, "One or more validation errors occurred.", errors)
        {
            
        }

        public ValidationException(string field, string message)
            : base(DefaultStatusCode, DefaultErrorCode, "Validation failed.",
                new List<ValidationError> { 
                    new ValidationError { 
                        Field = field,
                        Message = message 
                    } 
                })
        {
        }
    }
}
