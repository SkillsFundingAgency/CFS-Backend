using CalculateFunding.Tests.Common.Helpers;
using CalculateFunding.Common.ApiClient.Graph.Models;

namespace CalculateFunding.Services.Calcs.UnitTests.Services
{
    public class GraphCalculationBuilder : TestEntityBuilder
    {
        private string _id;
        private string _specificationId;

        public GraphCalculationBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public GraphCalculationBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }

        public Calculation Build()
        {
            return new Calculation
            {
                CalculationId = _id ?? NewRandomString(),
                SpecificationId = _specificationId ?? NewRandomString()
            };
        }

    }
}
