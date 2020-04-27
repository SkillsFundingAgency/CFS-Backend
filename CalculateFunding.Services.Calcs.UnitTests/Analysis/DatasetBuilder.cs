using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    public class DatasetBuilder : TestEntityBuilder
    {
        public Dataset Build()
        {
            return new Dataset
            {
                DatasetId = NewRandomString(),
                Description = NewRandomString(),
                Name = NewRandomString()
            };
        }
    }
}