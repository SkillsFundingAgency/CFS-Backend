using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedProviderIndexBuilder : TestEntityBuilder
    {
        public PublishedProviderIndex Build()
        {
            return new PublishedProviderIndex
            {
                Id = NewRandomString(),
                FundingStatus = NewRandomString(),
                FundingValue = NewRandomNumberBetween(1, int.MaxValue),
                LocalAuthority = NewRandomString(),
                ProviderName = NewRandomString(),
                ProviderType = NewRandomString(),
                SpecificationId = NewRandomString(),
                FundingPeriodId = NewRandomString(),
                FundingStreamId = NewRandomString(),
                UKPRN = NewRandomString()
            };
        }
    }
}