using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryPublishedFundingRepository : IPublishedFundingRepository
    {
        ConcurrentDictionary<string, ConcurrentBag<PublishedProvider>> _publishedProviders = new ConcurrentDictionary<string, ConcurrentBag<PublishedProvider>>();

        // Keyed on SpecificationId
        ConcurrentDictionary<string, ConcurrentBag<PublishedFunding>> _publishedFunding = new ConcurrentDictionary<string, ConcurrentBag<PublishedFunding>>();

        public Task<IEnumerable<PublishedFunding>> GetLatestPublishedFundingBySpecification(string specificationId)
        {
            IEnumerable<PublishedFunding> result = null;

            if (_publishedFunding.ContainsKey(specificationId))
            {
                result = _publishedFunding[specificationId];
            }

            return Task.FromResult(result);
        }

        public Task<IEnumerable<PublishedProvider>> GetLatestPublishedProvidersBySpecification(string specificationId)
        {
            IEnumerable<PublishedProvider> result = null;
            if (_publishedProviders.ContainsKey(specificationId))
            {
                result = _publishedProviders[specificationId];
            }

            return Task.FromResult(result);
        }

        public Task<IEnumerable<PublishedProvider>> GetPublishedProvidersForApproval(string specificationId)
        {
            IEnumerable<PublishedProvider> results = _publishedProviders[specificationId].Where(p =>
             p.Current.Status == PublishedProviderStatus.Draft || p.Current.Status == PublishedProviderStatus.Updated);

            return Task.FromResult(results);
        }

        public Task<PublishedProviderVersion> GetPublishedProviderVersion(string fundingStreamId, string fundingPeriodId, string providerId, string version)
        {
            PublishedProvider publishedProvider = _publishedProviders.SelectMany(c => c.Value).Where(p =>
              p.Current.FundingStreamId == fundingStreamId
              && p.Current.FundingPeriodId == fundingPeriodId
              && p.Current.ProviderId == providerId).FirstOrDefault();

            PublishedProviderVersion result = null;

            if (publishedProvider != null)
            {
                result = publishedProvider.Current;
            }

            return Task.FromResult(result);
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> UpsertPublishedFunding(PublishedFunding publishedFunding)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(publishedFunding.Current, nameof(publishedFunding.Current));
            Guard.IsNullOrWhiteSpace(publishedFunding.Current.SpecificationId, nameof(publishedFunding.Current.SpecificationId));
            Guard.IsNullOrWhiteSpace(publishedFunding.Id, nameof(publishedFunding.Id));

            string specificationId = publishedFunding.Current.SpecificationId;

            if (!_publishedFunding.ContainsKey(specificationId))
            {
                _publishedFunding.TryAdd(specificationId, new ConcurrentBag<PublishedFunding>());
            }

            List<PublishedFunding> itemsToRemove = new List<PublishedFunding>();

            PublishedFunding existingFunding = _publishedFunding[specificationId].Where(p => p.Id == publishedFunding.Id).FirstOrDefault();

            if (existingFunding != null)
            {
                existingFunding = publishedFunding;
            }
            else
            {
                _publishedFunding[specificationId].Add(publishedFunding);
            }

            return Task.FromResult(HttpStatusCode.OK);
        }

        public Task AllPublishedProviderBatchProcessing(Func<List<PublishedProviderVersion>, Task> persistIndexBatch, int batchSize)
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<HttpStatusCode>> UpsertPublishedProviders(IEnumerable<PublishedProvider> publishedProviders)
        {
            List<HttpStatusCode> results = new List<HttpStatusCode>();
            if (publishedProviders.AnyWithNullCheck())
            {
                foreach (PublishedProvider publishedProvider in publishedProviders)
                {
                    string specificationId = publishedProvider.Current.SpecificationId;
                    if (!_publishedProviders.ContainsKey(specificationId))
                    {
                        _publishedProviders.TryAdd(specificationId, new ConcurrentBag<PublishedProvider>());
                    }

                    var existingProvider = _publishedProviders[specificationId].FirstOrDefault(p => p.Id == publishedProvider.Id);
                    if (existingProvider != null)
                    {
                        _publishedProviders[specificationId].TryTake(out existingProvider);
                    }

                    _publishedProviders[specificationId].Add(publishedProvider);

                    results.Add(HttpStatusCode.OK);
                }
            }

            return await Task.FromResult(results);
        }

        public Task<PublishedProvider> AddPublishedProvider(string specificationId, PublishedProvider publishedProvider)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(publishedProvider, nameof(publishedProvider));

            if (!_publishedProviders.ContainsKey(specificationId))
            {
                _publishedProviders[specificationId] = new ConcurrentBag<PublishedProvider>();
            }

            _publishedProviders[specificationId].Add(publishedProvider);

            return Task.FromResult(publishedProvider);
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIdsForApproval(string fundingStreamId, string fundingPeriodId)
        {
            IEnumerable<KeyValuePair<string, string>> results = _publishedProviders
                .SelectMany(c => c.Value)
                .Where(p =>
                    (p.Current.Status == PublishedProviderStatus.Draft || p.Current.Status == PublishedProviderStatus.Updated)
                && p.Current.FundingStreamId == fundingStreamId
                && p.Current.FundingPeriodId == fundingPeriodId)
                .Select(r => new KeyValuePair<string, string>(r.Id, r.ParitionKey));

            return Task.FromResult(results);
        }

        public Task<PublishedProvider> GetPublishedProviderById(string cosmosId, string partitionKey)
        {
            PublishedProvider result = _publishedProviders.SelectMany(c => c.Value).FirstOrDefault(p => p.Id == cosmosId);

            return Task.FromResult(result);
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIds(string fundingStreamId, string fundingPeriodId)
        {
            IEnumerable<KeyValuePair<string, string>> results = _publishedProviders
                            .SelectMany(c => c.Value)
                            .Where(p => p.Current.FundingStreamId == fundingStreamId
                                && p.Current.FundingPeriodId == fundingPeriodId)
                            .Select(r => new KeyValuePair<string, string>(r.Id, r.ParitionKey));

            return Task.FromResult(results);
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedFundingIds(string fundingStreamId, string fundingPeriodId)
        {
            IEnumerable<KeyValuePair<string, string>> results = _publishedFunding
                .SelectMany(c => c.Value)
                .Where(p => p.Current.FundingStreamId == fundingStreamId
                    && p.Current.FundingPeriod.Id == fundingPeriodId)
                .Select(r => new KeyValuePair<string, string>(r.Id, r.ParitionKey));

            return Task.FromResult(results);
        }

        public Task<PublishedFunding> GetPublishedFundingById(string cosmosId, string partitionKey)
        {
            PublishedFunding publishedFunding = _publishedFunding.SelectMany(c => c.Value).FirstOrDefault(p => p.Id == cosmosId);

            return Task.FromResult(publishedFunding);
        }
    }
}
