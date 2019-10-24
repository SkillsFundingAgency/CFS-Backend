﻿using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Publishing.Interfaces;


namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public class PublishedProviderStepContext: IPublishedProviderStepContext
    {
        public IProviderService Service { get; set; }
        public IProvidersApiClient Client { get; set; }
        public ProvidersInMemoryClient EmulatedClient { get; set; }

        public InMemoryAzureBlobClient BlobRepo { get; set; }

        public PublishedProviderInMemorySearchRepository SearchRepo { get; set; }
    }
}