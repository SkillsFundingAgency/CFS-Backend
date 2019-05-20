using System;
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
using Microsoft.Azure.Documents;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Results.Repositories
{
    public class CalculationResultsRepository : ICalculationResultsRepository, IHealthChecker
    {
        private readonly ICosmosRepository _cosmosRepository;

        public CalculationResultsRepository(ICosmosRepository cosmosRepository)
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
            //HACK "FirstOrDefault not supported", hence the roundabout code
            IEnumerable<ProviderResult> result = _cosmosRepository
                .Query<ProviderResult>()
                .Where(x => x.Provider.Id == providerId && x.SpecificationId == specificationId)
                .ToList()
                .Take(1);

            return Task.FromResult(result.FirstOrDefault());
        }

        public Task<IEnumerable<DocumentEntity<ProviderResult>>> GetAllProviderResults()
        {
            return _cosmosRepository.GetAllDocumentsAsync<ProviderResult>();
        }

        public async Task ProviderResultsBatchProcessing(string specificationId, Func<List<ProviderResult>, Task> persistIndexBatch)
        {
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = $@"SELECT
                                c.id as id,
                                c.createdAt as createdAt,
                                c.content.specificationId as specificationId,
                                {{
                                ""urn"" : c.content.provider.urn,
                                ""ukPrn"" : c.content.provider.ukPrn,
                                ""upin"" : c.content.provider.upin,
                                ""Id"" : c.content.provider.id,
                                ""Name"" : c.content.provider.name,
                                ""providerType"" : c.content.provider.providerType,
                                ""providerSubType"" : c.content.provider.providerSubType,
                                ""authority"" : c.content.provider.authority,
                                ""establishmentNumber"" : c.content.provider.establishmentNumber,
                                ""dateOpened"" : c.content.provider.dateOpened }} AS provider,
                                ARRAY(
    	                            SELECT calcResult.calculation as calculation, 
    	                            calcResult[""value""],
    	                            calcResult.exceptionType as exceptionType,
    	                            calcResult.exceptionMessage as exceptionMessage
    	                            FROM calcResult IN c.content.calcResults) AS calcResults
                            FROM    calculationresults c
                            WHERE   c.content.specificationId = @SpecificationID 
                                    AND c.documentType = 'ProviderResult' 
                                    AND c.deleted = false",
                Parameters = new SqlParameterCollection
                {
                    new SqlParameter("@SpecificationID", specificationId)
                }
            };

            await _cosmosRepository.DocumentsBatchProcessingAsync(persistBatchToIndex: persistIndexBatch, sqlQuerySpec: sqlQuerySpec);
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
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = "SELECT * FROM r WHERE r.content.provider.id = @ProviderId",
                Parameters = new SqlParameterCollection
                {
                    new SqlParameter("@ProviderId", providerId)
                }
            };

            dynamic[] resultsArray = _cosmosRepository.DynamicQuery<dynamic>(sqlQuerySpec, enableCrossPartitionQuery: true).ToArray();

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
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = @"SELECT value sum(c[""value""])
                            FROM results f 
                                JOIN c IN f.content.calcResults
                            WHERE c.calculationType = 10 
                                AND c[""value""] != null 
                                AND f.content.specificationId = @SpecificationId",
                Parameters = new SqlParameterCollection
                {
                    new SqlParameter("@SpecificationId", specificationId)
                }
            };

            IQueryable<decimal> result = _cosmosRepository.RawQuery<decimal>(sqlQuerySpec, 1, true);

            return Task.FromResult<decimal>(result.AsEnumerable().First());
        }

        public async Task<ProviderResult> GetSingleProviderResultBySpecificationId(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            IEnumerable<ProviderResult> providerResults = await GetProviderResultsBySpecificationId(specificationId, 1);

            if (providerResults.IsNullOrEmpty()) return null;

            return providerResults.First();
        }
    }
}
