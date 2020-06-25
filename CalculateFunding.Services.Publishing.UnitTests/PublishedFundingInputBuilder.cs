using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedFundingInputBuilder : TestEntityBuilder
    {
        private string _specificationId;

        public PublishedFundingInputBuilder WithSpecificationId(string specificationId)
        {
            _specificationId = specificationId;

            return this;
        }
        public PublishedFundingInput Build()
        {
            return new PublishedFundingInput
            {
                SpecificationId = _specificationId ?? NewRandomString()
            };
        }
    }
}
