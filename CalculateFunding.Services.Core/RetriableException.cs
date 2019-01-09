using System;
using System.Runtime.Serialization;

namespace CalculateFunding.Services.Core
{
    public class RetriableException : ApplicationException
    {
        public RetriableException() { }

        public RetriableException(string message) : base(message) { }

        public RetriableException(string message, Exception innerException) : base(message, innerException) { }

        public RetriableException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
