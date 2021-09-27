using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ReleaseProviderPersistanceService : IReleaseProviderPersistanceService
    {
        private readonly IReleaseToChannelSqlMappingContext _releaseToChannelSqlMappingContext;
        private readonly IReleaseManagementRepository _releaseManagementRepository;

        public ReleaseProviderPersistanceService(IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext,
                IReleaseManagementRepository releaseManagementRepository)
        {
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));

            _releaseToChannelSqlMappingContext = releaseToChannelSqlMappingContext;
            _releaseManagementRepository = releaseManagementRepository;
        }

        public async Task ReleaseProviders(IEnumerable<string> providers, string specificationId)
        {
            IEnumerable<ReleasedProvider> releasedProviders =
                await _releaseManagementRepository.CreateReleasedProviders(
                    providers.Where(_ => !_releaseToChannelSqlMappingContext.ReleasedProviders.ContainsKey(_))
                        .Select(_ => new ReleasedProvider
                        {
                            ProviderId = _,
                            SpecificationId = specificationId
                        }));

            _releaseToChannelSqlMappingContext.ReleasedProviders.AddOrUpdateRange(releasedProviders.ToDictionary(_ => _.ProviderId));
        }
    }
}
