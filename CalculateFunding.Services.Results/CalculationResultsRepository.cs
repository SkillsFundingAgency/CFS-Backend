using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Results.Interfaces;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Results
{
    public class CalculationResultsRepository : ICalculationResultsRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;

        public CalculationResultsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var cosmosRepoHealth = await _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(CalculationResultsRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
        }

        public Task<ProviderResult> GetProviderResult(string providerId, string specificationId)
	    {
		    var results = _cosmosRepository.Query<ProviderResult>().Where(x => x.Provider.Id == providerId && x.SpecificationId == specificationId).ToList().Take(1);

		    return Task.FromResult(results.FirstOrDefault());
		}

        public Task<IEnumerable<DocumentEntity<ProviderResult>>> GetAllProviderResults()
        {
            return _cosmosRepository.GetAllDocumentsAsync<ProviderResult>();
        }

        public Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId, int maxItemCount = -1)
        {
            var results = _cosmosRepository.Query<ProviderResult>(maxItemCount: maxItemCount, enableCrossPartitionQuery: true).Where(x => x.SpecificationId == specificationId).ToList();

            return Task.FromResult(results.AsEnumerable());
        }

        public Task<IEnumerable<ProviderResult>> GetSpecificationResults(string providerId)
	    {
            string sql = $"select * from r where r.content.provider.id = \"{ providerId }\"";

            var resultsArray = _cosmosRepository.DynamicQuery<dynamic>(sql, enableCrossPartitionQuery: true).ToArray();

            var resultsString = JsonConvert.SerializeObject(resultsArray);

            resultsString = resultsString.ConvertExpotentialNumber();

            DocumentEntity<ProviderResult>[] documentEntities = JsonConvert.DeserializeObject<DocumentEntity<ProviderResult>[]>(resultsString);

            IEnumerable<ProviderResult> providerResults = documentEntities.Select(m => m.Content).ToList();

            return Task.FromResult(providerResults);
        }

	    public Task<HttpStatusCode> UpdateProviderResults(List<ProviderResult> results)
	    {
            return _cosmosRepository.BulkUpdateAsync(results, "usp_update_provider_results");
	    }

        public Task<decimal> GetCalculationResultTotalForSpecificationId(string specificationId)
        {
            string sql = $"SELECT value sum(c[\"value\"]) from results f join c in f.content.calcResults where c.calculationType = 10 and c[\"value\"] != null and f.content.specificationId = \"{ specificationId }\"";

            IQueryable<decimal> result = _cosmosRepository.RawQuery<decimal>(sql, 1, true);

            return Task.FromResult<decimal>(result.AsEnumerable().First());
        }
    }
}
