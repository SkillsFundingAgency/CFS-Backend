using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class RowLoadResultBuilder : TestEntityBuilder
    {
        private string _identifier;
        private IdentifierFieldType? _identifierFieldType;
        private IEnumerable<(string, object)> _fields;

        public RowLoadResultBuilder WithIdentifierFieldType(IdentifierFieldType identifierFieldType)
        {
            _identifierFieldType = identifierFieldType;

            return this;
        }

        public RowLoadResultBuilder WithIdentifier(string identifier)
        {
            _identifier = identifier;

            return this;
        }
        
        public RowLoadResultBuilder WithFields(params (string, object)[] fields)
        {
            _fields = fields;

            return this;
        }
        
        public RowLoadResult Build()
        {
            return new RowLoadResult
            {
                Identifier = _identifier ?? NewRandomString(),
                IdentifierFieldType = _identifierFieldType.GetValueOrDefault(NewRandomEnum<IdentifierFieldType>()),
                Fields = _fields?.ToDictionary(_ => _.Item1, _ => _.Item2) ?? new Dictionary<string, object>()
            };
        }
    }
}