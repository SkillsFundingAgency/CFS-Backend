using CalculateFunding.Models.Policy;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Policy.Validators
{
    public class FundingPeriodBuilder : TestEntityBuilder
    {
        private string _period;
        private string _id;

        public FundingPeriodBuilder WithId(string id)
        {
            _id = id;

            return this;
        }

        public FundingPeriodBuilder WithPeriod(string period)
        {
            _period = period;

            return this;
        }

        public FundingPeriod Build()
        {
            return new FundingPeriod
            {
                Period = _period,
                Id = _id
            };
        }
    }
}