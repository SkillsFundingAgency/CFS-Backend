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
using Microsoft.Azure.Documents;
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

            (bool Ok, string Message) cosmosHealth = await _cosmosRepository.IsHealthOk();

            health.Name = nameof(CalculationsRepository);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cosmosHealth.Ok, DependencyName = GetType().Name, Message = cosmosHealth.Message });

            return health;
        }
        
        public async Task<TemplateMapping> GetTemplateMapping(string specificationId, string fundingStreamId)
        {
            DocumentEntity<TemplateMapping> result = await _cosmosRepository.ReadAsync<TemplateMapping>($"templatemapping-{specificationId}-{fundingStreamId}");
            
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
            Common.Models.DocumentEntity<Calculation> calculation = await _cosmosRepository.ReadAsync<Calculation>(calculationId);

            return calculation?.Content;
        }

        public Task<IEnumerable<Calculation>> GetCalculationsBySpecificationId(string specificationId)
        {
            IQueryable<Calculation> calculations = _cosmosRepository.Query<Calculation>().Where(x => x.SpecificationId == specificationId);

            return Task.FromResult(calculations.AsEnumerable());
        }

        public async Task<Calculation> GetCalculationsBySpecificationIdAndCalculationName(string specificationId, string calculationName)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(calculationName, nameof(calculationName));

            IQueryable<Calculation> calculations = _cosmosRepository.Query<Calculation>().Where(m => m.SpecificationId == specificationId && m.Current.Name == calculationName);

            return (await GetCalculationsBySpecificationId(specificationId)).FirstOrDefault(m => string.Equals(m.Name.RemoveAllSpaces(), calculationName.RemoveAllSpaces(), StringComparison.CurrentCultureIgnoreCase));
        }

        public async Task<HttpStatusCode> UpdateCalculation(Calculation calculation)
        {
            return await _cosmosRepository.UpdateAsync(calculation);
        }

        public Task<IEnumerable<Calculation>> GetAllCalculations()
        {
            IQueryable<Calculation> calculations = _cosmosRepository.Query<Calculation>();

            return Task.FromResult(calculations.AsEnumerable());
        }

        public async Task UpdateCalculations(IEnumerable<Calculation> calculations)
        {
            await _cosmosRepository.BulkUpsertAsync(calculations.ToList());
        }

        public async Task<StatusCounts> GetStatusCounts(string specificationId)
        {
            StatusCounts statusCounts = new StatusCounts();

            Task approvedCountTask = Task.Run(() =>
            {
                statusCounts.Approved = QueryStatus(specificationId, "Approved").AsEnumerable().First();
            });

            Task updatedCountTask = Task.Run(() =>
            {
                statusCounts.Updated = QueryStatus(specificationId, "Updated").AsEnumerable().First();
            });

            Task draftCountTask = Task.Run(() =>
            {
                statusCounts.Draft = QueryStatus(specificationId, "Draft").AsEnumerable().First();
            });

            await TaskHelper.WhenAllAndThrow(approvedCountTask, updatedCountTask, draftCountTask);

            return statusCounts;
        }

        public async Task<CompilerOptions> GetCompilerOptions(string specificationId)
        {
            Common.Models.DocumentEntity<CompilerOptions> options = await _cosmosRepository.ReadAsync<CompilerOptions>(specificationId);

            return options?.Content ?? new CompilerOptions();
        }

        public Task<int> GetCountOfNonApprovedTemplateCalculations(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
           
            IQueryable<int> count = GetCountOfNonApprovedTemplateCalculationsForSpecificationId(specificationId);

            return Task.FromResult(count.AsEnumerable().First());
        }

        private IQueryable<int> QueryStatus(string specificationId, string publishStatus)
        {
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = @"SELECT VALUE COUNT(1)
                            FROM    c 
                            WHERE   c.documentType = 'Calculation' 
                                    AND c.content.current.publishStatus = @PublishStatus
                                    AND c.content.specificationId = @SpecificationId"
            };

            SqlParameter[] sqlParameters =
            {
                new SqlParameter("@PublishStatus", publishStatus),
                new SqlParameter("@SpecificationId", specificationId)
            };

            sqlQuerySpec.Parameters = new SqlParameterCollection(sqlParameters);
            IQueryable<int> result = _cosmosRepository.RawQuery<int>(sqlQuerySpec, 1, true);
            return result;
        }

        public async Task<IEnumerable<CalculationMetadata>> GetCalculationsMetatadataBySpecificationId(string specificationId)
        {
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
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
                Parameters = new SqlParameterCollection
                {
                    new SqlParameter("@SpecificationId", specificationId)
                }
            };

            dynamic[] resultsArray = _cosmosRepository.DynamicQuery<dynamic>(sqlQuerySpec, enableCrossPartitionQuery: true).ToArray();

            string resultsString = JsonConvert.SerializeObject(resultsArray);

            CalculationMetadata[] items = JsonConvert.DeserializeObject<CalculationMetadata[]>(resultsString);

            return await Task.FromResult(items);
        }

        private IQueryable<int> GetCountOfNonApprovedTemplateCalculationsForSpecificationId(string specificationId)
        {
            SqlQuerySpec sqlQuerySpec = new SqlQuerySpec
            {
                QueryText = $@"SELECT VALUE COUNT(1)
                            FROM    c 
                            WHERE   c.documentType = 'Calculation' 
                                    AND c.content.current.publishStatus != 'Approved'
                                    AND c.content.specificationId = @SpecificationId
                                    AND c.content.current.calculationType = 'Template'"
            };

            SqlParameter[] sqlParameters =
            {
                new SqlParameter("@SpecificationId", specificationId)
            };

            sqlQuerySpec.Parameters = new SqlParameterCollection(sqlParameters);
            IQueryable<int> result = _cosmosRepository.RawQuery<int>(sqlQuerySpec, 1, true);
            return result;
        }
    }
}
