﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Models.Results;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Results.Interfaces;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Results
{
    public class ResultsRepository : IResultsRepository
    {
        private readonly CosmosRepository _cosmosRepository;

        public ResultsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

	    public Task<ProviderResult> GetProviderResult(string providerId, string specificationId)
	    {
		    var results = _cosmosRepository.Query<ProviderResult>().Where(x => x.Provider.Id == providerId && x.Specification.Id == specificationId).ToList().Take(1);

		    return Task.FromResult(results.FirstOrDefault());
		}

        public Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId)
        {
            var results = _cosmosRepository.Query<ProviderResult>().Where(x => x.Specification.Id == specificationId).ToList();

            return Task.FromResult(results.AsEnumerable());
        }

        public Task<IEnumerable<ProviderResult>> GetSpecificationResults(string providerId)
	    {
            string sql = $"select * from r where r.content.provider.id = \"{ providerId }\"";

            var resultsArray = _cosmosRepository.DynamicQuery<dynamic>(sql).ToArray();

            var resultsString = JsonConvert.SerializeObject(resultsArray);

            resultsString = resultsString.Replace("-7.9228162514264338E+28", "0");

            DocumentEntity<ProviderResult>[] documentEntities = JsonConvert.DeserializeObject<DocumentEntity<ProviderResult>[]>(resultsString);

            IEnumerable<ProviderResult> providerResults = documentEntities.Select(m => m.Content).ToList();

            return Task.FromResult(providerResults);
        }

	    public Task<HttpStatusCode> UpdateProviderResults(List<ProviderResult> results)
	    {
            return _cosmosRepository.BulkUpdateAsync(results, "usp_update_provider_results");
	    }

        public Task<HttpStatusCode> UpsertProviderSourceDataset(ProviderSourceDataset providerSourceDataset)
        {
            return _cosmosRepository.CreateAsync(providerSourceDataset);
        }

        public Task<IEnumerable<ProviderSourceDataset>> GetProviderSourceDatasets(string providerId, string specificationId)
        {
            var results = _cosmosRepository.Query<ProviderSourceDataset>().Where(x => x.Provider.Id == providerId && x.Specification.Id == specificationId);

            return Task.FromResult(results.AsEnumerable());
        }
    }
}
