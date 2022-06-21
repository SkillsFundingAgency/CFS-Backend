using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ReleaseApprovedProvidersService : IReleaseApprovedProvidersService
    {
        private readonly IPublishService _publishService;
        private readonly IPublishedProvidersLoadContext _publishedProvidersLoadContext;
        private readonly IReleaseToChannelSqlMappingContext _releaseToChannelSqlMappingContext;

        public ReleaseApprovedProvidersService(IPublishService publishService,
            IPublishedProvidersLoadContext publishedProvidersLoadContext,
            IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext)
        {
            Guard.ArgumentNotNull(publishService, nameof(publishService));
            Guard.ArgumentNotNull(publishedProvidersLoadContext, nameof(publishedProvidersLoadContext));
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));

            _publishService = publishService;
            _publishedProvidersLoadContext = publishedProvidersLoadContext;
            _releaseToChannelSqlMappingContext = releaseToChannelSqlMappingContext;
        }

        public async Task<IEnumerable<string>> ReleaseProvidersInApprovedState(SpecificationSummary specification, bool retrying = false)
        {
            Guard.ArgumentNotNull(specification, nameof(specification));

            PublishedProvider[] providersToRelease = _publishedProvidersLoadContext.Values.ToArray();

            // if not in a retry loop then filter out providers which are not in the approved state
            // if this is a retry then we may need to re-generate missing documents so we need to process providers regardless
            if (!retrying)
            {
                providersToRelease = providersToRelease
                    .Where(_ => _.Current.Status == PublishedProviderStatus.Approved).ToArray(); // Ensure array so cosmos query doesn't get loaded multiple times each eval
            }


            if (providersToRelease.Any())
            {
                PublishedProviderIdsRequest publishedProviderIdsRequest = new PublishedProviderIdsRequest()
                {
                    PublishedProviderIds = providersToRelease.Select(_ => _.Current.PublishedProviderId),
                };

                await _publishService.PublishProviderFundingResults(true,
                                                                    _releaseToChannelSqlMappingContext.Author,
                                                                    _releaseToChannelSqlMappingContext.JobId,
                                                                    _releaseToChannelSqlMappingContext.CorrelationId,
                                                                    specification,
                                                                    publishedProviderIdsRequest,
                                                                    false);
            }

            return providersToRelease.Select(_ => _.Current.ProviderId);
        }
    }
}
