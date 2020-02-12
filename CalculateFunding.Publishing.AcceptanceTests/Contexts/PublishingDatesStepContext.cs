using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class PublishingDatesStepContext : IPublishingDatesStepContext
    {
        private readonly ISpecificationsApiClient _specificationsApiClient;

        public PublishingDatesStepContext(IPublishedFundingDateService service,
            ISpecificationsApiClient specificationsApiClient)
        {
            _specificationsApiClient = specificationsApiClient;
            Service = service;
        }

        public IPublishedFundingDateService Service { get; }

        //?? no idea what this is for?
        public PublishedFundingDateService EmulatedService => (PublishedFundingDateService) Service;


        public SpecificationsInMemoryClient EmulatedClient => (SpecificationsInMemoryClient) _specificationsApiClient;
    }
}
