using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System;
using CalculateFunding.Services.Core.Extensions;
using Serilog;
using Neo4jDriver = Neo4j.Driver;
using CalculateFunding.Services.Core.Helpers;

namespace CalculateFunding.Services.Graph
{
    public class GraphService : IGraphService
    {
        private ILogger _logger;
        private ICalculationRepository _calcRepository;
        private ISpecificationRepository _specRepository;

        public GraphService(ILogger logger, ICalculationRepository calcRepository, ISpecificationRepository specRepository)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(calcRepository, nameof(calcRepository));
            Guard.ArgumentNotNull(specRepository, nameof(specRepository));

            _logger = logger;
            _calcRepository = calcRepository;
            _specRepository = specRepository;
        }

        public async Task<IActionResult> DeleteSpecification(string specificationId)
        {
            try
            {
                await _specRepository.DeleteSpecification(specificationId);

                return new OkResult();
            }
            catch (Neo4jDriver.Neo4jException ex)
            {
                string error = $"Delete specification failed for specification:'{specificationId}'";
                _logger.Error(error);
                return new InternalServerErrorResult(ex.ToString());
            }
        }

        public async Task<IActionResult> UpsertSpecifications(IEnumerable<Specification> specifications)
        {
            try
            {
                await _specRepository.UpsertSpecifications(specifications);

                return new OkResult();
            }
            catch (Neo4jDriver.Neo4jException ex)
            {
                string error = $"Save specifications failed for specifications:'{specifications.AsJson()}'";
                _logger.Error(error);
                return new InternalServerErrorResult(ex.ToString());
            }
        }

        public async Task<IActionResult> DeleteCalculation(string calculationId)
        {
            try 
            { 
                await _calcRepository.DeleteCalculation(calculationId);

                return new OkResult();
            }
            catch (Neo4jDriver.Neo4jException ex)
            {
                string error = $"Delete calculation failed for calculation:'{calculationId}'";
                _logger.Error(error);
                return new InternalServerErrorResult(ex.ToString());
            }
        }

        public async Task<IActionResult> UpsertCalculations(IEnumerable<Calculation> calculations)
        {
            try
            {
                await _calcRepository.UpsertCalculations(calculations);

                return new OkResult();
            }
            catch (Neo4jDriver.Neo4jException ex)
            {
                string error = $"Upsert calculations failed for calculations:'{calculations.AsJson()}'";
                _logger.Error(error);
                return new InternalServerErrorResult(ex.ToString());
            }
        }

        public async Task<IActionResult> UpsertCalculationSpecificationRelationship(string calculationId, string specificationId)
        {
            try
            {
                await _calcRepository.UpsertCalculationSpecificationRelationship(calculationId, specificationId);

                return new OkResult();
            }
            catch (Neo4jDriver.Neo4jException ex)
            {
                string error = $"Upsert calculation relationship between specification failed for calculation:'{calculationId}'" +
                    $" and specification:'{specificationId}'";
                _logger.Error(error);
                return new InternalServerErrorResult(ex.ToString());
            }
        }

        public async Task<IActionResult> UpsertCalculationCalculationRelationship(string calculationIdA, string calculationIdB)
        {
            try
            {
                await _calcRepository.UpsertCalculationCalculationRelationship(calculationIdA, calculationIdB);

                return new OkResult();
            }
            catch (Neo4jDriver.Neo4jException ex)
            {
                string error = $"Upsert calculation relationship call to calculation failed for calculation:'{calculationIdA}'" +
                    $" calling calculation:'{calculationIdB}'";
                _logger.Error(error);
                return new InternalServerErrorResult(ex.ToString());
            }
        }

        public async Task<IActionResult> UpsertCalculationCalculationsRelationships(string calculationId, string[] calculationIds)
        {
            try
            {
                IEnumerable<Task> tasks = calculationIds.Select(async(_) =>
                {
                    await _calcRepository.UpsertCalculationCalculationRelationship(calculationId, _);
                });

                await TaskHelper.WhenAllAndThrow(tasks.ToArray());

                return new OkResult();
            }
            catch (Neo4jDriver.Neo4jException ex)
            {
                string error = $"Upsert calculation relationship call to calculation failed for calculation:'{calculationId}'" +
                    $" calling calculations:'{calculationIds.AsJson()}'";
                _logger.Error(error);
                return new InternalServerErrorResult(ex.ToString());
            }
        }

        public async Task<IActionResult> DeleteCalculationSpecificationRelationship(string calculationId, string specificationId)
        {
            try
            {
                await _calcRepository.DeleteCalculationSpecificationRelationship(calculationId, specificationId);

                return new OkResult();
            }
            catch (Neo4jDriver.Neo4jException ex)
            {
                string error = $"Delete calculation relationship between specification failed for calculation:'{calculationId}'" +
                    $" and specification:'{specificationId}'";
                _logger.Error(error);
                return new InternalServerErrorResult(ex.ToString());
            }
        }

        public async Task<IActionResult> DeleteCalculationCalculationRelationship(string calculationIdA, string calculationIdB)
        {
            try
            {
                await _calcRepository.DeleteCalculationCalculationRelationship(calculationIdA, calculationIdB);

                return new OkResult();
            }
            catch (Neo4jDriver.Neo4jException ex)
            {
                string error = $"Delete calculation relationship call to calculation failed for calculation:'{calculationIdA}'" +
                    $" calling calculation:'{calculationIdB}'";
                _logger.Error(error);
                return new InternalServerErrorResult(ex.ToString());
            }
        }
    }
}
