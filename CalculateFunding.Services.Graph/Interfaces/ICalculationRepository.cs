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

        Task SaveCalculations(IEnumerable<Calculation> calculations);        
        Task CreateCalculationSpecificationRelationship(string calculationId, string specificationId);
        Task CreateCalculationCalculationRelationship(string calculationIdA, string calculationIdB);
        Task DeleteCalculationSpecificationRelationship(string calculationId, string specificationId);
        Task DeleteCalculationCalculationRelationship(string calculationIdA, string calculationIdB);

    }
}
