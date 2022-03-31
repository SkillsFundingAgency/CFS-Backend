using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryPublishedFundingBulkRepository : IPublishedFundingBulkRepository
    {
        private readonly InMemoryCosmosRepository _repo;

        public InMemoryPublishedFundingBulkRepository(InMemoryCosmosRepository inMemoryCosmosRepository)
        {
            _repo = inMemoryCosmosRepository;
        }

        public Task<IEnumerable<PublishedFunding>> GetPublishedFundings(
            IEnumerable<KeyValuePair<string, string>> publishedFundingIds)
        {
            List<PublishedFunding> publishedFundings = new List<PublishedFunding>();

            foreach (KeyValuePair<string, string> publishedFundingId in publishedFundingIds)
            {
                PublishedFunding publishedFunding = _repo
                .PublishedFunding
                .SelectMany(c => c.Value)
                .FirstOrDefault(p => p.Id == publishedFundingId.Key);

                if (publishedFunding != null)
                {
                    publishedFundings.Add(publishedFunding);
                }
            }

            return Task.FromResult(publishedFundings.AsEnumerable());
        }

        public Task<IEnumerable<PublishedFundingVersion>> GetPublishedFundingVersions(
            IEnumerable<KeyValuePair<string, string>> publishedFundingVersionIds)
        {
            List<PublishedFundingVersion> publishedFundingVersions = new List<PublishedFundingVersion>();

            foreach (KeyValuePair<string, string> publishedFundingVersionId in publishedFundingVersionIds)
            {
                PublishedFundingVersion publishedFundingVersion = _repo
                .PublishedFundingVersions
                .SelectMany(c => c.Value)
                .FirstOrDefault(p => p.Value.Id == publishedFundingVersionId.Key)
                .Value;

                if (publishedFundingVersion != null)
                {
                    publishedFundingVersions.Add(publishedFundingVersion);
                }
            }

            return Task.FromResult(publishedFundingVersions.AsEnumerable());
        }

        public Task<IEnumerable<PublishedProvider>> GetPublishedProviders(
            IEnumerable<KeyValuePair<string, string>> publishedProviderIds)
        {
            List<PublishedProvider> publishedProviders = new List<PublishedProvider>();

            foreach (KeyValuePair<string, string> publishedProviderId in publishedProviderIds)
            {
                PublishedProvider publishedProvider = _repo
                .PublishedProviders
                .SelectMany(c => c.Value)
                .FirstOrDefault(p => p.Key == publishedProviderId.Key).Value;

                if (publishedProvider != null)
                {
                    publishedProviders.Add(publishedProvider);
                }
            }

            return Task.FromResult(publishedProviders.AsEnumerable());
        }

        public Task<IEnumerable<PublishedProviderVersion>> GetPublishedProviderVersions(IEnumerable<KeyValuePair<string, string>> publishedProviderVersionIds)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<PublishedProvider>> TryGetPublishedProvidersByProviderId(IEnumerable<string> providerIds, string fundingStreamId, string fundingPeriodId)
        {
            return Task.FromResult(_repo.PublishedProviders
                .SelectMany(c => c.Value)
                .Where(_ => _.Value.Current.FundingStreamId == fundingStreamId
                && _.Value.Current.FundingPeriodId == fundingPeriodId
                && providerIds.Contains(_.Value.Current.ProviderId)).Select(p => p.Value));
        }

        public Task UpsertPublishedFundings(
            IEnumerable<PublishedFunding> publishedFundings,
            Action<Task<HttpStatusCode>, PublishedFunding> continueAction)
        {
            foreach (PublishedFunding publishedFunding in publishedFundings)
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
            }

            return Task.FromResult(HttpStatusCode.OK);
        }

        public Task UpsertPublishedProviders(
            IEnumerable<PublishedProvider> publishedProviders,
            Action<Task<HttpStatusCode>> continueAction)
        {
            foreach (PublishedProvider publishedProvider in publishedProviders)
            {
                List<HttpStatusCode> results = new List<HttpStatusCode>();
                string specificationId = publishedProvider.Current.SpecificationId;
                if (!_repo.PublishedProviders.ContainsKey(specificationId))
                {
                    _repo.PublishedProviders.TryAdd(specificationId, new ConcurrentDictionary<string, PublishedProvider>());
                }

                var existingProvider = _repo.PublishedProviders[specificationId].FirstOrDefault(p => p.Key == publishedProvider.Id).Value;
                if (existingProvider != null)
                {
                    existingProvider.Current = publishedProvider.Current;
                }
                else
                {
                    _repo.PublishedProviders[specificationId].TryAdd(publishedProvider.Id, publishedProvider);
                }
            }

            return Task.FromResult(HttpStatusCode.OK);
        }
    }
}
