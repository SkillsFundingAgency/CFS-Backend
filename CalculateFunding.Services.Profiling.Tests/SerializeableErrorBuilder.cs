using System.Collections.Generic;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Services.Profiling.Tests
{
    public class SerializeableErrorBuilder : TestEntityBuilder
    {
        private readonly ICollection<KeyValuePair<string, object>> _errors 
            = new List<KeyValuePair<string, object>>();

        public SerializeableErrorBuilder WithError(string name,
            object value)
        {
            _errors.Add(new KeyValuePair<string, object>(name, new [] { value  }));

            return this;
        }
        
        public SerializableError Build()
        {
            SerializableError serializableError = new SerializableError();

            foreach (KeyValuePair<string,object> error in _errors)
            {
                serializableError.Add(error.Key, error.Value);
            }
            
            return serializableError;
        }
    }
}