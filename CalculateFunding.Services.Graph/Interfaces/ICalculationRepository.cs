using CalculateFunding.Models.Graph;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Graph.Interfaces
{
    public interface ICalculationRepository
    {
        Task DeleteCalculation(string calculationId);

        Task UpsertCalculations(IEnumerable<Calculation> calculations);        
        Task UpsertCalculationSpecificationRelationship(string calculationId, string specificationId);
        Task UpsertCalculationCalculationRelationship(string calculationIdA, string calculationIdB);
        Task DeleteCalculationSpecificationRelationship(string calculationId, string specificationId);
        Task DeleteCalculationCalculationRelationship(string calculationIdA, string calculationIdB);

    }
}
