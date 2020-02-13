using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Graph;

namespace CalculateFunding.Services.Graph.Interfaces
{
    public interface IGraphService
    {
        Task<IActionResult> SaveCalculations(IEnumerable<Calculation> calculations);
        Task<IActionResult> SaveSpecifications(IEnumerable<Specification> specifications);
        Task<IActionResult> DeleteCalculation(string calculationId);
        Task<IActionResult> DeleteSpecification(string specificationId);
        Task<IActionResult> CreateCalculationSpecificationRelationship(string calculationId, string specificationId);
        Task<IActionResult> CreateCalculationCalculationRelationship(string calculationIdA, string calculationIdB);
        Task<IActionResult> DeleteCalculationSpecificationRelationship(string calculationId, string specificationId);
        Task<IActionResult> DeleteCalculationCalculationRelationship(string calculationIdA, string calculationIdB);
    }
}
