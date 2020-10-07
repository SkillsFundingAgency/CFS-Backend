using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;

namespace CalculateFunding.Services.Profiling.Tests
{
    public class UpsertProfilePatternRequestBuilder : TestEntityBuilder
    {
        private FundingStreamPeriodProfilePattern _pattern;

        public UpsertProfilePatternRequestBuilder WithPattern(FundingStreamPeriodProfilePattern pattern)
        {
            _pattern = pattern;

            return this;
        }
        
        public UpsertProfilePatternRequest Build()
        {
            return new UpsertProfilePatternRequest
            {
                Pattern = _pattern
            };
        }
    }
}