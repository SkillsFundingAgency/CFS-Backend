using System.Collections.Generic;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Datasets.IntegrationTests.Datasets
{
    public class TableDefinitionBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;
        private string _description;
        private FieldDefinition[] _fields;

        public TableDefinitionBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public TableDefinitionBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public TableDefinitionBuilder WithDescription(string description)
        {
            _description = description;

            return this;
        }

        public TableDefinitionBuilder WithFields(params FieldDefinition[] fields)
        {
            _fields = fields;

            return this;
        }

        public TableDefinition Build() =>
            new TableDefinition
            {
                Id = _id ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                Description = _description ?? NewRandomString(),
                FieldDefinitions = new List<FieldDefinition>(_fields ?? new FieldDefinition[0])
            };
    }
}