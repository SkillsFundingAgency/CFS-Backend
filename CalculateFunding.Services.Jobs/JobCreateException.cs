using System;
using System.Collections.Generic;

namespace CalculateFunding.Services.Jobs
{
    public class JobCreateException : Exception
    {
        public JobCreateException()
        {
        }

        public JobCreateException(string message) 
            : base(message)
        {
        }

        public JobCreateException(params JobCreateErrorDetails[] errorDetails)
        : this("Unable to create jobs")
        {
            Details = errorDetails;
        }

        public IEnumerable<JobCreateErrorDetails> Details { get;  }
    }
}