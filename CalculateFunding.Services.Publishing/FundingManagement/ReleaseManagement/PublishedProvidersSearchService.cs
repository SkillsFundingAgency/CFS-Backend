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

        public PublishedProvidersSearchService(IReleaseManagementRepository releaseManagementRepository, IPoliciesService policiesService)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));

            _releaseManagementRepository = releaseManagementRepository;
            _policiesService = policiesService;
        }

        public async Task<Dictionary<string, IEnumerable<ReleaseChannel>>> GetPublishedProviderReleaseChannelsLookup(
            IEnumerable<ReleaseChannelSearch> searchRequest)
        {
            Dictionary<string, IEnumerable<ReleaseChannel>> result = new Dictionary<string, IEnumerable<ReleaseChannel>>();

            IEnumerable<GroupedReleaseChannel> groupedReleaseChannels = searchRequest.GroupBy(_ => new
            {
                _.SpecificationId,
                _.FundingStreamId,
                _.FundingPeriodId
            }).Select(_ => new GroupedReleaseChannel
            {
                SpecificationId = _.Key.SpecificationId,
                FundingStreamId = _.Key.FundingStreamId,
                FundingPeriodId = _.Key.FundingPeriodId,
                Results = _.ToList()
            });

            foreach (GroupedReleaseChannel grc in groupedReleaseChannels)
            {
                IEnumerable<int> channelIds = await GetVisibleChannelIdsForFundingConfiguration(grc.FundingStreamId, grc.FundingPeriodId);
                if (channelIds.Any())
                {
                    IEnumerable<ProviderVersionInChannel> versions = await _releaseManagementRepository.GetLatestPublishedProviderVersions(grc.SpecificationId, channelIds);
                    IEnumerable<IGrouping<string, ProviderVersionInChannel>> versionsGroupedByProviderId = versions.GroupBy(_ => _.ProviderId);
                    foreach (IGrouping<string, ProviderVersionInChannel> item in versionsGroupedByProviderId)
                    {
                        if (result.ContainsKey(item.Key))
                        {
                            IEnumerable<ReleaseChannel> newReleaseChannels = item.ToList().Select(_ => new ReleaseChannel
                            {
                                ChannelCode = _.ChannelCode,
                                ChannelName = _.ChannelName,
                                MajorVersion = _.MajorVersion,
                                MinorVersion = _.MinorVersion
                            });

                            List<ReleaseChannel> existingReleaseChannels = result[item.Key].ToList();
                            existingReleaseChannels.AddRange(newReleaseChannels);
                            result[item.Key] = existingReleaseChannels.DistinctBy(_ => new { _.ChannelName, _.ChannelCode, _.MajorVersion, _.MinorVersion });
                        }
                        else
                        {
                            result[item.Key] = item.ToList().Select(_ => new ReleaseChannel
                            {
                                ChannelCode = _.ChannelCode,
                                ChannelName = _.ChannelName,
                                MajorVersion = _.MajorVersion,
                                MinorVersion = _.MinorVersion
                            });
                        }
                    }
                }
            }

            return result;
        }

        private async Task<IEnumerable<int>> GetVisibleChannelIdsForFundingConfiguration(string fundingStreamId, string fundingPeriodId)
        {
            List<int> channelIds = new List<int>();
            FundingConfiguration fundingConfiguration = await _policiesService.GetFundingConfiguration(fundingStreamId, fundingPeriodId);
            IEnumerable<string> visibleChannelCodes = fundingConfiguration.ReleaseChannels.Where(_ => _.IsVisible).Select(_ => _.ChannelCode);
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
