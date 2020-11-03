using CalculateFunding.Models.Graph;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph.Interfaces;

namespace CalculateFunding.Services.Graph.Interfaces
{
    public interface ICalculationRepository
    {
        Task DeleteCalculation(string calculationId);

        Task UpsertCalculations(IEnumerable<Calculation> calculations);        
        Task UpsertCalculationSpecificationRelationship(string calculationId, string specificationId);
        Task UpsertCalculationCalculationRelationship(string calculationIdA, string calculationIdB);
        Task UpsertCalculationDataFieldRelationship(string calculationId, string datasetFieldId);
        Task DeleteCalculationDataFieldRelationship(string calculationId, string datasetFieldId);
        Task DeleteCalculationSpecificationRelationship(string calculationId, string specificationId);
        Task DeleteCalculationCalculationRelationship(string calculationIdA, string calculationIdB);

        Task<IEnumerable<Entity<Calculation, IRelationship>>> GetCalculationCircularDependencies(string calculationId);

        Task<IEnumerable<Entity<Calculation, IRelationship>>> GetAllEntities(string calculationId);
        Task<IEnumerable<Entity<Calculation, IRelationship>>> GetCalculationCircularDependenciesBySpecificationId(string specificationId);
    }
}
