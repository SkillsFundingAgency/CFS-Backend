using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class PublishedFundingIndexBuilder : TestEntityBuilder
    {
        public PublishedFundingIndex Build()
        {
            return new PublishedFundingIndex
            {
                Id = NewRandomString(),
                FundingPeriodId = NewRandomString(),
                FundingStreamId = NewRandomString(),
                DocumentPath = NewRandomString()
            };
        }
    }
}