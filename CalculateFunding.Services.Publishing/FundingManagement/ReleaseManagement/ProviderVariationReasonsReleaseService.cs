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
    public class ProviderVariationReasonsReleaseService : IProviderVariationReasonsReleaseService
    {
        private readonly ILogger _logger;
        private readonly IReleaseToChannelSqlMappingContext _releaseToChannelSqlMappingContext;
        private readonly IReleaseManagementRepository _releaseManagementRepository;
        private readonly IUniqueIdentifierProvider _providerVariationReasonsIdentifierGenerator;
        private IEnumerable<VariationReason> _variationReasons;

        public ProviderVariationReasonsReleaseService(
            IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext,
                IReleaseManagementRepository releaseManagementRepository,
                IUniqueIdentifierProvider providerVariationReasonsIdentifierGenerator,
                ILogger logger)
        {
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(providerVariationReasonsIdentifierGenerator, nameof(providerVariationReasonsIdentifierGenerator));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _releaseToChannelSqlMappingContext = releaseToChannelSqlMappingContext;
            _releaseManagementRepository = releaseManagementRepository;
            _providerVariationReasonsIdentifierGenerator = providerVariationReasonsIdentifierGenerator;
            _logger = logger;
        }

        public async Task PopulateReleasedProviderChannelVariationReasons(
            IDictionary<string, IEnumerable<CalculateFunding.Models.Publishing.VariationReason>> variationReasonsForProviders,
            Channel channel)
        {
            _variationReasons = await _releaseManagementRepository.GetVariationReasons();

            List<ReleasedProviderChannelVariationReason> variationReasonsToBeCreated = new List<ReleasedProviderChannelVariationReason>();

            foreach (KeyValuePair<string, IEnumerable<CalculateFunding.Models.Publishing.VariationReason>> variationReasonForProvider in variationReasonsForProviders)
            {
                Guid id = GetReleasedProviderVersionChannelId(variationReasonForProvider.Key, channel.ChannelId);
                List<ReleasedProviderChannelVariationReason> variationReasonToBeCreated =
                    variationReasonForProvider.Value.Select(s => new ReleasedProviderChannelVariationReason
                    {
                        ReleasedProviderChannelVariationReasonId = _providerVariationReasonsIdentifierGenerator.GenerateIdentifier(),
                        ReleasedProviderVersionChannelId = id,
                        VariationReasonId = GetVariationReasonId(s),
                    }).ToList();

                variationReasonsToBeCreated.AddRange(variationReasonToBeCreated);
            }

            if (variationReasonsToBeCreated.Any())
            {
                await _releaseManagementRepository
                    .BulkCreateReleasedProviderChannelVariationReasonsUsingAmbientTransaction(
                        variationReasonsToBeCreated);
            }
        }

        private Guid GetReleasedProviderVersionChannelId(string providerId, int channelId)
        {
            if (_releaseToChannelSqlMappingContext.ReleasedProviderVersionChannels.TryGetValue($"{providerId}_{channelId}", out Guid releasedProviderVersionChannelId))
            {
                return releasedProviderVersionChannelId;
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
