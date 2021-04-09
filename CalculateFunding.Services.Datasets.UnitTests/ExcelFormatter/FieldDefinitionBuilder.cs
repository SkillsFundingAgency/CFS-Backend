using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Datasets.ExcelFormatter
{
    public class FieldDefinitionBuilder : TestEntityBuilder
    {
        private string _name;

        public FieldDefinitionBuilder WithName(string name)
        {
            _name = name;

            return this;
        }

        public FieldDefinition Build() =>
            new FieldDefinition
            {
                Name = _name ?? NewRandomString()
            };
    }
}