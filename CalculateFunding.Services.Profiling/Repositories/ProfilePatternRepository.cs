using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Profiling.Models;
using static CalculateFunding.Common.Extensions.TypeExtensions;

namespace CalculateFunding.Services.Profiling.Repositories
{
    public class ProfilePatternRepository : IProfilePatternRepository, IHealthChecker
    {
        private readonly ICosmosRepository _cosmosRepository;

        public ProfilePatternRepository(ICosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));
            
            _cosmosRepository = cosmosRepository;
        }
        
        public async Task<FundingStreamPeriodProfilePattern> GetProfilePattern(string fundingPeriodId, 
            string fundingStreamId, 
            string fundingLineCode,
            string profilePatternKey)
        {
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingLineCode, nameof(fundingLineCode));

            string profilePatternKeyIdComponent = profilePatternKey.IsNullOrEmpty() ? null : $"-{profilePatternKey}";

            return await GetProfilePattern($"{fundingPeriodId}-{fundingStreamId}-{fundingLineCode}{profilePatternKeyIdComponent}");
        }

        public async Task<FundingStreamPeriodProfilePattern> GetProfilePattern(string id)
        {
            Guard.IsNullOrWhiteSpace(id, nameof(id));
            
            DocumentEntity<FundingStreamPeriodProfilePattern> result = await _cosmosRepository.ReadDocumentByIdAsync<FundingStreamPeriodProfilePattern>(id);

            return result?.Content;    
        }

        public async Task<IEnumerable<FundingStreamPeriodProfilePattern>> GetProfilePatternsForFundingStreamAndFundingPeriod(string fundingStreamId,
            string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            return await _cosmosRepository.QuerySql<FundingStreamPeriodProfilePattern>(new CosmosDbQuery
            {
                QueryText = @"SELECT 
                                *
                              FROM profilePeriodPattern p
                              WHERE p.documentType = 'FundingStreamPeriodProfilePattern'
                              AND p.deleted = false
                              AND p.content.fundingStreamId = @fundingStreamId
                              AND p.content.fundingPeriodId = @fundingPeriodId",
                Parameters = new []
                {
                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId), 
                    new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId), 
                }
            });
        }

        public async Task<FundingStreamPeriodProfilePattern> GetProfilePattern(string fundingPeriodId,
            string fundingStreamId,
            string fundingLineCode,
            string providerType,
            string providerSubType)
        {
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingLineCode, nameof(fundingLineCode));
            Guard.IsNullOrWhiteSpace(providerType, nameof(providerType));
            Guard.IsNullOrWhiteSpace(providerSubType, nameof(providerSubType));

            string queryText = @"SELECT 
                                *
                              FROM profilePeriodPattern p
                              WHERE p.documentType = 'FundingStreamPeriodProfilePattern'
                              AND p.deleted = false
                              AND p.content.fundingStreamId = @fundingStreamId
                              AND p.content.fundingPeriodId = @fundingPeriodId
                              AND p.content.fundingLineId = @fundingLineCode";

            List<CosmosDbQueryParameter> parameters = new List<CosmosDbQueryParameter>()
            {
                new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId),
                new CosmosDbQueryParameter("@fundingLineCode", fundingLineCode),
            };

            IEnumerable<FundingStreamPeriodProfilePattern> patterns = await _cosmosRepository.QuerySql<FundingStreamPeriodProfilePattern>(new CosmosDbQuery
            {
                QueryText = queryText,
                Parameters = parameters
            });

            if(patterns?.Any() == false)
            {
                return null;
            }

            return patterns.FirstOrDefault(x => (!x.ProviderTypeSubTypes.IsNullOrEmpty() && x.ProviderTypeSubTypes.Any(p => string.Equals(p.ProviderType, providerType, StringComparison.InvariantCultureIgnoreCase) && string.Equals(p.ProviderSubType, providerSubType, StringComparison.InvariantCultureIgnoreCase))));
        }

        public async Task<HttpStatusCode> DeleteProfilePattern(string id)
        {
            Guard.IsNullOrWhiteSpace(id, nameof(id));
            

            return await _cosmosRepository.DeleteAsync<FundingStreamPeriodProfilePattern>(id, null);
        }

        public async Task<HttpStatusCode> SaveFundingStreamPeriodProfilePattern(FundingStreamPeriodProfilePattern fundingStreamPeriodProfilePattern)
        {
            Guard.ArgumentNotNull(fundingStreamPeriodProfilePattern, nameof(fundingStreamPeriodProfilePattern));
            
            return await _cosmosRepository.UpsertAsync(fundingStreamPeriodProfilePattern);
        }

        public Task<ServiceHealth> IsHealthOk()
        {
            (bool ok, string message) = _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProfilePatternRepository)
            };

            health.Dependencies.Add(new DependencyHealth
            {
                HealthOk = ok, 
                DependencyName = _cosmosRepository.GetType().GetFriendlyName(), 
                Message = message
            });

            return Task.FromResult(health);
        }
    }
}