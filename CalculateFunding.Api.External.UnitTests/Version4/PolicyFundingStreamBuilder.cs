using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.External.UnitTests.Version4
{
    public class PolicyFundingStreamBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;

        public PolicyFundingStreamBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        public PolicyFundingStreamBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public FundingStream Build()
        {
            return new FundingStream
            {
                Id = _id ?? NewRandomString(),
                Name = _name ?? NewRandomString()
            };
        }
    }
}
