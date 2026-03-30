using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Ecommerce.Application.Exceptions
{
    public class OutOfStockException : BaseException
        {
        public OutOfStockException(string message)
         : base((int)HttpStatusCode.Conflict, "OUT_OF_STOCK", message)
        {
        }
    }
    }

