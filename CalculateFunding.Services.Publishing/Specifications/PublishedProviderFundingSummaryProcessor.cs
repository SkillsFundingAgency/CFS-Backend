using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces.Threading;
using CalculateFunding.Services.Core.Threading;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class PublishedProviderFundingSummaryProcessor : IPublishedProviderFundingSummaryProcessor
    {
        private readonly IProducerConsumerFactory _producerConsumerFactory;
        private readonly AsyncPolicy _publishedFundingPolicy;
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly IReleaseManagementRepository _releaseManagementRepository;
        private readonly IProvidersForChannelFilterService _providersForChannelFilterService;
        private readonly IChannelOrganisationGroupGeneratorService _channelOrganisationGroupGeneratorService;
        private readonly ILogger _logger;

        public PublishedProviderFundingSummaryProcessor(IProducerConsumerFactory producerConsumerFactory,
            IPublishedFundingRepository publishedFunding,
            IPublishingResiliencePolicies resiliencePolicies,
            IReleaseManagementRepository releaseManagementRepository,
            IProvidersForChannelFilterService providersForChannelFilterService,
            IChannelOrganisationGroupGeneratorService channelOrganisationGroupGeneratorService,
            ILogger logger)
        {
            Guard.ArgumentNotNull(producerConsumerFactory, nameof(producerConsumerFactory));
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(resiliencePolicies.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(providersForChannelFilterService, nameof(providersForChannelFilterService));
            Guard.ArgumentNotNull(channelOrganisationGroupGeneratorService, nameof(channelOrganisationGroupGeneratorService));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _producerConsumerFactory = producerConsumerFactory;
            _publishedFunding = publishedFunding;
            _logger = logger;
            _publishedFundingPolicy = resiliencePolicies.PublishedFundingRepository;
            _releaseManagementRepository = releaseManagementRepository;
            _providersForChannelFilterService = providersForChannelFilterService;
            _channelOrganisationGroupGeneratorService = channelOrganisationGroupGeneratorService;
        }

        public async Task<ReleaseFundingPublishedProvidersSummary> GetFundingSummaryForApprovedPublishedProvidersByChannel(IEnumerable<string> publishedProviderIds,
            SpecificationSummary specificationSummary,
            FundingConfiguration fundingConfiguration,
            IEnumerable<string> channelCodes)
        {
            Guard.ArgumentNotNull(specificationSummary, nameof(specificationSummary));
            Guard.ArgumentNotNull(fundingConfiguration, nameof(fundingConfiguration));
            Guard.IsNotEmpty(channelCodes, nameof(channelCodes));

            IEnumerable<Channel> channels = await GetChannels(channelCodes);

            if (publishedProviderIds.IsNullOrEmpty())
            {
                publishedProviderIds = await _publishedFundingPolicy.ExecuteAsync(() => _publishedFunding.GetPublishedProviderPublishedProviderIds(specificationSummary.Id));
            }

            IEnumerable<PublishedProviderFundingSummary> approvedOrReleasedPublishedProviders = await GetPublishedProviderFundingSummaries(
                publishedProviderIds,
                specificationSummary,
                new[] { PublishedProviderStatus.Approved, PublishedProviderStatus.Released }
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

        private async Task<IEnumerable<PublishedProviderFundingSummary>> GetPublishedProviderFundingSummaries(
            IEnumerable<string> publishedProviderIds, SpecificationSummary specificationSummary, PublishedProviderStatus[] statuses)
        {
            PublishedProviderFundingSummaryProcessorContext context = new PublishedProviderFundingSummaryProcessorContext(
                publishedProviderIds,
                statuses,
                specificationSummary.Id);

            IProducerConsumer producerConsumer = _producerConsumerFactory.CreateProducerConsumer(ProduceFundingSummaryPublishedProviderIds,
                GetReleaseFundingSummaryForPublishedProviderIds,
                20,
                5,
                _logger);

            await producerConsumer.Run(context);

            return context.PublishedProviderFundingSummaries;
        }

        private Task<(bool isComplete, IEnumerable<string> items)> ProduceFundingSummaryPublishedProviderIds(CancellationToken token,
            dynamic context)
        {
            PublishedProviderFundingSummaryProcessorContext countContext = (PublishedProviderFundingSummaryProcessorContext)context;

            while (countContext.HasPages)
            {
                return Task.FromResult((false, countContext.NextPage().AsEnumerable()));
            }

            return Task.FromResult((true, ArraySegment<string>.Empty.AsEnumerable()));
        }

        private async Task GetReleaseFundingSummaryForPublishedProviderIds(CancellationToken cancellationToken,
            dynamic context,
            IEnumerable<string> items)
        {
            PublishedProviderFundingSummaryProcessorContext countContext = (PublishedProviderFundingSummaryProcessorContext)context;

            IEnumerable<PublishedProviderFundingSummary> fundings = await _publishedFundingPolicy.ExecuteAsync(() => _publishedFunding.GetReleaseFundingPublishedProviders(items,
                countContext.SpecificationId,
                countContext.Statuses));

            countContext.AddFundings(fundings);
        }

        private class PublishedProviderFundingSummaryProcessorContext : PagedContext<string>
        {
            private readonly ConcurrentBag<PublishedProviderFundingSummary> _fundings = new ConcurrentBag<PublishedProviderFundingSummary>();

            public PublishedProviderFundingSummaryProcessorContext(IEnumerable<string> items,
                PublishedProviderStatus[] statuses,
                string specificationId)
                : base(items, 100)
            {
                Statuses = statuses;
                SpecificationId = specificationId;
            }

            public string SpecificationId { get; }

            public PublishedProviderStatus[] Statuses { get; }

            public void AddFundings(IEnumerable<PublishedProviderFundingSummary> fundings)
            {
                foreach (PublishedProviderFundingSummary funding in fundings)
                {
                    _fundings.Add(funding);
                }
            }

            public IEnumerable<PublishedProviderFundingSummary> PublishedProviderFundingSummaries => _fundings.AsEnumerable();
        }
    }
}