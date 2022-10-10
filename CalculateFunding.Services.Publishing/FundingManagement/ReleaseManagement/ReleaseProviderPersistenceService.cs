using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels;
using Serilog;
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
        private readonly ILogger _logger;

        public ReleaseProviderPersistenceService(IReleaseToChannelSqlMappingContext releaseToChannelSqlMappingContext,
                IReleaseManagementRepository releaseManagementRepository,
                IUniqueIdentifierProvider releasedProviderIdentifierGenerator,
                ILogger logger)
        {
            Guard.ArgumentNotNull(releaseToChannelSqlMappingContext, nameof(releaseToChannelSqlMappingContext));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(releasedProviderIdentifierGenerator, nameof(releasedProviderIdentifierGenerator));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _releaseToChannelSqlMappingContext = releaseToChannelSqlMappingContext;
            _releaseManagementRepository = releaseManagementRepository;
            _releasedProviderIdentifierGenerator = releasedProviderIdentifierGenerator;
            _logger = logger;
        }

        public async Task<IEnumerable<ReleasedProvider>> ReleaseProviders(IEnumerable<string> providers, string specificationId)
        {
            //Below Logs only for testing purpose.We need to remove after testing
            _logger.Information("Checking ReleaseProviders list to Context '{Providers}'", providers);
            IEnumerable<ReleasedProvider> releasedProviders = providers.Where(_ => !_releaseToChannelSqlMappingContext.ReleasedProviders.ContainsKey(_))
                .Select(_ => new ReleasedProvider
                {
                    ReleasedProviderId = _releasedProviderIdentifierGenerator.GenerateIdentifier(),
                    ProviderId = _,
                    SpecificationId = specificationId,
                }).ToList();

            if (releasedProviders.Any())
            {
                //Below Logs only for testing purpose .We need to remove after testing
                var releaseProviders = releasedProviders.Select(x => x.ProviderId);
                _logger.Information("Inserting ReleaseProviders list to database '{Providers}'", releaseProviders);

                await _releaseManagementRepository
                    .BulkCreateReleasedProvidersUsingAmbientTransaction(releasedProviders);

                _releaseToChannelSqlMappingContext.ReleasedProviders.AddOrUpdateRange(
                    releasedProviders.ToDictionary(_ => _.ProviderId));
            }

            return releasedProviders;
        }
    }
}
