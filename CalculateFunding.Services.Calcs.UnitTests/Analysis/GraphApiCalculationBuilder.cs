using CalculateFunding.Common.ApiClient.Graph.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    public class GraphApiCalculationBuilder : TestEntityBuilder
    {
        public Calculation Build()
        {
            return new Calculation
            {
                CalculationId = NewRandomString(),
                CalculationName = NewRandomString(),
                CalculationType = NewRandomEnum<CalculationType>(),
                FundingStream = NewRandomString(),
                SpecificationId = NewRandomString()
            };
        }        
    }
}