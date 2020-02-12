using CalculateFunding.Models.Publishing;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IPublishedFundingResultStepContext
    {
        PublishedFunding CurrentPublishedFunding { get; set; }
        PublishedFundingVersion PublishedFundingVersion { get; }
    }
}
