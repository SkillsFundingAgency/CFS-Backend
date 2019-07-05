using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.FundingPolicy;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Policy.Interfaces;

namespace CalculateFunding.Services.Policy
{
    public class PolicyRepository : IPolicyRepository, IHealthChecker
    {
        private readonly ICosmosRepository _cosmosRepository;

        public PolicyRepository(ICosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var cosmosRepoHealth = await _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PolicyRepository)
            };

            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
        }

        public async Task<FundingStream> GetFundingStreamById(string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            return (await _cosmosRepository.ReadAsync<FundingStream>(fundingStreamId, true))?.Content;
        }

        public async Task<IEnumerable<FundingStream>> GetFundingStreams(Expression<Func<FundingStream, bool>> query = null)
        {
            IQueryable<FundingStream> fundingStreams = query == null
               ? _cosmosRepository.Query<FundingStream>(true)
               : _cosmosRepository.Query<FundingStream>(true).Where(query);

            return await Task.FromResult(fundingStreams.AsEnumerable());
        }

        public async Task<HttpStatusCode> SaveFundingStream(FundingStream fundingStream)
        {
            Guard.ArgumentNotNull(fundingStream, nameof(fundingStream));

            return await _cosmosRepository.UpsertAsync<FundingStream>(fundingStream, fundingStream.Id, true);
        }

        public async Task<HttpStatusCode> SaveFundingConfiguration(FundingConfiguration fundingConfiguration)
        {
            Guard.ArgumentNotNull(fundingConfiguration, nameof(fundingConfiguration));

            return await _cosmosRepository.UpsertAsync<FundingConfiguration>(fundingConfiguration, fundingConfiguration.Id, true);
        }

        public async Task<FundingConfiguration> GetFundingConfiguration(string configId)
        {
            Guard.IsNullOrWhiteSpace(configId, nameof(configId));

            return (await _cosmosRepository.ReadAsync<FundingConfiguration>(configId, true))?.Content;
        }


        public async Task<Period> GetFundingPeriodById(string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            return (await _cosmosRepository.ReadAsync<Period>(fundingPeriodId, true))?.Content;
        }

        public async Task<IEnumerable<Period>> GetFundingPeriods(Expression<Func<Period, bool>> query = null)
        {
            IQueryable<Period> fundingPeriods = query == null
               ? _cosmosRepository.Query<Period>(true)
               : _cosmosRepository.Query<Period>(true).Where(query);

            return await Task.FromResult(fundingPeriods.AsEnumerable());
        }

        public async Task SaveFundingPeriods(IEnumerable<Period> fundingPeriods)
        {
            Guard.ArgumentNotNull(fundingPeriods, nameof(fundingPeriods));

            await _cosmosRepository.BulkUpsertAsync<Period>(fundingPeriods.ToList(), enableCrossPartitionQuery: true);
        }
    }
}
