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
using CalculateFunding.Services.Publishing.Models;
using GroupingReason = CalculateFunding.Services.Publishing.GroupingReason;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class InMemoryPublishedFundingRepository : IPublishedFundingRepository
    {
        public InMemoryPublishedFundingRepository(InMemoryCosmosRepository inMemoryCosmosRepository)
        {
            _repo = inMemoryCosmosRepository;
        }
     
        private readonly InMemoryCosmosRepository _repo;

        public IEnumerable<PublishedProvider> GetInMemoryPublishedProviders(string specificationId)
        {
            return _repo.PublishedProviders.TryGetValue(specificationId,
                out ConcurrentBag<PublishedProvider> publishedProviders)
                ? publishedProviders
                : (IEnumerable<PublishedProvider>) new PublishedProvider[0];
        }

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

        public Task AllPublishedProviderBatchProcessing(Func<List<PublishedProvider>, Task> persistIndexBatch, int batchSize, string specificationId = null)
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

        public Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIdsForApproval(string fundingStreamId, string fundingPeriodId, string[] providerIds = null)
        {
            IEnumerable<PublishedProvider> publishedProviders = _repo.PublishedProviders
                .SelectMany(c => c.Value)
                .Where(p =>
                    (p.Current.Status == PublishedProviderStatus.Draft || p.Current.Status == PublishedProviderStatus.Updated)
                && p.Current.FundingStreamId == fundingStreamId
                && p.Current.FundingPeriodId == fundingPeriodId);

            if (providerIds != null && providerIds.Any())
            {
                publishedProviders = publishedProviders.Where(_ => providerIds.Contains(_.Current.ProviderId));
            }

            IEnumerable<KeyValuePair<string, string>> results = publishedProviders.Select(r => new KeyValuePair<string, string>(r.Id, r.PartitionKey));

            return Task.FromResult(results);
        }

        public Task<PublishedProvider> GetPublishedProviderById(string cosmosId, string partitionKey)
        {
            PublishedProvider result = _repo.PublishedProviders.SelectMany(c => c.Value).FirstOrDefault(p => p.Id == cosmosId);

            return Task.FromResult(result);
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedProviderIds(string fundingStreamId, string fundingPeriodId, string[] providerIds = null)
        {
            IEnumerable<PublishedProvider> publishedProviders = _repo.PublishedProviders
                .SelectMany(c => c.Value)
                .Where(p => p.Current.FundingStreamId == fundingStreamId
                    && p.Current.FundingPeriodId == fundingPeriodId);

            if (providerIds != null && providerIds.Any())
            {
                publishedProviders = publishedProviders.Where(_ => providerIds.Contains(_.Current.ProviderId));
            }

            IEnumerable<KeyValuePair<string, string>> results = publishedProviders.Select(r => new KeyValuePair<string, string>(r.Id, r.PartitionKey));

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

        public Task<IEnumerable<PublishedProviderFundingStreamStatus>> GetPublishedProviderStatusCounts(string specificationId, string providerType, string localAuthority, string status)
        {
            IEnumerable<PublishedFunding> publishedFundings = _repo.PublishedFunding
                .SelectMany(c => c.Value)
                .Where(p => p.Current.SpecificationId == specificationId);

            if (!string.IsNullOrEmpty(status))
            {
                publishedFundings = publishedFundings.Where(p => p.Current.Status.ToString() == status);
            }

            IEnumerable<PublishedProviderFundingStreamStatus> statuses = publishedFundings
                .GroupBy(p => new { p.Current.FundingStreamId, p.Current.Status })
                .Select(r => new PublishedProviderFundingStreamStatus
                {
                    FundingStreamId = r.Key.FundingStreamId,
                    Status = Enum.GetName(r.Key.Status.GetType(), r.Key.Status),
                    Count = r.Count(),
                    TotalFunding = r.Sum(x=>x.Current.TotalFunding)
                });

            return Task.FromResult(statuses);
        }

        public Task DeleteAllPublishedProvidersByFundingStreamAndPeriod(string fundingStreamId, string fundingPeriodId)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAllPublishedProviderVersionsByFundingStreamAndPeriod(string fundingStreamId, string fundingPeriodId)
        {
            throw new NotImplementedException();
        }

        public Task<PublishedProviderVersion> GetLatestPublishedProviderVersion(string fundingStreamId, string fundingPeriodId, string providerId)
        {
            throw new NotImplementedException();
        }

        public Task PublishedProviderBatchProcessing(string predicate, string specificationId, Func<List<PublishedProvider>, Task> batchProcessor, int batchSize,
            string joinPredicate = null, string fundingLineCode = null)
        {
            throw new NotImplementedException();
        }

        public Task PublishedProviderVersionBatchProcessing(string predicate, string specificationId, Func<List<PublishedProviderVersion>, Task> batchProcessor, int batchSize, string joinPredicate = null, string fundingLineCode = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<PublishedProviderVersion>> GetPublishedProviderVersions(string specificationId, string providerId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<dynamic>> GetFundings(string publishedProviderVersion)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<PublishedProviderVersion>> GetPublishedProviderVersions(string fundingStreamId, string fundingPeriodId, string providerId, string status = null)
        {
            throw new NotImplementedException();
        }

        public Task RefreshedProviderVersionBatchProcessing(string specificationId, Func<List<PublishedProviderVersion>, Task> persistIndexBatch, int batchSize)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<string>> GetPublishedProviderFundingLines(string specificationId, GroupingReason fundingLineType)
        {
            IEnumerable<PublishedProvider> publishedProviders = null;
            if (_repo.PublishedProviders.ContainsKey(specificationId))
            {
                publishedProviders = _repo.PublishedProviders[specificationId];
            }

            IEnumerable<string> fundingLines = publishedProviders
                .SelectMany(x => x.Current.FundingLines)
                .Select(x => x.Name)
                .Distinct();

            return Task.FromResult(fundingLines);
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedFundingVersionIds(string fundingStreamId, string fundingPeriodId)
        {
            IEnumerable<KeyValuePair<string, string>> results = _repo.PublishedFundingVersions
                .SelectMany(c => c.Value)
                .Where(p => p.Value.FundingStreamId == fundingStreamId
                    && p.Value.FundingPeriod.Id == fundingPeriodId)
                .Select(r => new KeyValuePair<string, string>(r.Value.Id, r.Value.PartitionKey));

            return Task.FromResult(results);
        }

        public Task<PublishedFundingVersion> GetPublishedFundingVersionById(string cosmosId, string partitionKey)
        {
            PublishedFundingVersion publishedFunding = _repo.PublishedFundingVersions.SelectMany(c => c.Value).FirstOrDefault(p => p.Value.Id == cosmosId).Value;

            return Task.FromResult(publishedFunding);
        }

        public Task DeleteAllPublishedFundingsByFundingStreamAndPeriod(string fundingStreamId, string fundingPeriodId)
        {
            throw new NotImplementedException();
        }

        public Task DeleteAllPublishedFundingVersionsByFundingStreamAndPeriod(string fundingStreamId, string fundingPeriodId)
        {
            throw new NotImplementedException();
        }

        public Task<HttpStatusCode> UpsertPublishedProvider(PublishedProvider publishedProvider)
        {
            List<HttpStatusCode> results = new List<HttpStatusCode>();
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
            return Task.FromResult(HttpStatusCode.OK);
        }

        public Task<PublishedProvider> GetPublishedProvider(string fundingStreamId, string fundingPeriodId, string providerId)
        {
            PublishedProvider publishedProvider = _repo.PublishedProviders.SelectMany(c => c.Value).Where(p =>
                  p.Current.FundingStreamId == fundingStreamId
                  && p.Current.FundingPeriodId == fundingPeriodId
                  && p.Current.ProviderId == providerId).FirstOrDefault();

            return Task.FromResult(publishedProvider);
        }

        public Task<IEnumerable<PublishedFundingIndex>> QueryPublishedFunding(IEnumerable<string> fundingStreamIds, IEnumerable<string> fundingPeriodIds, IEnumerable<string> groupingReasons, IEnumerable<string> variationReasons, int top, int? pageRef)
        {
            throw new NotImplementedException();
        }

        public Task<int> QueryPublishedFundingCount(IEnumerable<string> fundingStreamIds, IEnumerable<string> fundingPeriodIds, IEnumerable<string> groupingReasons, IEnumerable<string> variationReasons)
        {
            throw new NotImplementedException();
        }

        public Task<(string providerVersionId, string providerId)> GetPublishedProviderId(string publishedProviderVersion)
        {
            PublishedProvider publishedProvider = _repo.PublishedProviders.SelectMany(c => c.Value).Where(p =>
                p.Released.FundingId == publishedProviderVersion).FirstOrDefault();

            return Task.FromResult((
                providerVersionId: publishedProvider?.Released?.Provider?.ProviderVersionId, 
                providerId: publishedProvider?.Released?.Provider?.ProviderId));
        }

        public Task PublishedGroupBatchProcessing(string specificationId, Func<List<PublishedFunding>, Task> batchProcessor, int batchSize)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<PublishedProvider>> QueryPublishedProvider(string specificationId, IEnumerable<string> fundingIds)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<KeyValuePair<string, string>>> GetPublishedFundingIds(string specificationId, GroupingReason? groupingReason = null)
        {
            IEnumerable<KeyValuePair<string, string>> results = _repo.PublishedFunding
                .SelectMany(c => c.Value)
                .Where(p => p.Current.SpecificationId == specificationId)
                .Select(r => new KeyValuePair<string, string>(r.Id, r.ParitionKey));

            return Task.FromResult(results);
        }

        public Task PublishedFundingBatchProcessing(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            Func<List<PublishedFunding>, Task> batchProcessor,
            int batchSize) =>
            throw new NotImplementedException();

        public Task PublishedFundingVersionBatchProcessing(string specificationId,
            string fundingStreamId,
            string fundingPeriodId,
            Func<List<PublishedFundingVersion>, Task> batchProcessor,
            int batchSize) =>
            throw new NotImplementedException();

        public Task<IEnumerable<string>> GetPublishedProviderErrorSummaries(string specificationId)
        {
            IEnumerable<string> errorMessageSummaries = _repo.PublishedProviders
                .SelectMany(c => c.Value)
                .Where(_ => _.Current.Errors != null && _.Current.Errors.Any())
                .SelectMany(_ => _.Current.Errors)
                .Select(_ => _.SummaryErrorMessage);

            return Task.FromResult(errorMessageSummaries);
        }

        public Task<PublishedProviderVersion> GetLatestPublishedProviderVersionBySpecificationId(
            string specificationId,
            string fundingStreamId,
            string providerId) =>
            Task.FromResult(
                _repo.PublishedProviders
                    .SelectMany(c => c.Value)
                    .SingleOrDefault(_ =>
                        _.Current.SpecificationId == specificationId &&
                        _.Current.FundingStreamId == fundingStreamId &&
                        _.Current.ProviderId == providerId)
                    ?.Current);

        public Task<IEnumerable<PublishedProviderVersion>> GetPublishedProviderVersionsForApproval(
            string specificationId, 
            string fundingStreamId, 
            string providerId) =>
                throw new NotImplementedException();

        public Task<IEnumerable<PublishedProviderFunding>> GetPublishedProvidersFunding(IEnumerable<string> publishedProviderIds, string specificationId, params PublishedProviderStatus[] statuses)
        {
            throw new NotImplementedException();
        }

        public Task<PublishedProviderVersion> GetPublishedProviderVersionById(string publishedProviderVersionId)
        {
            throw new NotImplementedException();
        }
    }
}
