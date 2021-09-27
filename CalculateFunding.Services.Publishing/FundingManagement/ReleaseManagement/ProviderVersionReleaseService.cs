using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using CalculateFunding.Services.Publishing.Interfaces;
using Serilog;
using System;
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

            Dictionary<string, string> providerIdLookupFromFundingId = publishedProviderVersions.ToDictionary(_ => _.FundingId, _ => _.ProviderId);

            if (!VerifyProvidersExistInContext(newProviderVersions))
            {
                throw new KeyNotFoundException("Providers missing from sql mapping context");
            }

            IEnumerable<ReleasedProviderVersion> releasedProviderVersions =
                await _releaseManagementRepository.CreateReleasedProviderVersions(
                    newProviderVersions
                        .Select(_ => new ReleasedProviderVersion
                        {
                            MajorVersion = _.MajorVersion,
                            MinorVersion = _.MinorVersion,
                            FundingId = _.FundingId,
                            ReleasedProviderId = _releaseToChannelSqlMappingContext.ReleasedProviders[_.ProviderId].ReleasedProviderId
                        }));

            _releaseToChannelSqlMappingContext.ReleasedProviderVersions
                .AddOrUpdateRange(releasedProviderVersions.ToDictionary(_ => providerIdLookupFromFundingId[_.FundingId]));
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
