using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Repositories.Common.Search
{
    public class FailedToQuerySearchException : ApplicationException
    {
        public FailedToQuerySearchException(string message, Exception innerException) 
            : base(message, innerException)
        {

        }
    }
}
