using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ProviderVariationReasonsReleaseService : IProviderVariationReasonsReleaseService
    {
        private readonly ILogger _logger;
        private readonly IReleaseToChannelSqlMappingContext _releaseToChannelSqlMappingContext;
        private readonly IReleaseManagementRepository _releaseManagementRepository;
        private IEnumerable<VariationReason> _variationReasons;

        public ProviderVariationReasonsReleaseService(IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext,
                IReleaseManagementRepository releaseManagementRepository, ILogger logger)
        {
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _releaseToChannelSqlMappingContext = releaseToChannelSqlMappingContext;
            _releaseManagementRepository = releaseManagementRepository;
            _logger = logger;
        }

        public async Task PopulateReleasedProviderChannelVariationReasons(
            IDictionary<string, IEnumerable<CalculateFunding.Models.Publishing.VariationReason>> variationReasonsForProviders,
            Channel channel)
        {
            _variationReasons = await _releaseManagementRepository.GetVariationReasons();

            foreach (KeyValuePair<string, IEnumerable<CalculateFunding.Models.Publishing.VariationReason>> variationReasonForProvider in variationReasonsForProviders)
            {
                int id = GetReleasedProviderVersionChannelId(variationReasonForProvider.Key, channel.ChannelId);
                List<ReleasedProviderChannelVariationReason> variationReasonsToBeCreated =
                    variationReasonForProvider.Value.Select(s => new ReleasedProviderChannelVariationReason
                    {
                        ReleasedProviderVersionChannelId = id,
                        VariationReasonId = GetVariationReasonId(s)
                    }).ToList();
                await _releaseManagementRepository.CreateReleasedProviderChannelVariationReasonsUsingAmbientTransaction(variationReasonsToBeCreated);
            }
        }

        private int GetReleasedProviderVersionChannelId(string providerId, int channelId)
        {
            if (_releaseToChannelSqlMappingContext.ReleasedProviderVersionChannels.TryGetValue($"{providerId}_{channelId}", out var value))
            {
                return value.ReleasedProviderVersionChannelId;
            }
            else
            {
                throw new KeyNotFoundException($"ReleasedProviderVersionChannel not found in sql context for providerId {providerId}");
            }
        }

        private int GetVariationReasonId(CalculateFunding.Models.Publishing.VariationReason variationReason)
        {
            if (_variationReasons == null)
            {
                throw new NullReferenceException("Variation reasons not set");
            }

            try
            {
                return _variationReasons.First(v => v.VariationReasonCode == variationReason.ToString()).VariationReasonId;
            }
            catch
            {
                _logger.Error($"{variationReason} not found in sql Variation Reasons");
                throw new KeyNotFoundException($"{variationReason} not found in sql Variation Reasons");
            }
        }
    }
}
