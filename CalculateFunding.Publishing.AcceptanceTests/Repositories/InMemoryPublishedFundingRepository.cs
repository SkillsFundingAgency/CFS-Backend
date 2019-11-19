﻿using System;
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
        public InMemoryPublishedFundingRepository(InMemoryCosmosRepository inMemoryCosmosRepository)
        {
            _repo = inMemoryCosmosRepository;
        }

     
        private readonly InMemoryCosmosRepository _repo;

        public Task<IEnumerable<PublishedFunding>> GetLatestPublishedFundingBySpecification(string specificationId)
        {
            IEnumerable<PublishedFunding> result = null;

            if (_repo.PublishedFunding.ContainsKey(specificationId))
            {
                result = _repo.PublishedFunding[specificationId];
            }

            return Task.FromResult(result);
        }

        public Task<IEnumerable<PublishedProvider>> GetLatestPublishedProvidersBySpecification(string specificationId)
        {
            IEnumerable<PublishedProvider> result = null;
            if (_repo.PublishedProviders.ContainsKey(specificationId))
            {
                result = _repo.PublishedProviders[specificationId];
            }

            return Task.FromResult(result);
        }

        public Task<IEnumerable<PublishedProvider>> GetPublishedProvidersForApproval(string specificationId)
        {
            IEnumerable<PublishedProvider> results = _repo.PublishedProviders[specificationId].Where(p =>
             p.Current.Status == PublishedProviderStatus.Draft || p.Current.Status == PublishedProviderStatus.Updated);

            return Task.FromResult(results);
        }

        public Task<PublishedProviderVersion> GetPublishedProviderVersion(string fundingStreamId, string fundingPeriodId, string providerId, string version)
        {
            PublishedProvider publishedProvider = _repo.PublishedProviders.SelectMany(c => c.Value).Where(p =>
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

            if (!_repo.PublishedFunding.ContainsKey(specificationId))
            {
                _repo.PublishedFunding.TryAdd(specificationId, new ConcurrentBag<PublishedFunding>());
            }

            List<PublishedFunding> itemsToRemove = new List<PublishedFunding>();

            PublishedFunding existingFunding = _repo.PublishedFunding[specificationId].Where(p => p.Id == publishedFunding.Id).FirstOrDefault();

            if (existingFunding != null)
            {
                existingFunding = publishedFunding;
            }
            else
            {
                _repo.PublishedFunding[specificationId].Add(publishedFunding);
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
                    if (!_repo.PublishedProviders.ContainsKey(specificationId))
                    {
                        _repo.PublishedProviders.TryAdd(specificationId, new ConcurrentBag<PublishedProvider>());
                    }

                    var existingProvider = _repo.PublishedProviders[specificationId].FirstOrDefault(p => p.Id == publishedProvider.Id);
                    if (existingProvider != null)
                    {

                        existingProvider.Current = publishedProvider.Current;
                    }
                    else
                    {
                        _repo.PublishedProviders[specificationId].Add(publishedProvider);

                    }
                    results.Add(HttpStatusCode.OK);
                }
            }

            return await Task.FromResult(results);
        }

        public Task<PublishedProvider> AddPublishedProvider(string specificationId, PublishedProvider publishedProvider)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(publishedProvider, nameof(publishedProvider));

            if (!_repo.PublishedProviders.ContainsKey(specificationId))
            {
                _repo.PublishedProviders[specificationId] = new ConcurrentBag<PublishedProvider>();
            }

            _repo.PublishedProviders[specificationId].Add(publishedProvider);

            return Task.FromResult(publishedProvider);
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIdsForApproval(string fundingStreamId, string fundingPeriodId)
        {
            IEnumerable<KeyValuePair<string, string>> results = _repo.PublishedProviders
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
            PublishedProvider result = _repo.PublishedProviders.SelectMany(c => c.Value).FirstOrDefault(p => p.Id == cosmosId);

            return Task.FromResult(result);
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIds(string fundingStreamId, string fundingPeriodId)
        {
            IEnumerable<KeyValuePair<string, string>> results = _repo.PublishedProviders
                            .SelectMany(c => c.Value)
                            .Where(p => p.Current.FundingStreamId == fundingStreamId
                                && p.Current.FundingPeriodId == fundingPeriodId)
                            .Select(r => new KeyValuePair<string, string>(r.Id, r.ParitionKey));

            return Task.FromResult(results);
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedFundingIds(string fundingStreamId, string fundingPeriodId)
        {
            IEnumerable<KeyValuePair<string, string>> results = _repo.PublishedFunding
                .SelectMany(c => c.Value)
                .Where(p => p.Current.FundingStreamId == fundingStreamId
                    && p.Current.FundingPeriod.Id == fundingPeriodId)
                .Select(r => new KeyValuePair<string, string>(r.Id, r.ParitionKey));

            return Task.FromResult(results);
        }

        public Task<PublishedFunding> GetPublishedFundingById(string cosmosId, string partitionKey)
        {
            PublishedFunding publishedFunding = _repo.PublishedFunding.SelectMany(c => c.Value).FirstOrDefault(p => p.Id == cosmosId);

            return Task.FromResult(publishedFunding);
        }

        public Task<IEnumerable<PublishedProviderFundingStreamStatus>> GetPublishedProviderStatusCounts(string specificationId)
        {
            IEnumerable<PublishedProviderFundingStreamStatus> statuses = _repo.PublishedFunding
                .SelectMany(c => c.Value)
                .Where(p => p.Current.SpecificationId == specificationId)
                .GroupBy(p => new { p.Current.FundingStreamId, p.Current.Status })
                .Select(r => new PublishedProviderFundingStreamStatus
                {
                    FundingStreamId = r.Key.FundingStreamId,
                    Status = Enum.GetName(r.Key.Status.GetType(), r.Key.Status),
                    Count = r.Count()
                });

            return Task.FromResult(statuses);
        }
    }
}
