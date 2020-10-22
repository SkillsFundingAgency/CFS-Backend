using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public class ProfilePreviewRequestBuilder : TestEntityBuilder
    {
        public ProfilePreviewRequest Build()
        {
            return new ProfilePreviewRequest
            {
                ConfigurationType = NewRandomEnum<ProfileConfigurationType>(),
                SpecificationId = NewRandomString(),
                FundingStreamId = NewRandomString(),
                FundingLineCode = NewRandomString(),
                ProviderId = NewRandomString(),
                FundingPeriodId = NewRandomString(),
                ProfilePatternKey = NewRandomString()
            };
        }
    }
}