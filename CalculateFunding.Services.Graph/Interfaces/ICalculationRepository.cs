using CalculateFunding.Common.Graph;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Models.Graph;
using Newtonsoft.Json.Linq;
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

        Task CreateCalculationDataFieldRelationship(string calculationId,
            string dataFieldId);

        Task DeleteCalculationDataFieldRelationship(string calculationId,
            string dataFieldId);

        Task<IEnumerable<Entity<Calculation, IRelationship>>> GetCalculationCircularDependencies(string specificationId);
    }
}
