using System;

namespace CalculateFunding.Services.Calcs
{
    public class RefreshJobRunningException : ApplicationException
    {
        public RefreshJobRunningException() : base()
        {
        }

        public RefreshJobRunningException(string message) : base(message)
        {
        }

        public RefreshJobRunningException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
