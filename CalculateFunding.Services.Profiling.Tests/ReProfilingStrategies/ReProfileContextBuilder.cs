using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;

namespace CalculateFunding.Services.Profiling.Tests.ReProfilingStrategies
{
    public class ReProfileContextBuilder : TestEntityBuilder
    {
        private ReProfileRequest _request;
        private AllocationProfileResponse _response;
        private FundingStreamPeriodProfilePattern _profilePattern;

        public ReProfileContextBuilder WithProfilePattern(FundingStreamPeriodProfilePattern profilePattern)
        {
            _profilePattern = profilePattern;

            return this;
        }
        
        public ReProfileContextBuilder WithReProfileRequest(ReProfileRequest request)
        {
            _request = request;

            return this;
        }

        public ReProfileContextBuilder WithAllocationProfileResponse(AllocationProfileResponse response)
        {
            _response = response;

            return this;
        }
        
        public ReProfileContext Build()
        {
            return new ReProfileContext
            {
                Request = _request,
                ProfileResult = _response,
                ProfilePattern = _profilePattern
            };
        }
    }
}