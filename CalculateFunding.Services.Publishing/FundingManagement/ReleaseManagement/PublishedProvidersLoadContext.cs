using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    /// <summary>
    /// Scoped per execution - a cache of in memory published providers to be used during release management
    /// </summary>
    public class PublishedProvidersLoadContext : IPublishedProvidersLoadContext
    {
        private readonly IPublishedFundingBulkRepository _bulkRepo;
        private readonly IPublishedFundingRepository _publishedFundingRepository;

        readonly ConcurrentDictionary<string, PublishedProvider> _providers = new ConcurrentDictionary<string, PublishedProvider>();
        readonly ConcurrentDictionary<(string providerId, int majorVersion), PublishedProviderVersion> _providerVersionsByProviderIdAndMajorVersion 
            = new ConcurrentDictionary<(string providerId, int majorVersion), PublishedProviderVersion>();

        private string _fundingStreamId;
        private string _fundingPeriodId;

        public PublishedProvidersLoadContext(IPublishedFundingBulkRepository publishedFundingBulkRepository,
            IPublishedFundingRepository publishedFundingRepository)
        {
            _bulkRepo = publishedFundingBulkRepository;
            _publishedFundingRepository = publishedFundingRepository;
        }

        public PublishedProvider this[string key] => _providers[key];

        public IEnumerable<string> Keys => _providers.Keys;

        public IEnumerable<PublishedProvider> Values => _providers.Values;

        public int Count => _providers.Count;

        public void AddProviders(IEnumerable<PublishedProvider> publishedProviders)
        {
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));

            foreach (PublishedProvider provider in publishedProviders)
            {
                AddProvider(provider);
            }
        }

        private void AddProvider(PublishedProvider provider)
        {
            Guard.ArgumentNotNull(provider, nameof(provider));

            _providers.AddOrUpdate(provider.Current.ProviderId, provider, (key, existingItem) => { return provider; });
        }

        private void AddPublishedProviderVersion(PublishedProviderVersion publishedProviderVersion)
        {
            Guard.ArgumentNotNull(publishedProviderVersion, nameof(publishedProviderVersion));

            _providerVersionsByProviderIdAndMajorVersion.AddOrUpdate(
                (publishedProviderVersion.ProviderId, publishedProviderVersion.MajorVersion),
                publishedProviderVersion,
                (key, existingItem) => { return publishedProviderVersion; });
        }

        public bool ContainsKey(string key)
        {
            return _providers.ContainsKey(key);
        }

        public IEnumerator<KeyValuePair<string, PublishedProvider>> GetEnumerator()
        {
            return _providers.GetEnumerator();
        }

        public async Task LoadProviders(IEnumerable<string> providerIds)
        {
            if (string.IsNullOrWhiteSpace(_fundingStreamId))
            {
                throw new InvalidOperationException("Funding stream not set");
            }

            if (string.IsNullOrWhiteSpace(_fundingPeriodId))
            {
                throw new InvalidOperationException("Funding period not set");
            }

            IEnumerable<PublishedProvider> results = await _bulkRepo.TryGetPublishedProvidersByProviderId(providerIds, _fundingStreamId, _fundingPeriodId);

            AddProviders(results);
        }

        public async Task<PublishedProvider> LoadProvider(string providerId)
        {
            PublishedProvider publishedProvider = await _publishedFundingRepository.GetPublishedProvider(_fundingStreamId, _fundingPeriodId, providerId);
            if (publishedProvider == null)
            {
                throw new InvalidOperationException($"Published provider with provider ID {providerId} not found");
            }

            AddProvider(publishedProvider);

            return publishedProvider;
        }

        public async Task<PublishedProviderVersion> LoadProviderVersion(string providerId, int majorVersion)
        {
            PublishedProviderVersion publishedProviderVersion 
                = await _publishedFundingRepository.GetReleasedPublishedProviderVersion(_fundingStreamId, _fundingPeriodId, providerId, majorVersion);
            if (publishedProviderVersion == null)
            {
                throw new InvalidOperationException($"Published provider with provider ID {providerId} and major version {majorVersion} not found");
            }

            AddPublishedProviderVersion(publishedProviderVersion);

            return publishedProviderVersion;
        }

        public void SetSpecDetails(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            _fundingStreamId = fundingStreamId;
            _fundingPeriodId = fundingPeriodId;
        }

        public bool TryGetValue(string key, [MaybeNullWhen(false)] out PublishedProvider value)
        {
            return _providers.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _providers.GetEnumerator();
        }

        public async Task<IEnumerable<PublishedProvider>> GetOrLoadProviders(IEnumerable<string> providerIds)
        {
            Guard.ArgumentNotNull(providerIds, nameof(providerIds));

            IEnumerable<string> providerIdsToLoad = providerIds.Except(_providers.Keys);

            if (providerIdsToLoad.Any())
            {
                await LoadProviders(providerIdsToLoad);
            }

            return providerIds.Select(providerId => _providers[providerId]);
        }

        public async Task<PublishedProviderVersion> GetOrLoadProviderVersion(string providerId, int majorVersion)
        {
            Guard.ArgumentNotNull(providerId, nameof(providerId));
            Guard.ArgumentNotNull(majorVersion, nameof(majorVersion));

            if (_providers.ContainsKey(providerId) && _providers[providerId].Released.MajorVersion == majorVersion)
            {
                return _providers[providerId].Released;
            }

            if(_providerVersionsByProviderIdAndMajorVersion.ContainsKey((providerId, majorVersion)))
            {
                return _providerVersionsByProviderIdAndMajorVersion[(providerId, majorVersion)];
            }

            await LoadProviderVersion(providerId, majorVersion);

            return _providerVersionsByProviderIdAndMajorVersion[(providerId, majorVersion)];
        }
    }
}
