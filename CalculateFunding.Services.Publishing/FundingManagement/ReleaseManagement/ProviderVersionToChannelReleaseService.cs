using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ProviderVersionToChannelReleaseService : IProviderVersionToChannelReleaseService
    {
        private readonly IReleaseManagementRepository _repo;
        private readonly IReleaseToChannelSqlMappingContext _releaseToChannelSqlMappingContext;
        private readonly ILogger _logger;

        public ProviderVersionToChannelReleaseService(IReleaseManagementRepository repo,
            IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext,
            ILogger logger)
        {
            Guard.ArgumentNotNull(repo, nameof(repo));
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _repo = repo;
            _releaseToChannelSqlMappingContext = releaseToChannelSqlMappingContext;
            _logger = logger;
        }

        public async Task ReleaseProviderVersionChannel(IEnumerable<string> releasedProviders,
            int channelId, DateTime statusChangedDate)
        {
            foreach (string releasedProviderId in releasedProviders)
            {
                string key = $"{releasedProviderId}_{channelId}";

                if (_releaseToChannelSqlMappingContext.ReleasedProviderVersionChannels.ContainsKey(key))
                {
                    continue;
                }

                ReleasedProviderVersionChannel releasedProviderVersionChannel =
                    await _repo.CreateReleasedProviderVersionChannelsUsingAmbientTransaction(new ReleasedProviderVersionChannel
                    {
                        ReleasedProviderVersionId = GetReleaseProviderVersionId(releasedProviderId, channelId),
                        ChannelId = channelId,
                        StatusChangedDate = statusChangedDate,
                        AuthorId = _releaseToChannelSqlMappingContext.Author.Id,
                        AuthorName = _releaseToChannelSqlMappingContext.Author.Name
                    });

                _releaseToChannelSqlMappingContext.ReleasedProviderVersionChannels.Add(key, releasedProviderVersionChannel);
            }
        }

        private int GetReleaseProviderVersionId(string providerId, int channelId)
        {
            if (_releaseToChannelSqlMappingContext.ReleasedProviderVersions.TryGetValue(providerId, out ReleasedProviderVersion releasedProviderVersion))
            {
                return releasedProviderVersion.ReleasedProviderVersionId;
            }

            _logger.Error($"GetReleaseProviderVersionId: Provider {providerId} not found in sql context for {channelId}");
            throw new KeyNotFoundException($"GetReleaseProviderVersionId: Provider {providerId} not found in sql context for {channelId}");
        }
    }
}
