using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Ecommerce.Application.Exceptions
{
    public class ConflictException : BaseException
    {
        public ConflictException(string message)
            : base((int)HttpStatusCode.Conflict, message, "CONFLICT_ERROR")
        {

        }
    }
}
