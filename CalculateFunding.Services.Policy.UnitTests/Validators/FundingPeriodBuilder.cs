using CalculateFunding.Models.Policy;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Policy.Validators
{
    public class FundingPeriodBuilder : TestEntityBuilder
    {
        private string _period;

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
            };
        }
    }
}