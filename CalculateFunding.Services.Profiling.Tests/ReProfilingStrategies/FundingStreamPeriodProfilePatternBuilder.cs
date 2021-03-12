using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;

namespace CalculateFunding.Services.Profiling.Tests.ReProfilingStrategies
{
    public class FundingStreamPeriodProfilePatternBuilder : TestEntityBuilder
    {
        private RoundingStrategy? _roundingStrategy;
        private IEnumerable<ProfilePeriodPattern> _profilePattern;

        public FundingStreamPeriodProfilePatternBuilder WithProfilePattern(params ProfilePeriodPattern[] profilePeriod)
        {
            _profilePattern = profilePeriod;

            return this;
        }

        public FundingStreamPeriodProfilePatternBuilder WithRoundingStrategy(RoundingStrategy roundingStrategy)
        {
            _roundingStrategy = roundingStrategy;

            return this;
        }
        
        public FundingStreamPeriodProfilePattern Build()
        {
            return new FundingStreamPeriodProfilePattern
            {
                RoundingStrategy = _roundingStrategy.GetValueOrDefault(NewRandomEnum<RoundingStrategy>()),
                ProfilePattern = _profilePattern?.ToArray()
            };
        }
    }
}