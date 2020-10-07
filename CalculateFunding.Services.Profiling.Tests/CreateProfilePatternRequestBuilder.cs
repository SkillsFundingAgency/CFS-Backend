using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;

namespace CalculateFunding.Services.Profiling.Tests
{
    public class CreateProfilePatternRequestBuilder : TestEntityBuilder
    {
        private FundingStreamPeriodProfilePattern _pattern;

        public CreateProfilePatternRequestBuilder WithPattern(FundingStreamPeriodProfilePattern pattern)
        {
            _pattern = pattern;

            return this;
        }
        
        public CreateProfilePatternRequest Build()
        {
            return new CreateProfilePatternRequest
            {
                Pattern = _pattern
            };
        }  
    }
}