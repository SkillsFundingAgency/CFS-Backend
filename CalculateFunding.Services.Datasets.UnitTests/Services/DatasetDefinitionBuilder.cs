using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Tests.Common.Helpers;


namespace CalculateFunding.Services.Datasets.Services
{
    public class DatasetDefinitionBuilder : TestEntityBuilder
    {
        private string _id;
        private IEnumerable<TableDefinition> _tableDefinitions;

        public DatasetDefinitionBuilder WithTableDefinitions(params TableDefinition[] tableDefinitions)
        {
            _tableDefinitions = tableDefinitions;

            return this;
        }

        public DatasetDefinitionBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public DatasetDefinition Build()
        {
            return new DatasetDefinition
            {
                Id = _id,
                TableDefinitions = _tableDefinitions?.ToList()
            };
        }
    }
}