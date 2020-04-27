using CalculateFunding.Common.Graph;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Interfaces;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Graph
{
    public class CalculationRepository : GraphRepositoryBase, ICalculationRepository
    {
        private const string DataFieldId = DataField.IdField;
        
        public const string CalculationId = "calculationid";
        public const string SpecificationId = "specificationid";
        public const string CalculationSpecificationRelationship = "BelongsToSpecification";
        public const string SpecificationCalculationRelationship = "HasCalculation";
        public const string CalculationACalculationBRelationship = "CallsCalculation";
        public const string CalculationBCalculationARelationship = "CalledByCalculation";
        public const string CalculationDataFieldRelationship = "ReferencesDataField";
        public const string DataFieldCalculationRelationship = "IsReferencedInCalculation";

        public CalculationRepository(IGraphRepository graphRepository)
            : base(graphRepository)
        {
        }

        public async Task DeleteCalculation(string calculationId)
        {
            await DeleteNode<Calculation>(CalculationId, calculationId);
        }

        public async Task UpsertCalculations(IEnumerable<Calculation> calculations)
        {
            await UpsertNodes(calculations, CalculationId);
        }

        public async Task UpsertCalculationSpecificationRelationship(string calculationId, string specificationId)
        {
            await UpsertRelationship<Calculation, Specification>(CalculationSpecificationRelationship, 
                (CalculationId, calculationId), 
                (SpecificationId, specificationId));

            await UpsertRelationship<Specification, Calculation>(SpecificationCalculationRelationship, 
                (SpecificationId, specificationId),
                (CalculationId, calculationId));
        }

        public async Task<IEnumerable<Entity<Calculation, IRelationship>>> GetCalculationCircularDependencies(string specificationId)
        {
            IEnumerable<Entity<Calculation>> entities = await GetCircularDependencies<Calculation>(CalculationACalculationBRelationship,
                SpecificationId,
                specificationId);
            return entities.Select(_ => new Entity<Calculation, IRelationship> { Node = _.Node, Relationships = _.Relationships });
        }

        public async Task UpsertCalculationCalculationRelationship(string calculationIdA, string calculationIdB)
        {
            await UpsertRelationship<Calculation, Calculation>(CalculationACalculationBRelationship, 
                (CalculationId, calculationIdA),
                (CalculationId, calculationIdB));

            await UpsertRelationship<Calculation, Calculation>(CalculationBCalculationARelationship,
                (CalculationId, calculationIdB),
                (CalculationId, calculationIdA));
        }

        public async Task DeleteCalculationSpecificationRelationship(string calculationId, string specificationId)
        {
            await DeleteRelationship<Calculation, Specification>(CalculationSpecificationRelationship, 
                (CalculationId, calculationId), 
                (SpecificationId, specificationId));

            await DeleteRelationship<Specification, Calculation>(SpecificationCalculationRelationship,
                (SpecificationId, specificationId),
                (CalculationId, calculationId));
        }

        public async Task DeleteCalculationCalculationRelationship(string calculationIdA, string calculationIdB)
        {
            await DeleteRelationship<Calculation, Calculation>(CalculationACalculationBRelationship, 
                (CalculationId, calculationIdA), 
                (CalculationId, calculationIdB));

            await DeleteRelationship<Calculation, Calculation>(CalculationBCalculationARelationship,
                (CalculationId, calculationIdB),
                (CalculationId, calculationIdA));
        }

        public async Task UpsertCalculationDataFieldRelationship(string calculationId, string dataFieldId)
        {
            await UpsertRelationship<Calculation, DataField>(CalculationDataFieldRelationship,
                (CalculationId, calculationId),
                (DataFieldId, dataFieldId));

            await UpsertRelationship<DataField, Calculation>(DataFieldCalculationRelationship,
                (DataFieldId, dataFieldId),
                (CalculationId, calculationId));
        }

        public async Task DeleteCalculationDataFieldRelationship(string calculationId,
            string datasetFieldId)
        {
            await DeleteRelationship<Calculation, DataField>(CalculationDataFieldRelationship,
                (CalculationId, calculationId),
                (DataFieldId, datasetFieldId));

            await DeleteRelationship<DataField, Calculation>(DataFieldCalculationRelationship,
                (DataFieldId, datasetFieldId),
                (CalculationId, calculationId));
        }

        public async Task<IEnumerable<Entity<Calculation, IRelationship>>> GetAllEntities(string calculationId)
        {
            IEnumerable<Entity<Calculation>> entities = await GetAllEntities<Calculation>(CalculationId,
                calculationId,
                new[] { 
                    DataFieldCalculationRelationship,
                    CalculationDataFieldRelationship
                });
            return entities.Select(_ => new Entity<Calculation, IRelationship> { Node = _.Node, Relationships = _.Relationships });
        }
    }
}
