using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ExistingReleasedProvidersLoadService : IExistingReleasedProvidersLoadService
    {
        private readonly IReleaseManagementRepository _repo;
        private readonly IReleaseToChannelSqlMappingContext _ctx;

        public ExistingReleasedProvidersLoadService(IReleaseManagementRepository releaseManagementRepository,
            IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext)
        {
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));

            _repo = releaseManagementRepository;
            _ctx = releaseToChannelSqlMappingContext;
        }
        public async Task LoadExistingReleasedProviders(string specificationId, IEnumerable<string> providerIds)
        {
            IEnumerable<ReleasedProvider> existingReleasedProviders;

            bool isLargeBatchOfProviders = providerIds.Count() > 100;
            bool isAllProvidersReleaseMode = !providerIds.Any();

            if (isLargeBatchOfProviders || isAllProvidersReleaseMode)
            {
                existingReleasedProviders = await _repo.GetReleasedProvidersUsingAmbientTransaction(specificationId);
            }
            else
            {
                // Load selected providers only when batch size is smaller, this saves sending thousands of provider IDs for SQL to filter
                existingReleasedProviders = await _repo.GetReleasedProvidersUsingAmbientTransaction(specificationId, providerIds);
            }

            LoadIntoContextToMakeAvailableInFutureServices(existingReleasedProviders);
        }

        private void LoadIntoContextToMakeAvailableInFutureServices(IEnumerable<ReleasedProvider> existingReleasedProviders)
        {
            foreach (ReleasedProvider provider in existingReleasedProviders)
            {
                _ctx.ReleasedProviders.TryAdd(provider.ProviderId, provider);
            }
        }
    }
}
