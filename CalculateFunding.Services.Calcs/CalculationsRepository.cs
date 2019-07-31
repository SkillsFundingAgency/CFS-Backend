﻿using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Helpers;
using Microsoft.Azure.Documents;

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

        public Task<Calculation> GetCalculationByCalculationSpecificationId(string calculationSpecificationId)
        {
            IQueryable<Calculation> calculations = _cosmosRepository.Query<Calculation>().Where(x => x.CalculationSpecification.Id == calculationSpecificationId);

            return Task.FromResult(calculations.AsEnumerable().FirstOrDefault());
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
    }
}
