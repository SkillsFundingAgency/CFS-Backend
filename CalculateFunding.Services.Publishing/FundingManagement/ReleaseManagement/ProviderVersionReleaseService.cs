using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Interfaces;
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
        private readonly IUniqueIdentifierProvider _providerVersionIdentifierGenerator;

        public ProviderVersionReleaseService(IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext,
                                             IReleaseManagementRepository releaseManagementRepository,
                                             IUniqueIdentifierProvider providerVersionIdentifierGenerator,
                                             ILogger logger)
        {
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(providerVersionIdentifierGenerator, nameof(providerVersionIdentifierGenerator));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _releaseToChannelSqlMappingContext = releaseToChannelSqlMappingContext;
            _releaseManagementRepository = releaseManagementRepository;
            _providerVersionIdentifierGenerator = providerVersionIdentifierGenerator;
            _logger = logger;
        }

        public async Task ReleaseProviderVersions(IEnumerable<PublishedProviderVersion> publishedProviderVersions, string specificationId)
        {
            Dictionary<string, ReleasedProviderVersion> existingProviders = _releaseToChannelSqlMappingContext.ReleasedProviderVersions;

            List<PublishedProviderVersion> providersToCreate = new List<PublishedProviderVersion>(publishedProviderVersions.Count());

            foreach (PublishedProviderVersion provider in publishedProviderVersions)
            {
                if (ProviderHasNeverBeenReleased(existingProviders, provider))
                {
                    providersToCreate.Add(provider);
                }
                else
                {
                    if (NewVersionOfProviderPreviouslyReleasedIsInThisBatch(existingProviders, provider))
                    {
                        providersToCreate.Add(provider);
                    }
                }
            }

            if (!VerifyProvidersExistInContext(providersToCreate))
            {
                throw new KeyNotFoundException("Providers missing from sql mapping context");
            }

            List<ReleasedProviderVersion> releasedProviderVersionsToCreate = new List<ReleasedProviderVersion>();

            foreach (PublishedProviderVersion providerVersion in providersToCreate)
            {
                ReleasedProviderVersion releasedProviderVersion = new ReleasedProviderVersion
                {
                    ReleasedProviderVersionId = _providerVersionIdentifierGenerator.GenerateIdentifier(),
                    MajorVersion = providerVersion.MajorVersion,
                    MinorVersion = providerVersion.MinorVersion,
                    FundingId = providerVersion.FundingId,
                    TotalFunding = providerVersion.TotalFunding ?? 0m,
                    ReleasedProviderId = _releaseToChannelSqlMappingContext
                        .ReleasedProviders[providerVersion.ProviderId].ReleasedProviderId,
                    CoreProviderVersionId = providerVersion.Provider.ProviderVersionId,
                };

                releasedProviderVersionsToCreate.Add(releasedProviderVersion);

                // Add or update the record in released provider versions to indicate the latest group, as the overall release will release the latest major version for this provider
                // The provider version may already be in the dictionary from the start of the release process from an older major version previously released
                _releaseToChannelSqlMappingContext.ReleasedProviderVersions[providerVersion.ProviderId] = releasedProviderVersion;
            }

            if (releasedProviderVersionsToCreate.Any())
            {
                await _releaseManagementRepository.BulkCreateReleasedProviderVersionsUsingAmbientTransaction(
                    releasedProviderVersionsToCreate);
            }

            static bool NewVersionOfProviderPreviouslyReleasedIsInThisBatch(Dictionary<string, ReleasedProviderVersion> existingProviders, PublishedProviderVersion provider)
            {
                return existingProviders[provider.ProviderId].MajorVersion != provider.MajorVersion;
            }

            static bool ProviderHasNeverBeenReleased(Dictionary<string, ReleasedProviderVersion> existingProviders, PublishedProviderVersion provider)
            {
                return !existingProviders.ContainsKey(provider.ProviderId);
            }
        }

        private bool VerifyProvidersExistInContext(IEnumerable<PublishedProviderVersion> newProviderVersions)
        {
            List<PublishedProviderVersion> missingProviders = new List<PublishedProviderVersion>();

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
