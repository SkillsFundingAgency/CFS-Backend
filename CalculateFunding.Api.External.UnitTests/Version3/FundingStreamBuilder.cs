using CalculateFunding.Api.External.V3.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.External.UnitTests.Version3
{
    public class FundingStreamBuilder : TestEntityBuilder
    {
        private string _id;
        private string _name;

        public FundingStreamBuilder WithId(string id)
        {
            _id = id;
            return this;
        }

        public FundingStreamBuilder WithName(string name)
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
