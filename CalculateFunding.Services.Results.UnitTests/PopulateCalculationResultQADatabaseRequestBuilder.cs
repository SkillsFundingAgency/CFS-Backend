using CalculateFunding.Services.Results.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Results.UnitTests
{
    public class PopulateCalculationResultQADatabaseRequestBuilder : TestEntityBuilder
    {
        private string _specificationId;

        public PopulateCalculationResultQADatabaseRequestBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }


        public PopulateCalculationResultQADatabaseRequest Build()
            => new PopulateCalculationResultQADatabaseRequest
            {
                SpecificationId = _specificationId,
            };
    }
}
