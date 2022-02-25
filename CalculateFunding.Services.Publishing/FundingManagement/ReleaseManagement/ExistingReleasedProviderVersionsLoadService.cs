using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ExistingReleasedProviderVersionsLoadService : IExistingReleasedProviderVersionsLoadService
    {
        private readonly IReleaseManagementRepository _repo;
        private readonly IReleaseToChannelSqlMappingContext _ctx;

        public ExistingReleasedProviderVersionsLoadService(IReleaseManagementRepository releaseManagementRepository,
            IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));

            _repo = releaseManagementRepository;
            _ctx = releaseToChannelSqlMappingContext;
        }
        public async Task LoadExistingReleasedProviderVersions(string specificationId, IEnumerable<string> providerIds)
        {
            IEnumerable<LatestReleasedProviderVersion> existingReleasedProviderVersions;

            bool isLargeBatchOfProviders = providerIds.Count() > 100;
            bool isAllProvidersReleaseMode = !providerIds.Any();

            if (isLargeBatchOfProviders || isAllProvidersReleaseMode)
            {
                existingReleasedProviderVersions = await _repo.GetLatestReleasedProviderVersionsUsingAmbientTransaction(specificationId);
            }
            else
            {
                // Load selected provider versions only when batch size is smaller, this saves sending thousands of provider IDs for SQL to filter
                existingReleasedProviderVersions = await _repo.GetLatestReleasedProviderVersionsUsingAmbientTransaction(specificationId, providerIds);
            }

            LoadIntoContextToMakeAvailableInFutureServices(existingReleasedProviderVersions);
        }

        private void LoadIntoContextToMakeAvailableInFutureServices(IEnumerable<LatestReleasedProviderVersion> existingReleasedProviders)
        {
            foreach (LatestReleasedProviderVersion existingVersion in existingReleasedProviders)
            {
                // Only the ID and major version are required for now, if more information is required in the future, more columns will need to be returned
                _ctx.ReleasedProviderVersions.TryAdd(existingVersion.ProviderId, new SqlModels.ReleasedProviderVersion()
                {
                    MajorVersion = existingVersion.LatestMajorVersion,
                    ReleasedProviderVersionId = existingVersion.ReleasedProviderVersionId,
                });
            }
        }
    }
}
