using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.FundingManagement.ReleaseManagement
{
    public class ReleaseProviderPersistenceService : IReleaseProviderPersistenceService
    {
        private readonly IReleaseToChannelSqlMappingContext _releaseToChannelSqlMappingContext;
        private readonly IReleaseManagementRepository _releaseManagementRepository;
        private readonly IUniqueIdentifierProvider _releasedProviderIdentifierGenerator;

        public ReleaseProviderPersistenceService(IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext,
                IReleaseManagementRepository releaseManagementRepository,
                IUniqueIdentifierProvider releasedProviderIdentifierGenerator)
        {
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(releasedProviderIdentifierGenerator, nameof(releasedProviderIdentifierGenerator));

            _releaseToChannelSqlMappingContext = releaseToChannelSqlMappingContext;
            _releaseManagementRepository = releaseManagementRepository;
            _releasedProviderIdentifierGenerator = releasedProviderIdentifierGenerator;
        }

        public async Task<IEnumerable<ReleasedProvider>> ReleaseProviders(IEnumerable<string> providers, string specificationId)
        {
            IEnumerable<ReleasedProvider> releasedProviders = providers.Where(_ => !_releaseToChannelSqlMappingContext.ReleasedProviders.ContainsKey(_))
                .Select(_ => new ReleasedProvider
                {
                    ReleasedProviderId = _releasedProviderIdentifierGenerator.GenerateIdentifier(),
                    ProviderId = _,
                    SpecificationId = specificationId,
                }).ToList();

            if (releasedProviders.Any())
            {
                await _releaseManagementRepository
                    .BulkCreateReleasedProvidersUsingAmbientTransaction(releasedProviders);

                _releaseToChannelSqlMappingContext.ReleasedProviders.AddOrUpdateRange(
                    releasedProviders.ToDictionary(_ => _.ProviderId));
            }

            return releasedProviders;
        }
    }
}
