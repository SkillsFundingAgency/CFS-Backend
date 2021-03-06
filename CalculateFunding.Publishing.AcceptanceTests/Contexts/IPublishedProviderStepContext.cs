﻿using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Publishing.AcceptanceTests.Repositories;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Contexts
{
    public interface IPublishedProviderStepContext
    {
        IProviderService Service { get; }

        IProvidersApiClient Client { get; }

        ProvidersInMemoryClient EmulatedClient { get; }

        InMemoryAzureBlobClient BlobRepo { get; }

        PublishedProviderInMemorySearchRepository SearchRepo { get; }
    }
}