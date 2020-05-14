using CalculateFunding.Models.Publishing;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Undo
{
    public class PublishedFundingPeriodBuilder : TestEntityBuilder
    {
        public PublishedFundingPeriod Build()
        {
            return new PublishedFundingPeriod
            {
                Name = NewRandomString(),
                Period = NewRandomString(),
                Type = NewRandomEnum<PublishedFundingPeriodType>()
            };
        }
    }
}