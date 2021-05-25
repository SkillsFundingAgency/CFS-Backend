using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Datasets.IntegrationTests.ConverterWizard
{
    public class FieldDefinitionBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;
        private IdentifierFieldType? _identifierFieldType;
        private string _description;
        private FieldType? _type;
        private bool _required;

        public FieldDefinitionBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public FieldDefinitionBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public FieldDefinitionBuilder WithIdentifierFieldType(IdentifierFieldType identifierFieldType)
        {
            _identifierFieldType = identifierFieldType;

            return this;
        }

        public FieldDefinitionBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public FieldDefinitionBuilder WithFieldType(FieldType fieldType)
        {
            _type = fieldType;

            return this;
        }

        public FieldDefinitionBuilder WithRequired(bool required)
        {
            _required = required;

            return this;
        }

        public FieldDefinition Build() =>
            new FieldDefinition
            {
                Id = _id ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                IdentifierFieldType = _identifierFieldType,
                Description = _description ?? NewRandomString(),
                Required = _required,
                Type = _type.GetValueOrDefault(FieldType.String)
            };
    }
}