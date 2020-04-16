using System.Collections.Generic;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Profiling.Custom;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling.Overrides
{
    public class FundingLineProfileOverridesBuilder : TestEntityBuilder
    {
        private string _fundingLineCode;
        private IEnumerable<DistributionPeriod> _distributionPeriods;

        public FundingLineProfileOverridesBuilder WithFundingLineCode(string fundingLineCode)
        {
            _fundingLineCode = fundingLineCode;

            return this;
        }

        public FundingLineProfileOverridesBuilder WithDistributionPeriods(params DistributionPeriod[] distributionPeriods)
        {
            _distributionPeriods = distributionPeriods;

            return this;
        }
        
        public FundingLineProfileOverrides Build()
        {
            return new FundingLineProfileOverrides
            {
                FundingLineCode = _fundingLineCode ?? NewRandomString(),
                DistributionPeriods = _distributionPeriods
            };
        }
    }
}