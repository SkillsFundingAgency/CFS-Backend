using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;
using System.Linq;

namespace CalculateFunding.Services.Profiling.Tests.Services
{
    public class FundingStreamPeriodProfilePatternBuilder : TestEntityBuilder
    {
        private ProfilePatternReProfilingConfiguration _profilePatternReProfilingConfiguration;
        private string _profilePatternKey;
        private string _profilePatternDisplayName;
        private ProfilePeriodPattern[] _profilePattern;

        public FundingStreamPeriodProfilePatternBuilder WithProfilePatternReProfilingConfiguration(ProfilePatternReProfilingConfiguration patternReProfilingConfiguration)
        {
            _profilePatternReProfilingConfiguration = patternReProfilingConfiguration;

            return this;
        }

        public FundingStreamPeriodProfilePatternBuilder WithProfilePatternKey(string profilePatternKey)
        {
            _profilePatternKey = profilePatternKey;
            return this;
        }

        public FundingStreamPeriodProfilePatternBuilder WithProfilePatternDisplayName(string profilePatternDisplayName)
        {
            _profilePatternDisplayName = profilePatternDisplayName;
            return this;
        }

        public FundingStreamPeriodProfilePatternBuilder WithProfilePattern(params ProfilePeriodPattern[] profilePattern)
        {
            _profilePattern = profilePattern.ToArray();
            return this;
        }

        public FundingStreamPeriodProfilePattern Build()
        {
            return new FundingStreamPeriodProfilePattern
            {
                ReProfilingConfiguration = _profilePatternReProfilingConfiguration,
                ProfilePatternKey = _profilePatternKey,
                ProfilePatternDisplayName = _profilePatternDisplayName,
                ProfilePattern = _profilePattern
            };
        }
    }
}