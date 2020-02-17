using CalculateFunding.Common.ApiClient.Graph.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    public class GraphApiSpecificationBuilder : TestEntityBuilder
    {
        public Specification Build()
        {
            return new Specification
            {
                SpecificationId = NewRandomString(),
                Description = NewRandomString(),
                Name = NewRandomString()
            };
        }
    }
}