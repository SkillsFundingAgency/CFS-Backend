using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;

namespace CalculateFunding.Services.Profiling.Tests.Services
{
    public class FundingStreamPeriodProfilePatternBuilder : TestEntityBuilder
    {
        private ProfilePatternReProfilingConfiguration _profilePatternReProfilingConfiguration;

        public FundingStreamPeriodProfilePatternBuilder WithProfilePatternReProfilingConfiguration(ProfilePatternReProfilingConfiguration patternReProfilingConfiguration)
        {
            _profilePatternReProfilingConfiguration = patternReProfilingConfiguration;

            return this;
        }

        public FundingStreamPeriodProfilePattern Build()
        {
            return new FundingStreamPeriodProfilePattern
            {
                ReProfilingConfiguration = _profilePatternReProfilingConfiguration
            };
        }
    }
}