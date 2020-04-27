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
        Task UpsertCalculationDataFieldRelationship(string calculationId, string datasetFieldId);
        Task DeleteCalculationDataFieldRelationship(string calculationId, string datasetFieldId);
        Task DeleteCalculationSpecificationRelationship(string calculationId, string specificationId);
        Task DeleteCalculationCalculationRelationship(string calculationIdA, string calculationIdB);

        Task<IEnumerable<Entity<Calculation, IRelationship>>> GetCalculationCircularDependencies(string specificationId);

        Task<IEnumerable<Entity<Calculation, IRelationship>>> GetAllEntities(string calculationId);
    }
}
