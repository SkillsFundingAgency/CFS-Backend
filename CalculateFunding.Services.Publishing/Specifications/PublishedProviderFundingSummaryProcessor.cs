using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Models;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class PublishedProviderFundingSummaryProcessor : IPublishedProviderFundingSummaryProcessor
    {
        private readonly IReleaseManagementRepository _releaseManagementRepository;
        private readonly IProvidersForChannelFilterService _providersForChannelFilterService;
        private readonly IChannelOrganisationGroupGeneratorService _channelOrganisationGroupGeneratorService;
        private readonly IPublishedProviderLookupService _publishedProviderLookupService;

        public PublishedProviderFundingSummaryProcessor(IProducerConsumerFactory producerConsumerFactory,
            IReleaseManagementRepository releaseManagementRepository,
            IProvidersForChannelFilterService providersForChannelFilterService,
            IChannelOrganisationGroupGeneratorService channelOrganisationGroupGeneratorService,
            IPublishedProviderLookupService publishedProviderLookupService)
        {
            Guard.ArgumentNotNull(producerConsumerFactory, nameof(producerConsumerFactory));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(providersForChannelFilterService, nameof(providersForChannelFilterService));
            Guard.ArgumentNotNull(channelOrganisationGroupGeneratorService, nameof(channelOrganisationGroupGeneratorService));
            Guard.ArgumentNotNull(publishedProviderLookupService, nameof(publishedProviderLookupService));

            _releaseManagementRepository = releaseManagementRepository;
            _providersForChannelFilterService = providersForChannelFilterService;
            _channelOrganisationGroupGeneratorService = channelOrganisationGroupGeneratorService;
            _publishedProviderLookupService = publishedProviderLookupService;
        }

        public async Task<IEnumerable<ProviderVersionInChannel>> GetProviderVersionInFundingConfiguration(
            string specificationId,
            FundingConfiguration fundingConfiguration)
        {
            if (fundingConfiguration.ReleaseChannels.IsNullOrEmpty())
            {
                return Enumerable.Empty<ProviderVersionInChannel>();
            }

            IEnumerable<Channel> channels = await GetChannels(fundingConfiguration.ReleaseChannels.Where(rc => rc.IsVisible).Select(_ => _.ChannelCode));

            IEnumerable<ProviderVersionInChannel> latestPublishedProviderVersions =
                await _releaseManagementRepository.GetLatestPublishedProviderVersions(specificationId, channels.Select(_ => _.ChannelId));

            return latestPublishedProviderVersions;
        }

        public async Task<ReleaseFundingPublishedProvidersSummary> GetFundingSummaryForApprovedPublishedProvidersByChannel(IEnumerable<string> publishedProviderIds,
            SpecificationSummary specificationSummary,
            FundingConfiguration fundingConfiguration,
            IEnumerable<string> channelCodes)
        {
            Guard.ArgumentNotNull(specificationSummary, nameof(specificationSummary));
            Guard.ArgumentNotNull(fundingConfiguration, nameof(fundingConfiguration));
            Guard.IsNotEmpty(channelCodes, nameof(channelCodes));

            if (fundingConfiguration.ApprovalMode != ApprovalMode.All && publishedProviderIds.IsNullOrEmpty())
            {
                Guard.ArgumentNotNull(publishedProviderIds, nameof(publishedProviderIds));
            }

            IEnumerable<Channel> channels = await GetChannels(channelCodes.Where(_ => fundingConfiguration.ReleaseChannels.Any(rc => rc.IsVisible && rc.ChannelCode == _)));

            IEnumerable<PublishedProviderFundingSummary> approvedOrReleasedPublishedProviders = await _publishedProviderLookupService.GetPublishedProviderFundingSummaries(
                    specificationSummary,
                    new[] { PublishedProviderStatus.Approved, PublishedProviderStatus.Released },
                    publishedProviderIds
            );

            List<PublishedProviderFundingSummary> finalSummaries = new List<PublishedProviderFundingSummary>();

            IEnumerable<ProviderVersionInChannel> latestPublishedProviderVersions =
                await _releaseManagementRepository.GetLatestPublishedProviderVersions(specificationSummary.Id, channels.Select(_=>_.ChannelId));

            foreach (Channel channel in channels)
            {
                List<PublishedProviderFundingSummary> eligibleProviders = new List<PublishedProviderFundingSummary>();

                IEnumerable<PublishedProviderFundingSummary> approvedNewVersionPublishedProviders =
                    approvedOrReleasedPublishedProviders
                        .Where(_ => _.Status == PublishedProviderStatus.Approved.ToString())
                        .Select(_ => _);

                eligibleProviders.AddRange(approvedNewVersionPublishedProviders);

                IEnumerable<PublishedProviderFundingSummary> releasedProviders = approvedOrReleasedPublishedProviders
                    .Where(_=>_.Status == PublishedProviderStatus.Released.ToString())
                    .Select(_=>_);

                foreach (PublishedProviderFundingSummary provider in releasedProviders)
                {
                    ProviderVersionInChannel providerVersionInChannel = latestPublishedProviderVersions
                        .FirstOrDefault(_ => _.ProviderId == provider.Provider.ProviderId && _.ChannelId == channel.ChannelId);

                    if (providerVersionInChannel == null || ProviderHasNewerVersionWhichHasNotBeenReleasedInThisChannel(provider, providerVersionInChannel))
                    {
                        eligibleProviders.Add(provider);
                    }
                }

                IEnumerable<PublishedProviderVersion> publishedProviderVersions = eligibleProviders
                    .Select(_ => new PublishedProviderVersion
                    {
                        SpecificationId = _.SpecificationId,
                        Provider = _.Provider,
                        TotalFunding = _.TotalFunding,
                        IsIndicative = _.IsIndicative,
                        MajorVersion = _.MajorVersion,
                        MinorVersion = _.MinorVersion,
                    });

                IEnumerable<PublishedProviderVersion> filteredPublishedProviders = _providersForChannelFilterService.FilterProvidersForChannel(
                    channel, publishedProviderVersions, fundingConfiguration);

                IEnumerable<OrganisationGroupResult> organisationGroupResults =
                    await _channelOrganisationGroupGeneratorService.GenerateOrganisationGroups(
                        channel, fundingConfiguration, specificationSummary, filteredPublishedProviders);

                Dictionary<string, string> providersInOrganisationGroupResultLookup = organisationGroupResults
                    .SelectMany(_ => _.Providers)
                    .Select(_ => _.ProviderId)
                    .Distinct()
                    .ToDictionary(_ => _);

                IEnumerable<PublishedProviderFundingSummary> orgFilteredPublishedProviders = filteredPublishedProviders
                    .Where(_ => providersInOrganisationGroupResultLookup.ContainsKey(_.Provider.ProviderId))
                    .Select(_ => new PublishedProviderFundingSummary
                    {
                        TotalFunding = _.TotalFunding,
                        IsIndicative = _.IsIndicative,
                        Provider = _.Provider,
                        ChannelCode = channel.ChannelCode,
                        ChannelName = channel.ChannelName
                    });

                finalSummaries.AddRange(orgFilteredPublishedProviders);
            }

            ReleaseFundingPublishedProvidersSummary result = new ReleaseFundingPublishedProvidersSummary
            {
                TotalProviders = finalSummaries.Select(_ => _.Provider.ProviderId).Distinct().Count(),
                TotalIndicativeProviders = finalSummaries.Where(_ => _.IsIndicative).Select(_ => _.Provider.ProviderId).Distinct().Count(),
                TotalFunding = finalSummaries.GroupBy(_ => _.Provider.ProviderId).Select(_ => _.First().TotalFunding).Sum(),
                ChannelFundings = GenerateChannelFundings(finalSummaries)
            };

            return result;

            static bool ProviderHasNewerVersionWhichHasNotBeenReleasedInThisChannel(PublishedProviderFundingSummary provider, ProviderVersionInChannel providerVersionInChannel)
            {
                return provider.MajorVersion > providerVersionInChannel.MajorVersion;
            }
        }

        private IEnumerable<ChannelFunding> GenerateChannelFundings(IEnumerable<PublishedProviderFundingSummary> finalSummaries)
        {
            List<ChannelFunding> result = new List<ChannelFunding>();

            IEnumerable<IGrouping<string, PublishedProviderFundingSummary>> channelGroupings = finalSummaries.GroupBy(_ => _.ChannelCode);

            foreach (IGrouping<string, PublishedProviderFundingSummary> channelGrouping in channelGroupings)
            {
                ChannelFunding channelFunding = new ChannelFunding
                {
                    ChannelCode = channelGrouping.Key,
                    ChannelName = channelGrouping.First().ChannelName,
                    TotalProviders = channelGrouping.Count(),
                    TotalFunding = channelGrouping.Sum(_ => _.TotalFunding)
                };

                result.Add(channelFunding);
            }

            return result.OrderBy(_ => _.ChannelName);
        }

        private async Task<IEnumerable<Channel>> GetChannels(IEnumerable<string> channelCodes)
        {
            List<Channel> channels = new List<Channel>();
            foreach (string channelCode in channelCodes)
            {
                Channel channel = await _releaseManagementRepository.GetChannelByChannelCode(channelCode);
                if (channel == null)
                {
                    throw new KeyNotFoundException($"Channel {channelCode} not found");
                }
                channels.Add(channel);
            }

            return channels;
        }

        
    }
}