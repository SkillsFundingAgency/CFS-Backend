﻿using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
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

        public ReleaseApprovedProvidersService(IPublishService publishService,
            IPublishedProvidersLoadContext publishedProvidersLoadContext)
        {
            Guard.ArgumentNotNull(publishService, nameof(publishService));
            Guard.ArgumentNotNull(publishedProvidersLoadContext, nameof(publishedProvidersLoadContext));

            _publishService = publishService;
            _publishedProvidersLoadContext = publishedProvidersLoadContext;
        }

        public async Task<IEnumerable<string>> ReleaseProvidersInApprovedState(Reference author, string correlationId, SpecificationSummary specification)
        {
            Guard.ArgumentNotNull(author, nameof(author));
            Guard.IsNullOrWhiteSpace(correlationId, correlationId);
            Guard.ArgumentNotNull(specification, nameof(specification));

            IEnumerable<PublishedProvider> providersToRelease = _publishedProvidersLoadContext
                .Values
                .Where(_ => _.Current.Status == PublishedProviderStatus.Approved);

            if (providersToRelease.Any())
            {
                PublishedProviderIdsRequest publishedProviderIdsRequest = new PublishedProviderIdsRequest()
                {
                    PublishedProviderIds = providersToRelease.Select(_ => _.Current.ProviderId),
                };

                await _publishService.PublishProviderFundingResults(true, author, correlationId, specification, publishedProviderIdsRequest);
            }

            return providersToRelease.Select(_ => _.Current.ProviderId);
        }
    }
}