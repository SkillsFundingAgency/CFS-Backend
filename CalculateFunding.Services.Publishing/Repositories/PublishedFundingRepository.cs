using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Repositories
{
    public class PublishedFundingRepository : IPublishedFundingRepository
    {
        private readonly ICosmosRepository _repository;

        public PublishedFundingRepository(ICosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));

            _repository = cosmosRepository;
        }

        public Task<IEnumerable<PublishedProvider>> GetPublishedProvidersForApproval(
            string specificationId)
        {
            return Task.FromResult(_repository.Query<PublishedProvider>(true)
                .Where(_ => _.Current.SpecificationId == specificationId &&
                            (_.Current.Status == PublishedProviderStatus.Draft ||
                             _.Current.Status == PublishedProviderStatus.Updated))
                .AsEnumerable());
        }

        public Task<IEnumerable<PublishedProvider>> GetLatestPublishedProvidersBySpecification(
            string specificationId)
        {
            return Task.FromResult(_repository.Query<PublishedProvider>(true)
                .Where(_ => _.Current.SpecificationId == specificationId)
                .AsEnumerable());
        }

        public async Task<IEnumerable<HttpStatusCode>> UpsertPublishedProviders(IEnumerable<PublishedProvider> publishedProviders)
        {
            Guard.ArgumentNotNull(publishedProviders, nameof(publishedProviders));

            IEnumerable<Task<HttpStatusCode>> tasks = publishedProviders.Select(async (_) => await _repository.UpsertAsync(_));

            await TaskHelper.WhenAllAndThrow(tasks.ToArray());

            return tasks.Select(_ => _.Result);
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) cosmosRepoHealth = await _repository.IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(PublishedFundingRepository)
            };

            health.Dependencies.Add(new DependencyHealth
            {
                HealthOk = cosmosRepoHealth.Ok,
                DependencyName = _repository.GetType().GetFriendlyName(),
                Message = cosmosRepoHealth.Message
            });

            return health;
        }

        public async Task<PublishedProviderVersion> GetPublishedProviderVersion(string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            string version)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));
            Guard.IsNullOrWhiteSpace(version, nameof(version));


            string id = $"publishedprovider-{fundingStreamId}-{fundingPeriodId}-{providerId}-{version}";

            return (await _repository.ReadAsync<PublishedProviderVersion>(id, true))?.Content;
        }

        public Task<IEnumerable<PublishedFunding>> GetLatestPublishedFundingBySpecification(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            return Task.FromResult(_repository.Query<PublishedFunding>(true)
            .Where(_ => _.Current.SpecificationId == specificationId)
            .AsEnumerable());
        }

        public async Task<HttpStatusCode> UpsertPublishedFunding(PublishedFunding publishedFunding)
        {
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));

            return await _repository.UpsertAsync(publishedFunding);
        }
    }
}