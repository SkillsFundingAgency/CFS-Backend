using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;

namespace CalculateFunding.Services.Profiling.Tests
{
    public class EditProfilePatternRequestBuilder : TestEntityBuilder
    {
        private FundingStreamPeriodProfilePattern _pattern;

        public EditProfilePatternRequestBuilder WithPattern(FundingStreamPeriodProfilePattern pattern)
        {
            _pattern = pattern;

            return this;
        }
        
        public EditProfilePatternRequest Build()
        {
            return new EditProfilePatternRequest
            {
                Pattern = _pattern
            };
        }
    }
}