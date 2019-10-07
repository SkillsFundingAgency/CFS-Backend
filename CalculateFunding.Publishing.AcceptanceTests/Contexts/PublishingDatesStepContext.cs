using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class PublishingDatesStepContext : IPublishingDatesStepContext
    {
        public IPublishedFundingDateService Service { get; set; }

        public PublishedFundingDateService EmulatedService { get; set; }
        public SpecificationsInMemoryClient EmulatedClient { get; set; }
    }
}
