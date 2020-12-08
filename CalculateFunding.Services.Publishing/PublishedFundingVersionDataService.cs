using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using Polly;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedFundingVersionDataService : IPublishedFundingVersionDataService
    {
        private readonly AsyncPolicy _publishedFundingRepositoryPolicy;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly IPublishedFundingBulkRepository _publishedFundingBulkRepository;

        public PublishedFundingVersionDataService(
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPublishedFundingRepository publishedFundingRepository,
            IPublishedFundingBulkRepository publishedFundingBulkRepository)
        {
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishedFundingBulkRepository, nameof(publishedFundingBulkRepository));

            _publishedFundingRepositoryPolicy = publishingResiliencePolicies.PublishedFundingRepository;
            _publishedFundingRepository = publishedFundingRepository;
            _publishedFundingBulkRepository = publishedFundingBulkRepository;
        }

        public async Task<IEnumerable<PublishedFundingVersion>> GetPublishedFundingVersion(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            IEnumerable<KeyValuePair<string, string>> publishedFundingIds = await _publishedFundingRepositoryPolicy.ExecuteAsync(
                                () => _publishedFundingRepository.GetPublishedFundingVersionIds(fundingStreamId, fundingPeriodId));

            return await _publishedFundingBulkRepository.GetPublishedFundingVersions(publishedFundingIds);
        }
    }
}
