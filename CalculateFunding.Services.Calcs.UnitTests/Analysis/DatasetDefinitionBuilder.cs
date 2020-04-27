using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    public class DatasetDefinitionBuilder : TestEntityBuilder
    {
        public DatasetDefinition Build()
        {
            return new DatasetDefinition
            {
                DatasetDefinitionId = NewRandomString(),
                Description = NewRandomString(),
                Name = NewRandomString()
            };
        }
    }
}