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

namespace CalculateFunding.Services.Graph
{
    public class GraphService : IGraphService
    {
        private ICalculationRepository _calcRepository;
        private ISpecificationRepository _specRepository;

        public GraphService(ICalculationRepository calcRepository, ISpecificationRepository specRepository)
        {
            Guard.ArgumentNotNull(calcRepository, nameof(calcRepository));
            Guard.ArgumentNotNull(specRepository, nameof(specRepository));

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
            catch (Exception ex)
            {
                return new InternalServerErrorResult(ex.Message);
            }
        }

        public async Task<IActionResult> SaveSpecifications(IEnumerable<Specification> specifications)
        {
            try
            {
                await _specRepository.SaveSpecifications(specifications);

                return new OkResult();
            }
            catch (Exception ex)
            {
                return new InternalServerErrorResult(ex.Message);
            }
        }

        public async Task<IActionResult> DeleteCalculation(string calculationId)
        {
            try 
            { 
                await _calcRepository.DeleteCalculation(calculationId);

                return new OkResult();
            }
            catch (Exception ex)
            {
                return new InternalServerErrorResult(ex.Message);
            }
        }

        public async Task<IActionResult> SaveCalculations(IEnumerable<Calculation> calculations)
        {
            try
            {
                await _calcRepository.SaveCalculations(calculations);

                return new OkResult();
            }
            catch (Exception ex)
            {
                return new InternalServerErrorResult(ex.Message);
            }
        }

        public async Task<IActionResult> CreateCalculationSpecificationRelationship(string calculationId, string specificationId)
        {
            try
            {
                await _calcRepository.CreateCalculationSpecificationRelationship(calculationId, specificationId);

                return new OkResult();
            }
            catch (Exception ex)
            {
                return new InternalServerErrorResult(ex.Message);
            }
        }

        public async Task<IActionResult> CreateCalculationCalculationRelationship(string calculationIdA, string calculationIdB)
        {
            try
            {
                await _calcRepository.CreateCalculationCalculationRelationship(calculationIdA, calculationIdB);

                return new OkResult();
            }
            catch (Exception ex)
            {
                return new InternalServerErrorResult(ex.Message);
            }
        }

        public async Task<IActionResult> DeleteCalculationSpecificationRelationship(string calculationId, string specificationId)
        {
            try
            {
                await _calcRepository.DeleteCalculationSpecificationRelationship(calculationId, specificationId);

                return new OkResult();
            }
            catch (Exception ex)
            {
                return new InternalServerErrorResult(ex.Message);
            }
        }

        public async Task<IActionResult> DeleteCalculationCalculationRelationship(string calculationIdA, string calculationIdB)
        {
            try
            {
                await _calcRepository.DeleteCalculationCalculationRelationship(calculationIdA, calculationIdB);

                return new OkResult();
            }
            catch (Exception ex)
            {
                return new InternalServerErrorResult(ex.Message);
            }
        }
    }
}
