using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.Services
{
    public class TableDefinitionBuilder : TestEntityBuilder
    {
        private IEnumerable<FieldDefinition> _fieldDefinitions;

        public TableDefinitionBuilder WithFieldDefinitions(params FieldDefinition[] fieldDefinitions)
        {
            _fieldDefinitions = fieldDefinitions;

            return this;
        }

        public TableDefinition Build()
        {
            return new TableDefinition
            {
                FieldDefinitions = _fieldDefinitions?.ToList() ?? new List<FieldDefinition>()
            };
        }
    }
}