using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class FundingPeriodParametersBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;

        public FundingPeriodParametersBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public FundingPeriodParametersBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        public FundingPeriodParameters Build()
        {
            return new FundingPeriodParameters()
            {
                Id = _id ?? NewRandomString(),
                Name = _name ?? NewRandomString()
            };
        }
    }
}
