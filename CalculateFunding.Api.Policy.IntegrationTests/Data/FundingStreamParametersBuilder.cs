using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Policy.IntegrationTests.Data
{
    public class FundingStreamParametersBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;
        private string _shortName;

        public FundingStreamParametersBuilder WithShortName(string shortName)
        {
            _shortName = shortName;
            return this;
        }

        public FundingStreamParametersBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public FundingStreamParametersBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        public FundingStreamParameters Build()
        {
            return new FundingStreamParameters()
            {
                Id = _id ?? NewRandomString(),
                Name = _name ?? NewRandomString(),
                ShortName = _shortName ?? NewRandomString()
            };
        }
    }
}
