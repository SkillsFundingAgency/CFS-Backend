using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Profiling
{
    public class FundingStreamPaymentDatesRepository : IFundingStreamPaymentDatesRepository, IHealthChecker
    {
        private readonly ICosmosRepository _cosmosRepository;

        public FundingStreamPaymentDatesRepository(ICosmosRepository cosmosRepository)
        {
            Guard.ArgumentNotNull(cosmosRepository, nameof(cosmosRepository));

            _cosmosRepository = cosmosRepository;
        }
        
        public Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            (bool Ok, string Message) cosmosHealth = _cosmosRepository.IsHealthOk();

            health.Name = nameof(FundingStreamPaymentDatesRepository);
            health.Dependencies.Add(new DependencyHealth
            {
                HealthOk = cosmosHealth.Ok, 
                Message = cosmosHealth.Message,
                DependencyName = nameof(FundingStreamPaymentDatesRepository) 
            });

            return Task.FromResult(health);
        }

        public async Task SaveFundingStreamUpdatedDates(FundingStreamPaymentDates paymentDates)
        {
            await _cosmosRepository.UpsertAsync(paymentDates, paymentDates.FundingStreamId);
        }

        public async Task<FundingStreamPaymentDates> GetUpdateDates(string fundingStreamId, string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            
            return (await _cosmosRepository.QuerySql<FundingStreamPaymentDates>(new CosmosDbQuery
            {
                QueryText = @"SELECT *
                              FROM c
                              WHERE c.documentType = 'FundingStreamPaymentDates'
                              AND c.deleted = false
                              AND c.content.fundingStreamId = @fundingStreamId
                              AND c.content.fundingPeriodId = @fundingPeriodId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@fundingStreamId", fundingStreamId),
                    new CosmosDbQueryParameter("@fundingPeriodId", fundingPeriodId),
                }
            }, maxItemCount: 1)).SingleOrDefault();
        }
    }
}