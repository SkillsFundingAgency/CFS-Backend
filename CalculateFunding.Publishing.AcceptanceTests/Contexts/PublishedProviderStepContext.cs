using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Publishing.Interfaces;


namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class PublishedProviderStepContext: IPublishedProviderStepContext
    {
        private readonly ISearchRepository<PublishedProviderIndex> _searchRepository;
        private readonly IBlobClient _blobClient;

        public PublishedProviderStepContext(IProviderService service, 
            IProvidersApiClient client,
            ISearchRepository<PublishedProviderIndex> searchRepository,
            IBlobClient blobClient)
        {
            _searchRepository = searchRepository;
            _blobClient = blobClient;
            Service = service;
            Client = client;
        }

        public IProviderService Service { get; }

        public IProvidersApiClient Client { get; }

        public ProvidersInMemoryClient EmulatedClient => (ProvidersInMemoryClient) Client;

        public InMemoryAzureBlobClient BlobRepo => (InMemoryAzureBlobClient) _blobClient;

        public PublishedProviderInMemorySearchRepository SearchRepo =>
            (PublishedProviderInMemorySearchRepository) _searchRepository;
    }
}
