using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ProviderVersionReleaseService : IProviderVersionReleaseService
    {
        private readonly ILogger _logger;
        private readonly IReleaseToChannelSqlMappingContext _releaseToChannelSqlMappingContext;
        private readonly IReleaseManagementRepository _releaseManagementRepository;

        public ProviderVersionReleaseService(IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext,
                IReleaseManagementRepository releaseManagementRepository, ILogger logger)
        {
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _releaseToChannelSqlMappingContext = releaseToChannelSqlMappingContext;
            _releaseManagementRepository = releaseManagementRepository;
            _logger = logger;
        }

        public async Task ReleaseProviderVersions(IEnumerable<PublishedProviderVersion> publishedProviderVersions, string specificationId)
        {
            IEnumerable<PublishedProviderVersion> newProviderVersions = publishedProviderVersions
                .Where(_ => !_releaseToChannelSqlMappingContext.ReleasedProviderVersions.ContainsKey(_.ProviderId));

            if (!VerifyProvidersExistInContext(newProviderVersions))
            {
                throw new KeyNotFoundException("Providers missing from sql mapping context");
            }

            foreach (PublishedProviderVersion providerVersion in newProviderVersions)
            {
                ReleasedProviderVersion releasedProviderVersion = await _releaseManagementRepository.CreateReleasedProviderVersionsUsingAmbientTransaction(new ReleasedProviderVersion
                {
                    MajorVersion = providerVersion.MajorVersion,
                    MinorVersion = providerVersion.MinorVersion,
                    FundingId = providerVersion.FundingId,
                    TotalFunding = providerVersion.TotalFunding ?? 0m,
                    ReleasedProviderId = _releaseToChannelSqlMappingContext.ReleasedProviders[providerVersion.ProviderId].ReleasedProviderId
                });

                _releaseToChannelSqlMappingContext.ReleasedProviderVersions.Add(providerVersion.ProviderId, releasedProviderVersion);
            }
        }

        private bool VerifyProvidersExistInContext(IEnumerable<PublishedProviderVersion> newProviderVersions)
        {
            var missingProviders = new List<PublishedProviderVersion>();

            foreach (PublishedProviderVersion providerVersion in newProviderVersions)
            {
                if (!_releaseToChannelSqlMappingContext.ReleasedProviders.ContainsKey(providerVersion.ProviderId))
                {
                    _logger.Error("Provider {providerId} not found in ReleasedProviders context when attempting to release provider version", providerVersion.ProviderId);
                    missingProviders.Add(providerVersion);
                }
            }

            return !missingProviders.Any();
        }
    }
}
