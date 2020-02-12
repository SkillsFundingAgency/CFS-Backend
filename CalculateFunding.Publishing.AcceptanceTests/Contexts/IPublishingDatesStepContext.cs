using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IPublishingDatesStepContext
    {
        IPublishedFundingDateService Service { get; }

        PublishedFundingDateService EmulatedService { get; }

        SpecificationsInMemoryClient EmulatedClient { get; }
    }
}
