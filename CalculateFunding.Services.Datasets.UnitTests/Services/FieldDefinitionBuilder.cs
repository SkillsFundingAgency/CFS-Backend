using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class FieldDefinitionBuilder : TestEntityBuilder
    {
        private IdentifierFieldType? _identifierFieldType;
        private FieldType? _fieldType;
        private string _name;
        private bool _isAggregate;
        private bool _required;

        public FieldDefinitionBuilder WithFieldType(FieldType fieldType)
        {
            _fieldType = fieldType;

            return this;
        }

        public FieldDefinitionBuilder WithRequired(bool required)
        {
            _required = required;

            return this;
        }

        public FieldDefinitionBuilder WithIsAggregate(bool isAggregate)
        {
            _isAggregate = isAggregate;

            return this;
        }
        
        public FieldDefinitionBuilder WithIdentifierFieldType(IdentifierFieldType identifierFieldType)
        {
            _identifierFieldType = identifierFieldType;

            return this;
        }

        public FieldDefinitionBuilder WithName(string name)
        {
            _name = name;

            return this;
        }
        
        public FieldDefinition Build()
        {
            return new FieldDefinition
            {
                Name = _name ?? NewRandomString(),
                IsAggregable = _isAggregate,
                Required = _required,
                IdentifierFieldType = _identifierFieldType,
                Type = _fieldType.GetValueOrDefault(NewRandomEnum<FieldType>())
            };
        }
    }
}