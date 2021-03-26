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

        public static void EnsureIsNotNullOrWhitespace(string literal,
            string message)
        {
            if (!string.IsNullOrWhiteSpace(literal))
            {
                return;
            }

            throw new NonRetriableException(message);
        }

        public static void EnsureIsNotNull(object item,
            string message)
        {
            if (item != null)
            {
                return;
            }

            throw new NonRetriableException(message);
        }

        public static void Ensure(bool condition,
            string message)
        {
            if (condition)
            {
                return;
            }
            
            throw new NonRetriableException(message);
        }
    }
}
