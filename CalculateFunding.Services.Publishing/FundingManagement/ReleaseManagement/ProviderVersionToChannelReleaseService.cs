using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ProviderVersionToChannelReleaseService : IProviderVersionToChannelReleaseService
    {
        private readonly IReleaseManagementRepository _repo;
        private readonly IReleaseToChannelSqlMappingContext _releaseToChannelSqlMappingContext;
        private readonly IUniqueIdentifierProvider _providerVersionChannelIdentifierGenerator;
        private readonly ILogger _logger;

        public ProviderVersionToChannelReleaseService(IReleaseManagementRepository repo,
            IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext,
            IUniqueIdentifierProvider providerVersionChannelIdentifierGenerator,
            ILogger logger)
        {
            Guard.ArgumentNotNull(repo, nameof(repo));
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));
            Guard.ArgumentNotNull(providerVersionChannelIdentifierGenerator, nameof(providerVersionChannelIdentifierGenerator));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _repo = repo;
            _releaseToChannelSqlMappingContext = releaseToChannelSqlMappingContext;
            _providerVersionChannelIdentifierGenerator = providerVersionChannelIdentifierGenerator;
            _logger = logger;
        }

        public async Task ReleaseProviderVersionChannel(IEnumerable<string> releasedProviders,
            int channelId, DateTime statusChangedDate)
        {
            List<ReleasedProviderVersionChannel> releasedProviderVersionChannelsToCreate = new List<ReleasedProviderVersionChannel>();
            
            foreach (string releasedProviderId in releasedProviders)
            {
                Guid releasedProviderVersionId = GetReleaseProviderVersionId(releasedProviderId, channelId);
                ReleasedProviderVersionChannel releasedProviderVersionChannel = new ReleasedProviderVersionChannel
                {
                    ReleasedProviderVersionChannelId = _providerVersionChannelIdentifierGenerator.GenerateIdentifier(),
                    ReleasedProviderVersionId = releasedProviderVersionId,
                    ChannelId = channelId,
                    StatusChangedDate = statusChangedDate,
                    AuthorId = _releaseToChannelSqlMappingContext.Author.Id,
                    AuthorName = _releaseToChannelSqlMappingContext.Author.Name,
                    ChannelVersion = await GetReleaseProviderChannelVersion(_releaseToChannelSqlMappingContext.Specification.SpecificationId, releasedProviderId,channelId),
                };

                releasedProviderVersionChannelsToCreate.Add(releasedProviderVersionChannel);

                // Ensure the version is overwritten for the key, as it is loaded in by the filter service for previous major versions
                string key = $"{releasedProviderId}_{channelId}";
                _releaseToChannelSqlMappingContext.ReleasedProviderVersionChannels[key] = releasedProviderVersionChannel.ReleasedProviderVersionChannelId;
            }

            if (releasedProviderVersionChannelsToCreate.Any())
            {
                //Below Logs only for testing purpose.We need to remove after testing
                _logger.Information("Inserting bulkdate to ReleasedProviderVersionChannel table");
                await _repo.BulkCreateReleasedProviderVersionChannelsUsingAmbientTransaction(
                    releasedProviderVersionChannelsToCreate);              
            }
        }

        private Guid GetReleaseProviderVersionId(string providerId, int channelId)
        {
            if (_releaseToChannelSqlMappingContext.ReleasedProviderVersions.TryGetValue(providerId, out ReleasedProviderVersion releasedProviderVersion))
            {
                return releasedProviderVersion.ReleasedProviderVersionId;
            }

            _logger.Error($"GetReleaseProviderVersionId: Provider {providerId} not found in sql context for {channelId}");
            throw new KeyNotFoundException($"GetReleaseProviderVersionId: Provider {providerId} not found in sql context for {channelId}");
        }

        private async Task<int> GetReleaseProviderChannelVersion(string SpecificationId, string releasedProviderId, int channelId)
        {
            var channelVersionResult = await _repo.GetLatestReleasedProviderVersionsId(SpecificationId, releasedProviderId, channelId);
            var channelVersion = (channelVersionResult != null && channelVersionResult.Count() > 0)
                ? channelVersionResult.FirstOrDefault().ChannelVersion : 0;
            return channelVersion + 1;
        }
    }
}
