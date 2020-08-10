using CalculateFunding.Models.Policy;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Policy.Validators
{
    public class FundingStreamBuilder : TestEntityBuilder
    {
        private string _id;

        public FundingStreamBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public FundingStream Build()
        {
            return new FundingStream
            {
                Id = _id,
            };
        }
    }
}