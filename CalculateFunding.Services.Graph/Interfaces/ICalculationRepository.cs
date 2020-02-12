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

        Task CreateCalculationRelationship(string calculationId, string specificationId);
    }
}
