using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class PublishedFundingResultStepContext : IPublishedFundingResultStepContext
    {
        public PublishedFunding CurrentPublishedFunding { get; set; }

        public PublishedFundingVersion PublishedFundingVersion => CurrentPublishedFunding?.Current;
    }
}
