using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationsRepository : ICalculationsRepository, IHealthChecker
    {
        private readonly CosmosRepository _cosmosRepository;

        public CalculationsRepository(CosmosRepository cosmosRepository)
        {
            _cosmosRepository = cosmosRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth health = new ServiceHealth();

            (bool Ok, string Message) cosmosHealth = _cosmosRepository.IsHealthOk();

            health.Name = nameof(CalculationsRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosHealth.Ok, DependencyName = GetType().Name, Message = cosmosHealth.Message });

            return health;
        }

        public async Task<TemplateMapping> GetTemplateMapping(string specificationId, string fundingStreamId)
        {
            DocumentEntity<TemplateMapping> result = await _cosmosRepository.ReadDocumentByIdAsync<TemplateMapping>($"templatemapping-{specificationId}-{fundingStreamId}");

            return result?.Content;
        }

        public async Task UpdateTemplateMapping(string specificationId, string fundingStreamId, TemplateMapping templateMapping)
        {
            Guard.ArgumentNotNull(templateMapping, nameof(templateMapping));

            await _cosmosRepository.UpsertAsync(templateMapping);
        }

        public async Task<HttpStatusCode> CreateDraftCalculation(Calculation calculation)
        {
            return await _cosmosRepository.CreateAsync(calculation);
        }

        public async Task<Calculation> GetCalculationById(string calculationId)
        {
            DocumentEntity<Calculation> calculation = await _cosmosRepository.ReadDocumentByIdAsync<Calculation>(calculationId);

            return calculation?.Content;
        }

        public async Task<IEnumerable<Calculation>> GetCalculationsBySpecificationId(string specificationId)
        {
            return await _cosmosRepository.Query<Calculation>(x => x.Content.SpecificationId == specificationId);
        }

        public async Task<Calculation> GetCalculationsBySpecificationIdAndCalculationName(string specificationId, string calculationName)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(calculationName, nameof(calculationName));

            IEnumerable<Calculation> calculations = await _cosmosRepository.Query<Calculation>(m => m.Content.SpecificationId == specificationId && m.Content.Current.Name == calculationName);

            return (await GetCalculationsBySpecificationId(specificationId)).FirstOrDefault(m => string.Equals(m.Name.RemoveAllSpaces(), calculationName.RemoveAllSpaces(), StringComparison.CurrentCultureIgnoreCase));
        }

        public async Task<HttpStatusCode> UpdateCalculation(Calculation calculation)
        {
            return await _cosmosRepository.UpdateAsync(calculation);
        }

        public async Task<IEnumerable<Calculation>> GetAllCalculations()
        {
            return await _cosmosRepository.Query<Calculation>();
        }

        public async Task UpdateCalculations(IEnumerable<Calculation> calculations)
        {
            await _cosmosRepository.BulkUpsertAsync(calculations.ToList());
        }

        public async Task<StatusCounts> GetStatusCounts(string specificationId)
        {
            StatusCounts statusCounts = new StatusCounts();

            Task approvedCountTask = Task.Run(async () =>
            {
                statusCounts.Approved = (await QueryStatus(specificationId, "Approved")).First();
            });

            Task updatedCountTask = Task.Run(async () =>
            {
                statusCounts.Updated = (await QueryStatus(specificationId, "Updated")).First();
            });

            Task draftCountTask = Task.Run(async () =>
            {
                statusCounts.Draft = (await QueryStatus(specificationId, "Draft")).First();
            });

            await TaskHelper.WhenAllAndThrow(approvedCountTask, updatedCountTask, draftCountTask);

            return statusCounts;
        }

        public async Task<CompilerOptions> GetCompilerOptions(string specificationId)
        {
            CompilerOptions options = await _cosmosRepository.ReadByIdAsync<CompilerOptions>(specificationId);

            return options ?? new CompilerOptions();
        }

        public async Task<int> GetCountOfNonApprovedTemplateCalculations(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            IEnumerable<int> count = await GetCountOfNonApprovedTemplateCalculationsForSpecificationId(specificationId);

            return count.First();
        }

        private async Task<IEnumerable<int>> QueryStatus(string specificationId, string publishStatus)
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT VALUE COUNT(1)
                            FROM    c 
                            WHERE   c.documentType = 'Calculation' 
                                    AND c.content.current.calculationType = 'Template'
                                    AND c.content.current.publishStatus = @PublishStatus
                                    AND c.content.specificationId = @SpecificationId",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@PublishStatus", publishStatus),
                    new CosmosDbQueryParameter("@SpecificationId", specificationId)
                }
            };

            IEnumerable<int> result = await _cosmosRepository.RawQuery<int>(cosmosDbQuery, 1);
            return result;
        }

        public async Task<IEnumerable<CalculationMetadata>> GetCalculationsMetatadataBySpecificationId(string specificationId)
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = @"SELECT 
                                    c.content.specificationId as SpecificationId,
                                    c.content.fundingStreamId as FundingStreamId,
                                    c.content.current.calculationId as CalculationId,
                                    c.content.current.valueType as ValueType,
                                    c.content.current.name as Name,
                                    c.content.current.sourceCodeName as SourceCodeName,
                                    c.content.current.calculationType as CalculationType,
                                    c.content.current.namespace as Namespace,
                                    c.content.current.wasTemplateCalculation as WasTemplateCalculation,
                                    c.content.current.description as Description,
                                    c.content.current.publishStatus as PublishStatus
                                    FROM c 
                                    WHERE c.content.specificationId = @SpecificationId and c.documentType = 'Calculation'",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@SpecificationId", specificationId)
                }
            };

            IEnumerable<dynamic> results = await _cosmosRepository.DynamicQuery(cosmosDbQuery);

            string resultsString = JsonConvert.SerializeObject(results.ToArray());

            CalculationMetadata[] items = JsonConvert.DeserializeObject<CalculationMetadata[]>(resultsString);

            return await Task.FromResult(items);
        }

        private async Task<IEnumerable<int>> GetCountOfNonApprovedTemplateCalculationsForSpecificationId(string specificationId)
        {
            CosmosDbQuery cosmosDbQuery = new CosmosDbQuery
            {
                QueryText = $@"SELECT VALUE COUNT(1)
                            FROM    c 
                            WHERE   c.documentType = 'Calculation' 
                                    AND c.content.current.publishStatus != 'Approved'
                                    AND c.content.specificationId = @SpecificationId
                                    AND c.content.current.calculationType = 'Template'",
                Parameters = new[]
                {
                    new CosmosDbQueryParameter("@SpecificationId", specificationId)
                }
            };

            IEnumerable<int> result = await _cosmosRepository.RawQuery<int>(cosmosDbQuery, 1);
            return result;
        }
    }
}
