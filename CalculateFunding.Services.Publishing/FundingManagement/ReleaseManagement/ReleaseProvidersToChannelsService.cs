using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing.FundingManagement;
using CalculateFunding.Services.Publishing.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ReleaseProvidersToChannelsService : IReleaseProvidersToChannelsService
    {
        private readonly ISpecificationService _specificationService;
        private readonly IPoliciesService _policiesService;
        private readonly IChannelsService _channelService;
        private readonly IPublishedProvidersLoadContext _publishProvidersLoadContext;
        private readonly IReleaseApprovedProvidersService _releaseApprovedProvidersService;

        public ReleaseProvidersToChannelsService(ISpecificationService specificationService,
            IPoliciesService policiesService,
            IChannelsService channelService,
            IPublishedProvidersLoadContext publishProvidersLoadContext,
            IReleaseApprovedProvidersService releaseApprovedProvidersService)
        {
            _specificationService = specificationService;
            _policiesService = policiesService;
            _channelService = channelService;
            _publishProvidersLoadContext = publishProvidersLoadContext;
            _releaseApprovedProvidersService = releaseApprovedProvidersService;
        }

        public async Task ReleaseProviderVersions(
         string specificationId,
        ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest,
        Reference author,
        string correlationId
        )
        {
            SpecificationSummary specification = await _specificationService.GetSpecificationSummaryById(specificationId);
            if (specification == null)
            {
                throw new InvalidOperationException("Specification not found");
            }

            if (!specification.IsSelectedForFunding)
            {
                throw new InvalidOperationException("Specification is not chosen for funding");
            }

            string fundingStreamId = specification.FundingStreams.First().Id;

            FundingConfiguration fundingConfiguration = await _policiesService.GetFundingConfiguration(fundingStreamId, specification.FundingPeriod.Id);

            IEnumerable<KeyValuePair<string, SqlModels.Channel>> channels = await _channelService.GetAndVerifyChannels(releaseProvidersToChannelRequest.Channels);

            _publishProvidersLoadContext.SetSpecDetails(fundingStreamId, specification.FundingPeriod.Id);

            await LoadGivenProvidersFromFundingApprovals(releaseProvidersToChannelRequest);

            IEnumerable<string> providerIdsReleased = await _releaseApprovedProvidersService.ReleaseProvidersInApprovedState(author, correlationId, specification);

            await RefreshLoadContextWithProvidersApprovedNowReleased(providerIdsReleased);

        }

        private async Task RefreshLoadContextWithProvidersApprovedNowReleased(IEnumerable<string> providerIdsReleased)
        {
            await _publishProvidersLoadContext.LoadProviders(providerIdsReleased);
        }

        private async Task LoadGivenProvidersFromFundingApprovals(ReleaseProvidersToChannelRequest releaseProvidersToChannelRequest)
        {
            await _publishProvidersLoadContext.LoadProviders(releaseProvidersToChannelRequest.ProviderIds);
        }
    }
}
