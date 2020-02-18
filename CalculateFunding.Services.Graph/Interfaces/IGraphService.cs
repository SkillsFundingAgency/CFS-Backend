using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Graph;

namespace CalculateFunding.Services.Graph.Interfaces
{
    public interface IGraphService
    {
        Task<IActionResult> UpsertCalculations(IEnumerable<Calculation> calculations);
        Task<IActionResult> UpsertSpecifications(IEnumerable<Specification> specifications);
        Task<IActionResult> DeleteCalculation(string calculationId);
        Task<IActionResult> DeleteSpecification(string specificationId);
        Task<IActionResult> UpsertCalculationSpecificationRelationship(string calculationId, string specificationId);
        Task<IActionResult> UpsertCalculationCalculationRelationship(string calculationIdA, string calculationIdB);
        Task<IActionResult> UpsertCalculationCalculationsRelationships(string calculationId, string[] calculationIds);
        Task<IActionResult> DeleteCalculationSpecificationRelationship(string calculationId, string specificationId);
        Task<IActionResult> DeleteCalculationCalculationRelationship(string calculationIdA, string calculationIdB);
    }
}
