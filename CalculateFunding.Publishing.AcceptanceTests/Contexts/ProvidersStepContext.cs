using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class ProvidersStepContext : IProvidersStepContext
    {
        private readonly IBlobClient _blobClient;

        public ProvidersStepContext(IProviderService service, 
            IProvidersApiClient client,
            IBlobClient blobClient)
        {
            _blobClient = blobClient;
            Service = service;
            Client = client;
        }

        public IProviderService Service { get; }

        public IProvidersApiClient Client { get; }

        public ProvidersInMemoryClient EmulatedClient => (ProvidersInMemoryClient) Client;

        public InMemoryAzureBlobClient BlobRepo => (InMemoryAzureBlobClient) _blobClient;
    }
}
