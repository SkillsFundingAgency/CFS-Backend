using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Providers.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using ApiProviderVersion = CalculateFunding.Common.ApiClient.Providers.Models.ProviderVersion;

namespace CalculateFunding.Services.Publishing.Providers
{
    public class ProviderService : IProviderService
    {
        private readonly IProvidersApiClient _providers;
        private readonly Policy _resiliencePolicy;

        public ProviderService(IProvidersApiClient providers,
            IPublishingResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(providers, nameof(providers));
            Guard.ArgumentNotNull(resiliencePolicies?.ProvidersApiClient, nameof(resiliencePolicies.ProvidersApiClient));

            _providers = providers;
            _resiliencePolicy = resiliencePolicies.ProvidersApiClient;
        }

        public async Task<IEnumerable<Provider>> GetProvidersByProviderVersionsId(string providerVersionId)
        {
            Guard.IsNullOrWhiteSpace(providerVersionId, nameof(providerVersionId));

            ApiResponse<ApiProviderVersion> providerVersionsResponse =
                await _resiliencePolicy.ExecuteAsync(() => _providers.GetProvidersByVersion(providerVersionId));

            Guard.ArgumentNotNull(providerVersionsResponse?.Content, nameof(providerVersionsResponse));

            return providerVersionsResponse.Content.Providers?.ToArray() ?? new Provider[0];
        }

        public async Task<IEnumerable<string>> GetScopedProviderIdsForSpecification(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            ApiResponse<IEnumerable<string>> scopedProviderIdResponse =
                 await _resiliencePolicy.ExecuteAsync(() => _providers.GetScopedProviderIds(specificationId));

            if (scopedProviderIdResponse == null)
            {
                throw new InvalidOperationException("Scoped provider response was null");
            }

            if (scopedProviderIdResponse.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new RetriableException($"Scoped provider response was not OK, but instead '{scopedProviderIdResponse.StatusCode}'");
            }

            if (scopedProviderIdResponse.Content == null)
            {
                throw new InvalidOperationException("Scoped provider response content was null");
            }

            return scopedProviderIdResponse.Content;
        }

        public async Task<IEnumerable<Provider>> GetScopedProvidersForSpecification(string specificationId, string providerVersionId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(providerVersionId, nameof(providerVersionId));

            ApiResponse<ApiProviderVersion> providerVersionsResponse =
                await _resiliencePolicy.ExecuteAsync(() => _providers.GetProvidersByVersion(providerVersionId));

            Guard.ArgumentNotNull(providerVersionsResponse?.Content, nameof(providerVersionsResponse));

            ApiResponse<IEnumerable<string>> scopedProviderIdResponse =
                 await _resiliencePolicy.ExecuteAsync(() => _providers.GetScopedProviderIds(specificationId));

            Guard.ArgumentNotNull(scopedProviderIdResponse?.Content, nameof(scopedProviderIdResponse));

            return providerVersionsResponse.Content.Providers.Where(p => scopedProviderIdResponse.Content.Contains(p.ProviderId));
        }
    }
}