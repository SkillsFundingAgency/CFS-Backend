using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ExistingReleasedProvidersForChannelFilterService : IExistingReleasedProvidersForChannelFilterService
    {
        private readonly IReleaseManagementRepository _repo;
        private readonly IReleaseToChannelSqlMappingContext _context;

        public ExistingReleasedProvidersForChannelFilterService(
            IReleaseManagementRepository releaseManagementRepository,
            IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));

            _repo = releaseManagementRepository;
            _context = releaseToChannelSqlMappingContext;
        }
        public IEnumerable<PublishedProviderVersion> FilterExistingReleasedProviderInChannel(
            IEnumerable<PublishedProviderVersion> providersInGroupsToRelease,
            IEnumerable<ProviderVersionInChannel> latestProviderVersionsInChannel,
            IEnumerable<string> batchPublishedProviderIds,
            int channelId,
            string specificationId)
        {

            // Populate provider versions in channel for use when a new funding group is added, but contains providers which aren't included in this batch
            foreach (ProviderVersionInChannel pvc in latestProviderVersionsInChannel)
            {
                string key = $"{pvc.ProviderId}_{channelId}";

                _context.ReleasedProviderVersionChannels.Add(key, pvc.ReleasedProviderVersionChannelId);
            }

            Dictionary<string, ProviderVersionInChannel> providersInChannel = latestProviderVersionsInChannel.ToDictionary(_ => _.ProviderId);

            return providersInGroupsToRelease.Where(_ =>
                    !providersInChannel.ContainsKey(_.ProviderId) ||
                    providersInChannel[_.ProviderId].MajorVersion != _.MajorVersion);
        }
    }
}
