﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Results.Interfaces;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Results.Repositories
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
            (bool Ok, string Message) cosmosRepoHealth = await _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(CalculationResultsRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosRepoHealth.Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = cosmosRepoHealth.Message });

            return health;
        }

        public Task<ProviderResult> GetProviderResult(string providerId, string specificationId)
        {
            IEnumerable<ProviderResult> results = _cosmosRepository.Query<ProviderResult>().Where(x => x.Provider.Id == providerId && x.SpecificationId == specificationId).ToList().Take(1);

            return Task.FromResult(results.FirstOrDefault());
        }

        public Task<IEnumerable<DocumentEntity<ProviderResult>>> GetAllProviderResults()
        {
            return _cosmosRepository.GetAllDocumentsAsync<ProviderResult>();
        }

        public Task<IEnumerable<DocumentEntity<ProviderResult>>> GetAllProviderResults(string specificationId)
        {
            return _cosmosRepository.GetAllDocumentsAsync<ProviderResult>(query: m => m.Content.SpecificationId == specificationId);
        }

        public Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId, int maxItemCount = -1)
        {
            List<ProviderResult> results;
            if (maxItemCount > 0)
            {
                results = _cosmosRepository.Query<ProviderResult>(enableCrossPartitionQuery: true).Where(x => x.SpecificationId == specificationId).Take(maxItemCount).ToList();
            }
            else
            {
                results = _cosmosRepository.Query<ProviderResult>(enableCrossPartitionQuery: true).Where(x => x.SpecificationId == specificationId).ToList();
            }

            return Task.FromResult(results.AsEnumerable());
        }

        public Task<IEnumerable<ProviderResult>> GetSpecificationResults(string providerId)
        {
            string sql = $"select * from r where r.content.provider.id = \"{ providerId }\"";

            dynamic[] resultsArray = _cosmosRepository.DynamicQuery<dynamic>(sql, enableCrossPartitionQuery: true).ToArray();

            string resultsString = JsonConvert.SerializeObject(resultsArray);

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

        public async Task<ProviderResult> GetSingleProviderResultBySpecificationId(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            IEnumerable<ProviderResult> providerResults = await GetProviderResultsBySpecificationId(specificationId, 1);

            if (providerResults.IsNullOrEmpty())
            {
                return null;
            }

            return providerResults.First();
        }
    }
}
