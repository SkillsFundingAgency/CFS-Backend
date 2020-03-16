using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class ProvidersInMemoryService : IProviderService
    {
        private Dictionary<string, Dictionary<string, Provider>> _providers = new Dictionary<string, Dictionary<string, Provider>>();
        private Dictionary<string, Dictionary<string, Provider>> _scopedProviders = new Dictionary<string, Dictionary<string, Provider>>();

        public Task<IEnumerable<Provider>> GetProvidersByProviderVersionsId(string providerVersionId)
        {
            IEnumerable<Provider> result = null;
            if (_providers.ContainsKey(providerVersionId))
            {
                result = _providers[providerVersionId].Values;
            }

            return Task.FromResult(result);
        }

        public Task<IDictionary<string, Provider>> GetScopedProvidersForSpecification(string specificationId, string providerVersionId)
        {
            IDictionary<string, Provider> result = null;
            if (_scopedProviders.ContainsKey(specificationId))
            {
                result = _scopedProviders[specificationId];
            }

            return Task.FromResult(result);
        }

        public void AddProviderToCoreProviderData(string providerVersionId, Provider provider)
        {
            Guard.IsNullOrWhiteSpace(providerVersionId, nameof(providerVersionId));
            Guard.ArgumentNotNull(provider, nameof(provider));
            Guard.ArgumentNotNull(provider.ProviderId, nameof(provider.ProviderId));

            if (!_providers.ContainsKey(providerVersionId))
            {
                _providers.Add(providerVersionId, new Dictionary<string, Provider>());
            }

            _providers[providerVersionId][provider.ProviderId] = provider;
        }

        public void AddProviderAsScopedProvider(string specificationId, string providerVersionId, string providerId)
        {
            Guard.IsNullOrWhiteSpace(providerVersionId, nameof(providerVersionId));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            Dictionary<string, Provider> providerVersionProviders;
            if (!_providers.TryGetValue(providerVersionId, out providerVersionProviders))
            {
                throw new InvalidOperationException($"Provider Version not found. '{providerVersionId}'");
            }

            Provider provider;
            if (!providerVersionProviders.TryGetValue(providerId, out provider))
            {
                throw new InvalidOperationException($"Provider Version not found. Provider ID '{providerId}' in '{providerVersionId}'");
            }

            if (!_scopedProviders.ContainsKey(specificationId))
            {
                _scopedProviders.Add(specificationId, new Dictionary<string, Provider>());
            }

            _scopedProviders[specificationId][providerId] = provider;
        }

        public Task<IEnumerable<string>> GetScopedProviderIdsForSpecification(string specificationId)
        {
            return Task.FromResult(_scopedProviders[specificationId].Keys.AsEnumerable());
        }

        public Task<(IDictionary<string, PublishedProvider> PublishedProvidersForFundingStream, IDictionary<string, PublishedProvider> ScopedPublishedProviders)> GetPublishedProviders(Reference fundingStream, SpecificationSummary specification)
        {
            throw new NotImplementedException();
        }

        public IDictionary<string, PublishedProvider> GenerateMissingPublishedProviders(IEnumerable<Provider> scopedProviders, SpecificationSummary specification, Reference fundingStream, IDictionary<string, PublishedProvider> publishedProviders)
        {
            throw new NotImplementedException();
        }

        public Task<PublishedProvider> CreateMissingPublishedProviderForPredecessor(PublishedProvider predecessor, string successorId)
        {
            throw new NotImplementedException();
        }
    }
}
