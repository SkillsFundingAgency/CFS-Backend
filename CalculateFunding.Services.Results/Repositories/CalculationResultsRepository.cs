using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Models;
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

        public Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = _cosmosRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(CalculationResultsRepository)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = _cosmosRepository.GetType().GetFriendlyName(), Message = Message });

            return Task.FromResult(health);
        }

        public async Task<ProviderResult> GetProviderResult(string providerId, string specificationId)
        {
            //HACK "FirstOrDefault not supported", hence the roundabout code
            IEnumerable<ProviderResult> result = await _cosmosRepository
                .Query<ProviderResult>(x => x.Content.Provider.Id == providerId && x.Content.SpecificationId == specificationId, 1);

            return result.FirstOrDefault();
        }

        public async Task<ProviderResult> GetProviderResultByCalculationType(string providerId, string specificationId, CalculationType calculationType)
        {
            IEnumerable<ProviderResult> result = await _cosmosRepository
                .Query<ProviderResult>(x => x.Content.Provider.Id == providerId && x.Content.SpecificationId == specificationId, 1);

            ProviderResult providerResult = result.FirstOrDefault();

            providerResult?.CalculationResults.RemoveAll(_ => _.CalculationType != calculationType);

            return providerResult;
        }

        public Task<IEnumerable<DocumentEntity<ProviderResult>>> GetAllProviderResults()
        {
            return _cosmosRepository.GetAllDocumentsAsync<ProviderResult>();
        }

        public async Task ProviderResultsBatchProcessing(string specificationId, Func<List<ProviderResult>, Task> processProcessProviderResultsBatch, int itemsPerPage = 1000)
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
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
                                ""laCode"" : c.content.provider.laCode,
                                ""localAuthorityName"" : c.content.provider.localAuthorityName,
                                ""establishmentNumber"" : c.content.provider.establishmentNumber,
                                ""dateOpened"" : c.content.provider.dateOpened }} AS provider,
                                ARRAY(
    	                            SELECT calcResult.calculation as calculation, 
    	                            calcResult[""value""],
    	                            calcResult.exceptionType as exceptionType,
    	                            calcResult.exceptionMessage as exceptionMessage,
    	                            calcResult.calculationType as calculationType
    	                            FROM calcResult IN c.content.calcResults) AS calcResults,
                                ARRAY(
                                    SELECT fundingLineResult.fundingLine as fundingLine,
                                    fundingLineResult.fundingLineFundingStreamId as fundingLineFundingStreamId,
                                    fundingLineResult[""value""],
    	                            fundingLineResult.exceptionType as exceptionType,
    	                            fundingLineResult.exceptionMessage as exceptionMessage
                                    FROM fundingLineResult IN c.content.fundingLineResults) AS fundingLineResults
                            FROM    calculationresults c
                            WHERE   c.content.specificationId = @SpecificationId 
                                    AND c.documentType = 'ProviderResult' 
                                    AND c.deleted = false",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@SpecificationId", specificationId)
                }
            };

            await _cosmosRepository.DocumentsBatchProcessingAsync(persistBatchToIndex: processProcessProviderResultsBatch,
				cosmosDbQuery: cosmosDbQuery,
                itemsPerPage: itemsPerPage);
        }

        public async Task DeleteCurrentProviderResults(IEnumerable<ProviderResult> providerResults)
        {
            Guard.ArgumentNotNull(providerResults, nameof(providerResults));

            await _cosmosRepository.BulkDeleteAsync<ProviderResult>(
                providerResults.Select(x => new KeyValuePair<string, ProviderResult>(x.Provider.Id, x)),
                degreeOfParallelism: 15,
                hardDelete: true);
        }

        public ICosmosDbFeedIterator<ProviderWithResultsForSpecifications> GetProvidersWithResultsForSpecificationBySpecificationId(string specificationId)
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT * 
                              FROM    providerWithResultsForSpecifications p
                              WHERE   p.documentType = 'ProviderWithResultsForSpecifications'
                              AND p.deleted = false
                              AND EXISTS(SELECT VALUE specification FROM specification IN p.content.specifications WHERE specification.id = @specificationId)",
                Parameters = new []
                {
                    new CosmosDbQueryParameter("@specificationId", specificationId), 
                }
            };
            
            return _cosmosRepository.GetFeedIterator<ProviderWithResultsForSpecifications>(cosmosDbQuery);
        }

        public async Task<ProviderWithResultsForSpecifications> GetProviderWithResultsForSpecificationsByProviderId(string providerId)
        {
            return (await _cosmosRepository.Query<ProviderWithResultsForSpecifications>(_ => _.Content.Provider.Id == providerId))
                .SingleOrDefault();
        }

        public async Task UpsertSpecificationWithProviderResults(params ProviderWithResultsForSpecifications[] providerWithResultsForSpecifications)
        {
            await _cosmosRepository.BulkUpsertAsync(providerWithResultsForSpecifications.ToList());
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
                    CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
                    {
                        QueryText = @"SELECT * 
                                FROM    Root r 
                                WHERE   r.documentType = @DocumentType 
                                        AND r.content.specificationId = @SpecificationId
                                        AND r.deleted = false",
                        Parameters = new[]
                   {
                       new CosmosDbQueryParameter("@DocumentType", nameof(ProviderResult)),
                       new CosmosDbQueryParameter("@SpecificationId", specificationId)
                   }
                    };

                    IEnumerable<ProviderResult> providerResults = await _cosmosRepository.QueryPartitionedEntity<ProviderResult>(cosmosDbQuery, partitionKey: providerId);
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

        public async Task<bool> ProviderHasResultsBySpecificationId(string specificationId)
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = "SELECT VALUE COUNT(1) FROM c WHERE c.documentType = 'ProviderResult' AND  c.content.specificationId = @SpecificationId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@SpecificationId", specificationId)
                }
            };

            IEnumerable<bool> result = await _cosmosRepository.RawQuery<bool>(cosmosDbQuery, 1);
            return result.FirstOrDefault();
        }

        public async Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId, int maxItemCount = -1)
        {
            IEnumerable<ProviderResult> results;
            if (maxItemCount > 0)
            {
                results = await _cosmosRepository.Query<ProviderResult>(x => x.Content.SpecificationId == specificationId, maxItemCount: maxItemCount);
            }
            else
            {
                results = await _cosmosRepository.Query<ProviderResult>(x => x.Content.SpecificationId == specificationId);
            }

            return results;
        }

        public async Task<IEnumerable<ProviderResult>> GetSpecificationResults(string providerId)
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = "SELECT * FROM r WHERE r.content.provider.id = @ProviderId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@ProviderId", providerId)
                }
            };

            IEnumerable<dynamic> results = await _cosmosRepository.DynamicQuery(cosmosDbQuery);

            string resultsString = JsonConvert.SerializeObject(results.ToArray());

            resultsString = resultsString.ConvertExpotentialNumber();

            DocumentEntity<ProviderResult>[] documentEntities = JsonConvert.DeserializeObject<DocumentEntity<ProviderResult>[]>(resultsString);

            IEnumerable<ProviderResult> providerResults = documentEntities.Select(m => m.Content).ToList();

            return providerResults;
        }

        public Task<HttpStatusCode> UpdateProviderResults(List<ProviderResult> results)
        {
            return _cosmosRepository.BulkUpdateAsync(results, "usp_update_provider_results");
        }

        public async Task<decimal> GetCalculationResultTotalForSpecificationId(string specificationId)
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT value sum(c[""value""])
                            FROM results f 
                                JOIN c IN f.content.calcResults
                            WHERE c.calculationType = 10 
                                AND c[""value""] != null 
                                AND f.content.specificationId = @SpecificationId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@SpecificationId", specificationId)
                }
            };

            IEnumerable<decimal> result = await _cosmosRepository.RawQuery<decimal>(cosmosDbQuery, 1);

            return result.First();
        }

        public async Task<bool> CheckHasNewResultsForSpecificationIdAndTimePeriod(string specificationId, DateTimeOffset dateFrom, DateTimeOffset dateTo)
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT c.id 
                            FROM calculationresults c
                            WHERE c.content.specificationId = @SpecificationId
                            AND c.documentType = 'ProviderResult'
                            AND (c.createdAt >= @DateFrom AND c.createdAt < @DateTo)",

                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@SpecificationId", specificationId),
                    new CosmosDbQueryParameter("@DateFrom", dateFrom),
                    new CosmosDbQueryParameter("@DateTo", dateTo)
                }
            };

            IEnumerable<dynamic> result = await _cosmosRepository.DynamicQuery(cosmosDbQuery, 1);

            return result.FirstOrDefault() != null;
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
