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

        public async Task<IActionResult> SaveSpecifications(IEnumerable<Specification> specifications)
        {
            try
            {
                await _specRepository.SaveSpecifications(specifications);

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

        public async Task<IActionResult> SaveCalculations(IEnumerable<Calculation> calculations)
        {
            try
            {
                await _calcRepository.SaveCalculations(calculations);

                return new OkResult();
            }
            catch (Neo4jDriver.Neo4jException ex)
            {
                string error = $"Save calculations failed for calculations:'{calculations.AsJson()}'";
                _logger.Error(error);
                return new InternalServerErrorResult(ex.ToString());
            }
        }

        public async Task<IActionResult> CreateCalculationSpecificationRelationship(string calculationId, string specificationId)
        {
            try
            {
                await _calcRepository.CreateCalculationSpecificationRelationship(calculationId, specificationId);

                return new OkResult();
            }
            catch (Neo4jDriver.Neo4jException ex)
            {
                string error = $"Create calculation relationship between specification failed for calculation:'{calculationId}'" +
                    $" and specification:'{specificationId}'";
                _logger.Error(error);
                return new InternalServerErrorResult(ex.ToString());
            }
        }

        public async Task<IActionResult> CreateCalculationCalculationRelationship(string calculationIdA, string calculationIdB)
        {
            try
            {
                await _calcRepository.CreateCalculationCalculationRelationship(calculationIdA, calculationIdB);

                return new OkResult();
            }
            catch (Neo4jDriver.Neo4jException ex)
            {
                string error = $"Create calculation relationship call to calculation failed for calculation:'{calculationIdA}'" +
                    $" calling calculation:'{calculationIdB}'";
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
