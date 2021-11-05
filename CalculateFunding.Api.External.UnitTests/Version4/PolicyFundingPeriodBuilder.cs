using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.External.UnitTests.Version4
{
    public class PolicyFundingPeriodBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;

        public PolicyFundingPeriodBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        public PolicyFundingPeriodBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public FundingPeriod Build()
        {
            return new FundingPeriod
            {
                Id = _id ?? NewRandomString(),
                Name = _name ?? NewRandomString()
            };
        }
    }
}
