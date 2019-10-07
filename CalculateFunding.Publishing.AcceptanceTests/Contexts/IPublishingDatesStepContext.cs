using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IPublishingDatesStepContext
    {
        IPublishedFundingDateService Service { get; set; }

        PublishedFundingDateService EmulatedService { get; set; }

        SpecificationsInMemoryClient EmulatedClient { get; set; }
    }
}
