using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.Utility;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class PublishedProvidersSearchService : IPublishedProvidersSearchService
    {
        private readonly IReleaseManagementRepository _releaseManagementRepository;
        private readonly IPoliciesService _policiesService;

        public PublishedProvidersSearchService(IReleaseManagementRepository releaseManagementRepository,
            IPoliciesService policiesService)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));

            _releaseManagementRepository = releaseManagementRepository;
            _policiesService = policiesService;
        }

        public async Task<Dictionary<string, IEnumerable<ReleaseChannel>>> GetPublishedProviderReleaseChannelsLookup(
            ReleaseChannelSearch searchRequest)
        {
            Dictionary<string, IEnumerable<ReleaseChannel>> result = new Dictionary<string, IEnumerable<ReleaseChannel>>();

            IEnumerable<int> channelIds =
                    await GetVisibleChannelIdsForFundingConfiguration(searchRequest.FundingStreamId, searchRequest.FundingPeriodId);

            if (channelIds.Any())
            {
                IEnumerable<ProviderVersionInChannel> versions = await _releaseManagementRepository.GetLatestPublishedProviderVersionsByChannelId(
                            searchRequest.SpecificationId, channelIds);

                IEnumerable<IGrouping<string, ProviderVersionInChannel>> versionsGroupedByProviderId = versions.GroupBy(_ => _.ProviderId);
                foreach (IGrouping<string, ProviderVersionInChannel> provider in versionsGroupedByProviderId)
                {
                    if (result.ContainsKey(provider.Key))
                    {
                        IEnumerable<ReleaseChannel> newReleaseChannels = provider.ToList().Select(_ => new ReleaseChannel
                        {
                            ChannelCode = _.ChannelCode,
                            ChannelName = _.ChannelName,
                            MajorVersion = _.MajorVersion,
                            MinorVersion = _.MinorVersion
                        });

                        List<ReleaseChannel> existingReleaseChannels = result[provider.Key].ToList();
                        existingReleaseChannels.AddRange(newReleaseChannels);
                        result[provider.Key] = Enumerable.DistinctBy(existingReleaseChannels, _ => new { _.ChannelName, _.ChannelCode, _.MajorVersion, _.MinorVersion });
                    }
                    else
                    {
                        result[provider.Key] = provider.ToList().Select(_ => new ReleaseChannel
                        {
                            ChannelCode = _.ChannelCode,
                            ChannelName = _.ChannelName,
                            MajorVersion = _.MajorVersion,
                            MinorVersion = _.MinorVersion
                        });
                    }
                }
            }

            return result;
        }

        private async Task<IEnumerable<int>> GetVisibleChannelIdsForFundingConfiguration(string fundingStreamId, string fundingPeriodId)
        {
            List<int> channelIds = new List<int>();
            FundingConfiguration fundingConfiguration = await _policiesService.GetFundingConfiguration(fundingStreamId, fundingPeriodId);
            IEnumerable<string> visibleChannelCodes = fundingConfiguration.ReleaseChannels?.Where(_ => _.IsVisible).Select(_ => _.ChannelCode);
            if (visibleChannelCodes.AnyWithNullCheck())
            {
                foreach (string channelCode in visibleChannelCodes)
                {
                    int? channelId = (await _releaseManagementRepository.GetChannelByChannelCode(channelCode))?.ChannelId;
                    if (channelId == null)
                    {
                        throw new KeyNotFoundException(
                            $"PublishedProvidersSearchService:GetVisibleChannelIds ChannelCode {channelCode} could not be found." +
                            $"FundingStreamId: {fundingStreamId} and FundingPeriodId: {fundingPeriodId}");
                    }
                    channelIds.Add(channelId.Value);
                }
            }
            return channelIds;
        }
    }

    internal class GroupedReleaseChannel
    {
        public string SpecificationId { get; set; }
        public string FundingStreamId { get; set; }
        public string FundingPeriodId { get; set; }
        public List<ReleaseChannelSearch> Results { get; internal set; }
    }
}
