using System;
using System.Runtime.Serialization;

namespace CalculateFunding.Services.Core
{
    public class EntityNotFoundException : ApplicationException
    {
        public EntityNotFoundException() { }

        public EntityNotFoundException(string message) : base(message){}

        public EntityNotFoundException(string message, Exception innerException) : base(message, innerException){}
        public EntityNotFoundException(string message, string entity) : base(message) { Entity = entity; }

        public EntityNotFoundException(string message, Exception innerException, string entity) : base(message, innerException) { Entity = entity; }
       
        public EntityNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public string Entity { get; private set; }
    }
}
