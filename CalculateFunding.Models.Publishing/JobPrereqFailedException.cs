using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Publishing
{
    public class JobPrereqFailedException : Exception
    {
        public IEnumerable<string> Errors { get; private set; }

        public JobPrereqFailedException(string message, IEnumerable<string> errors) : base(message)
        {
            Errors = errors;
        }
    }
}
