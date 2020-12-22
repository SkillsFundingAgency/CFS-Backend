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
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.FundingPolicy;
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

        public Task<ServiceHealth> IsHealthOk()
        {
            var (Ok, Message) = _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PolicyRepository)
            };

            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = Message });

            return Task.FromResult(health);
        }

        public async Task<FundingStream> GetFundingStreamById(string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            return (await _cosmosRepository.ReadDocumentByIdPartitionedAsync<FundingStream>(fundingStreamId, fundingStreamId))?.Content;
        }

        public async Task<IEnumerable<FundingStream>> GetFundingStreams(Expression<Func<DocumentEntity<FundingStream>, bool>> query = null)
        {
            IEnumerable<FundingStream> fundingStreams = query == null
               ? await _cosmosRepository.Query<FundingStream>()
               : (await _cosmosRepository.Query<FundingStream>(query));

            return fundingStreams;
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

            return (await _cosmosRepository.TryReadDocumentByIdPartitionedAsync<FundingConfiguration>(configId, configId))?.Content;
        }

        public async Task<IEnumerable<FundingConfiguration>> GetFundingConfigurationsByFundingStreamId(string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            return (await _cosmosRepository.Query<FundingConfiguration>(m => m.Content.FundingStreamId == fundingStreamId));
        }

        public async Task<IEnumerable<FundingConfiguration>> GetFundingConfigurations()
        {
            return await _cosmosRepository.Query<FundingConfiguration>();
        }

        public async Task<FundingPeriod> GetFundingPeriodById(string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            return (await _cosmosRepository.ReadDocumentByIdPartitionedAsync<FundingPeriod>(fundingPeriodId, fundingPeriodId))?.Content;
        }

        public async Task<IEnumerable<FundingPeriod>> GetFundingPeriods(Expression<Func<DocumentEntity<FundingPeriod>, bool>> query = null)
        {
            IEnumerable<FundingPeriod> fundingPeriods = query == null
               ? await _cosmosRepository.Query<FundingPeriod>()
               : (await _cosmosRepository.Query<FundingPeriod>(query));

            return fundingPeriods;
        }

        public async Task SaveFundingPeriods(IEnumerable<FundingPeriod> fundingPeriods)
        {
            Guard.ArgumentNotNull(fundingPeriods, nameof(fundingPeriods));

            await _cosmosRepository.BulkUpsertAsync<FundingPeriod>(fundingPeriods.ToList());
        }

        public async Task<FundingDate> GetFundingDate(
            string fundingDateId)
        {
            Guard.IsNullOrWhiteSpace(fundingDateId, nameof(fundingDateId));

            return (await _cosmosRepository.TryReadDocumentByIdPartitionedAsync<FundingDate>(fundingDateId, fundingDateId))?.Content;
        }

        public async Task<HttpStatusCode> SaveFundingDate(FundingDate fundingDate)
        {
            Guard.ArgumentNotNull(fundingDate, nameof(fundingDate));

            return await _cosmosRepository.UpsertAsync(fundingDate, fundingDate.Id, true);
        }
    }
}
