using CalculateFunding.Models.Graph;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Calcs.UnitTests.Analysis
{
    public class GraphCalculationBuilder : TestEntityBuilder
    {
        private string _id;

        public GraphCalculationBuilder WithId(string id)
        {
            _id = id;

            return this;
        }
        
        public Calculation Build()
        {
            return new Calculation
            {
                CalculationId = _id ?? NewRandomString(), 
            };
        }
    }
}