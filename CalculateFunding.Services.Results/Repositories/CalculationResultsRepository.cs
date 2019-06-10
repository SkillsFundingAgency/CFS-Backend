using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
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
                            WHERE   c.content.specificationId = @SpecificationId 
                                    AND c.documentType = 'ProviderResult' 
                                    AND c.deleted = false",
                Parameters = new SqlParameterCollection
                {
                    new SqlParameter("@SpecificationId", specificationId)
                }
            };

            await _cosmosRepository.DocumentsBatchProcessingAsync(persistBatchToIndex: persistIndexBatch, sqlQuerySpec: sqlQuerySpec);
        }

        public async Task DeleteCurrentProviderResults(IEnumerable<ProviderResult> providerResults)
        {
            Guard.ArgumentNotNull(providerResults, nameof(providerResults));

            await _cosmosRepository.BulkDeleteAsync<ProviderResult>(
                providerResults.Select(x => new KeyValuePair<string, ProviderResult>(x.Provider.Id, x)),
                degreeOfParallelism: 15, 
                hardDelete: true);
        }

        public async Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationIdAndProviders(IEnumerable<string> providerIds, string specificationId)
        {
            Guard.ArgumentNotNull(providerIds, nameof(providerIds));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            if (providerIds.IsNullOrEmpty())
            {
                return Enumerable.Empty<ProviderResult>();
            }

            ConcurrentBag<ProviderResult> results = new ConcurrentBag<ProviderResult>();

            int completedCount = 0;

            Parallel.ForEach(providerIds, async (providerId) =>
            {
                try
                {
                    SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
                    {
                        QueryText = @"SELECT * 
                                FROM    Root r 
                                WHERE   r.documentType = @DocumentType 
                                        AND r.content.specificationId = @SpecificationId
                                        AND r.deleted = false",
                        Parameters = new SqlParameterCollection
                   {
                       new SqlParameter("@DocumentType", nameof(ProviderResult)),
                       new SqlParameter("@SpecificationId", specificationId)
                   }
                    };

                    IEnumerable<ProviderResult> providerResults = await _cosmosRepository.QueryPartitionedEntity<ProviderResult>(sqlQuerySpec, partitionEntityId: providerId);
                    foreach (ProviderResult providerResult in providerResults)
                    {
                        results.Add(providerResult);
                    }
                }
                finally
                {
                    completedCount++;
                }
            });

            while (completedCount < providerIds.Count())
            {
                await Task.Delay(20);
            }

            return results.AsEnumerable();
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

            return Task.FromResult(result.AsEnumerable().First());
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
