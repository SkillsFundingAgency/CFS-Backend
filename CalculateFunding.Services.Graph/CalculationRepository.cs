using CalculateFunding.Common.Graph;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Constants;
using CalculateFunding.Services.Graph.Interfaces;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Graph
{
    public class CalculationRepository : GraphRepositoryBase, ICalculationRepository
    {
        public CalculationRepository(IGraphRepository graphRepository)
            : base(graphRepository)
        {
        }

        public async Task DeleteCalculation(string calculationId)
        {
            await DeleteNode<Calculation>(AttributeConstants.CalculationId, calculationId);
        }

        public async Task UpsertCalculations(IEnumerable<Calculation> calculations)
        {
            await UpsertNodes(calculations, AttributeConstants.CalculationId);
        }

        public async Task UpsertCalculationSpecificationRelationship(string calculationId, string specificationId)
        {
            await UpsertRelationship<Calculation, Specification>(AttributeConstants.CalculationSpecificationRelationshipId, 
                (AttributeConstants.CalculationId, calculationId), 
                (AttributeConstants.SpecificationId, specificationId));

            await UpsertRelationship<Specification, Calculation>(AttributeConstants.SpecificationCalculationRelationshipId, 
                (AttributeConstants.SpecificationId, specificationId),
                (AttributeConstants.CalculationId, calculationId));
        }

        public async Task<IEnumerable<Entity<Calculation, IRelationship>>> GetCalculationCircularDependencies(string calculationId)
        {
            IEnumerable<Entity<Calculation>> entities = await GetCircularDependencies<Calculation>(AttributeConstants.CalculationACalculationBRelationship,
                AttributeConstants.CalculationId,
                calculationId);
            return entities?.Select(_ => new Entity<Calculation, IRelationship> { Node = _.Node, Relationships = _.Relationships });
        }
        
        public async Task<IEnumerable<Entity<Calculation, IRelationship>>> GetCalculationCircularDependenciesBySpecificationId(string specificationId)
        {
            IEnumerable<Entity<Calculation>> entities = await GetCircularDependencies<Calculation>(AttributeConstants.CalculationACalculationBRelationship,
                AttributeConstants.SpecificationId,
                specificationId);
            return entities?.Select(_ => new Entity<Calculation, IRelationship> { Node = _.Node, Relationships = _.Relationships });
        }

        public async Task UpsertCalculationCalculationRelationship(string calculationIdA, string calculationIdB)
        {
            await UpsertRelationship<Calculation, Calculation>(AttributeConstants.CalculationACalculationBRelationship, 
                (AttributeConstants.CalculationId, calculationIdA),
                (AttributeConstants.CalculationId, calculationIdB));

            await UpsertRelationship<Calculation, Calculation>(AttributeConstants.CalculationBCalculationARelationship,
                (AttributeConstants.CalculationId, calculationIdB),
                (AttributeConstants.CalculationId, calculationIdA));
        }

        public async Task DeleteCalculationSpecificationRelationship(string calculationId, string specificationId)
        {
            await DeleteRelationship<Calculation, Specification>(AttributeConstants.CalculationSpecificationRelationshipId, 
                (AttributeConstants.CalculationId, calculationId), 
                (AttributeConstants.SpecificationId, specificationId));

            await DeleteRelationship<Specification, Calculation>(AttributeConstants.SpecificationCalculationRelationshipId,
                (AttributeConstants.SpecificationId, specificationId),
                (AttributeConstants.CalculationId, calculationId));
        }

        public async Task DeleteCalculationCalculationRelationship(string calculationIdA, string calculationIdB)
        {
            await DeleteRelationship<Calculation, Calculation>(AttributeConstants.CalculationACalculationBRelationship, 
                (AttributeConstants.CalculationId, calculationIdA), 
                (AttributeConstants.CalculationId, calculationIdB));

            await DeleteRelationship<Calculation, Calculation>(AttributeConstants.CalculationBCalculationARelationship,
                (AttributeConstants.CalculationId, calculationIdB),
                (AttributeConstants.CalculationId, calculationIdA));
        }

        public async Task UpsertCalculationDataFieldRelationship(string calculationId, string dataFieldId)
        {
            await UpsertRelationship<Calculation, DataField>(AttributeConstants.CalculationDataFieldRelationshipId,
                (AttributeConstants.CalculationId, calculationId),
                (AttributeConstants.DataFieldId, dataFieldId));

            await UpsertRelationship<DataField, Calculation>(AttributeConstants.DataFieldCalculationRelationship,
                (AttributeConstants.DataFieldId, dataFieldId),
                (AttributeConstants.CalculationId, calculationId));
        }

        public async Task DeleteCalculationDataFieldRelationship(string calculationId,
            string datasetFieldId)
        {
            await DeleteRelationship<Calculation, DataField>(AttributeConstants.CalculationDataFieldRelationshipId,
                (AttributeConstants.CalculationId, calculationId),
                (AttributeConstants.DataFieldId, datasetFieldId));

            await DeleteRelationship<DataField, Calculation>(AttributeConstants.DataFieldCalculationRelationship,
                (AttributeConstants.DataFieldId, datasetFieldId),
                (AttributeConstants.CalculationId, calculationId));
        }

        public async Task<IEnumerable<Entity<Calculation, IRelationship>>> GetAllEntities(string calculationId)
        {
            IEnumerable<Entity<Calculation>> entities = await GetAllEntities<Calculation>(AttributeConstants.CalculationId,
                calculationId,
                new[] {
                    AttributeConstants.DataFieldCalculationRelationship,
                    AttributeConstants.CalculationDataFieldRelationshipId,
                    AttributeConstants.CalculationACalculationBRelationship,
                    AttributeConstants.CalculationBCalculationARelationship
                });
            return entities.Select(_ => new Entity<Calculation, IRelationship> { Node = _.Node, Relationships = _.Relationships });
        }
    }
}
