using System;
using System.Runtime.Serialization;

namespace CalculateFunding.Services.Core
{
    public class NonRetriableException : ApplicationException
    {
        public NonRetriableException() { }

        public NonRetriableException(string message) : base(message) { }

        public NonRetriableException(string message, Exception innerException) : base(message, innerException) { }

        public NonRetriableException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
}
