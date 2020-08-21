using System;
using System.Runtime.Serialization;

namespace CalculateFunding.Services.Jobs
{
    public class QueueJobException : Exception
    {
        public QueueJobException()
        {
        }

        protected QueueJobException(SerializationInfo info,
            StreamingContext context) 
            : base(info, context)
        {
        }

        public QueueJobException(string message) 
            : base(message)
        {
        }

        public QueueJobException(string message,
            Exception innerException) 
            : base(message, innerException)
        {
        }
    }
}