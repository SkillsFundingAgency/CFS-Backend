using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph;
using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Graph.Constants;
using CalculateFunding.Services.Graph.Interfaces;

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

        public async Task DeleteCalculations(params string[] calculationIds)
        {
            await DeleteNodes<Calculation>(calculationIds.Select(_ => (AttributeConstants.CalculationId, _)).ToArray());
        }

        public async Task UpsertCalculations(IEnumerable<Calculation> calculations)
        {
            await UpsertNodes(calculations, AttributeConstants.CalculationId);
        }

        public async Task UpsertCalculationSpecificationRelationship(string calculationId,
            string specificationId)
        {
            await UpsertRelationship<Calculation, Specification>(AttributeConstants.CalculationSpecificationRelationshipId,
                (AttributeConstants.CalculationId, calculationId),
                (AttributeConstants.SpecificationId, specificationId));

            await UpsertRelationship<Specification, Calculation>(AttributeConstants.SpecificationCalculationRelationshipId,
                (AttributeConstants.SpecificationId, specificationId),
                (AttributeConstants.CalculationId, calculationId));
        }

        public async Task UpsertCalculationSpecificationRelationships(params (string calculationId, string specificationId)[] relationships)
        {
            await UpsertRelationships<Calculation, Specification>(relationships.Select(_ => (AttributeConstants.CalculationSpecificationRelationshipId,
                    (AttributeConstants.CalculationId, _.calculationId),
                    (AttributeConstants.SpecificationId, _.specificationId)))
                .ToArray());

            await UpsertRelationships<Specification, Calculation>(relationships.Select(_ => (AttributeConstants.SpecificationCalculationRelationshipId,
                    (AttributeConstants.SpecificationId, _.specificationId),
                    (AttributeConstants.CalculationId, _.calculationId)))
                .ToArray());
        }

        public async Task<IEnumerable<Entity<Calculation, IRelationship>>> GetCalculationCircularDependencies(string calculationId)
        {
            IEnumerable<Entity<Calculation>> entities = await GetCircularDependencies<Calculation>(AttributeConstants.CalculationACalculationBRelationship,
                AttributeConstants.CalculationId,
                calculationId);
            return entities?.Select(_ => new Entity<Calculation, IRelationship>
            {
                Node = _.Node,
                Relationships = _.Relationships
            });
        }

        public async Task<IEnumerable<Entity<Calculation, IRelationship>>> GetCalculationCircularDependenciesBySpecificationId(string specificationId)
        {
            IEnumerable<Entity<Calculation>> entities = await GetCircularDependencies<Calculation>(AttributeConstants.CalculationACalculationBRelationship,
                AttributeConstants.SpecificationId,
                specificationId);
            return entities?.Select(_ => new Entity<Calculation, IRelationship>
            {
                Node = _.Node,
                Relationships = _.Relationships
            });
        }

        public async Task UpsertCalculationCalculationRelationship(string calculationIdA,
            string calculationIdB)
        {
            await UpsertRelationship<Calculation, Calculation>(AttributeConstants.CalculationACalculationBRelationship,
                (AttributeConstants.CalculationId, calculationIdA),
                (AttributeConstants.CalculationId, calculationIdB));

            await UpsertRelationship<Calculation, Calculation>(AttributeConstants.CalculationBCalculationARelationship,
                (AttributeConstants.CalculationId, calculationIdB),
                (AttributeConstants.CalculationId, calculationIdA));
        }

        public async Task UpsertCalculationCalculationRelationships(params (string calculationIdA, string calculationIdB)[] relationships)
        {
            await UpsertRelationships<Calculation, Calculation>(relationships.Select(_ => (AttributeConstants.CalculationACalculationBRelationship,
                    (AttributeConstants.CalculationId, _.calculationIdA),
                    (AttributeConstants.CalculationId, _.calculationIdB)))
                .ToArray());

            await UpsertRelationships<Calculation, Calculation>(relationships.Select(_ => (AttributeConstants.CalculationBCalculationARelationship,
                    (AttributeConstants.CalculationId, _.calculationIdB),
                    (AttributeConstants.CalculationId, _.calculationIdA)))
                .ToArray());
        }

        public async Task DeleteCalculationSpecificationRelationship(string calculationId,
            string specificationId)
        {
            await DeleteRelationship<Calculation, Specification>(AttributeConstants.CalculationSpecificationRelationshipId,
                (AttributeConstants.CalculationId, calculationId),
                (AttributeConstants.SpecificationId, specificationId));

            await DeleteRelationship<Specification, Calculation>(AttributeConstants.SpecificationCalculationRelationshipId,
                (AttributeConstants.SpecificationId, specificationId),
                (AttributeConstants.CalculationId, calculationId));
        }

        public async Task DeleteCalculationSpecificationRelationships(params (string calculationId, string specificationId)[] relationships)
        {
            await DeleteRelationships<Calculation, Specification>(relationships.Select(_ => (AttributeConstants.CalculationSpecificationRelationshipId,
                    (AttributeConstants.CalculationId, _.calculationId),
                    (AttributeConstants.SpecificationId, _.specificationId)))
                .ToArray());

            await DeleteRelationships<Specification, Calculation>(relationships.Select(_ => (AttributeConstants.SpecificationCalculationRelationshipId,
                    (AttributeConstants.SpecificationId, _.specificationId),
                    (AttributeConstants.CalculationId, _.calculationId)))
                .ToArray());
        }

        public async Task DeleteCalculationCalculationRelationship(string calculationIdA,
            string calculationIdB)
        {
            await DeleteRelationship<Calculation, Calculation>(AttributeConstants.CalculationACalculationBRelationship,
                (AttributeConstants.CalculationId, calculationIdA),
                (AttributeConstants.CalculationId, calculationIdB));

            await DeleteRelationship<Calculation, Calculation>(AttributeConstants.CalculationBCalculationARelationship,
                (AttributeConstants.CalculationId, calculationIdB),
                (AttributeConstants.CalculationId, calculationIdA));
        }
        
        public async Task DeleteCalculationCalculationRelationships(params (string calculationIdA,
            string calculationIdB)[] relationships)
        {
            await DeleteRelationships<Calculation, Calculation>(relationships.Select(_ => (AttributeConstants.CalculationACalculationBRelationship,
                (AttributeConstants.CalculationId, _.calculationIdA),
                (AttributeConstants.CalculationId, _.calculationIdB)))
                .ToArray());

            await DeleteRelationships<Calculation, Calculation>(relationships.Select(_ => (AttributeConstants.CalculationBCalculationARelationship,
                (AttributeConstants.CalculationId, _.calculationIdB),
                (AttributeConstants.CalculationId, _.calculationIdA)))
                .ToArray());
        }

        public async Task UpsertCalculationDataFieldRelationship(string calculationId,
            string dataFieldId)
        {
            await UpsertRelationship<Calculation, DataField>(AttributeConstants.CalculationDataFieldRelationshipId,
                (AttributeConstants.CalculationId, calculationId),
                (AttributeConstants.DataFieldId, dataFieldId));

            await UpsertRelationship<DataField, Calculation>(AttributeConstants.DataFieldCalculationRelationship,
                (AttributeConstants.DataFieldId, dataFieldId),
                (AttributeConstants.CalculationId, calculationId));
        }

        public async Task UpsertCalculationDataFieldRelationships(params (string calculationId, string dataFieldId)[] relationships)
        {
            await UpsertRelationships<Calculation, DataField>(relationships.Select(_ => (AttributeConstants.CalculationDataFieldRelationshipId,
                    (AttributeConstants.CalculationId, _.calculationId),
                    (AttributeConstants.DataFieldId, _.dataFieldId)))
                .ToArray());

            await UpsertRelationships<DataField, Calculation>(relationships.Select(_ => (AttributeConstants.DataFieldCalculationRelationship,
                    (AttributeConstants.DataFieldId, _.dataFieldId),
                    (AttributeConstants.CalculationId, _.calculationId)))
                .ToArray());
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

        public async Task DeleteCalculationDataFieldRelationships(params (string calculationId,
            string datasetFieldId)[] relationships)
        {
            await DeleteRelationships<Calculation, DataField>(relationships.Select(_ => (AttributeConstants.CalculationDataFieldRelationshipId,
                    (AttributeConstants.CalculationId, _.calculationId),
                    (AttributeConstants.DataFieldId, _.datasetFieldId)))
                .ToArray());

            await DeleteRelationships<DataField, Calculation>(relationships.Select(_ => (AttributeConstants.DataFieldCalculationRelationship,
                    (AttributeConstants.DataFieldId, _.datasetFieldId),
                    (AttributeConstants.CalculationId, _.calculationId)))
                .ToArray());
        }

        public async Task<IEnumerable<Entity<Calculation, IRelationship>>> GetAllEntitiesForAll(params string[] calculationIds)
        {
            IEnumerable<Entity<Calculation>> entities = await GetAllEntitiesForAll<Calculation>(AttributeConstants.CalculationId,
                calculationIds,
                new[]
                {
                    AttributeConstants.DataFieldCalculationRelationship,
                    AttributeConstants.CalculationDataFieldRelationshipId,
                    AttributeConstants.CalculationACalculationBRelationship,
                    AttributeConstants.CalculationBCalculationARelationship
                });
            return entities.Select(_ => new Entity<Calculation, IRelationship>
            {
                Node = _.Node,
                Relationships = _.Relationships
            });
        }

        public async Task<IEnumerable<Entity<Calculation, IRelationship>>> GetAllEntities(string calculationId)
        {
            IEnumerable<Entity<Calculation>> entities = await GetAllEntities<Calculation>(AttributeConstants.CalculationId,
                calculationId,
                new[]
                {
                    AttributeConstants.DataFieldCalculationRelationship,
                    AttributeConstants.CalculationDataFieldRelationshipId,
                    AttributeConstants.CalculationACalculationBRelationship,
                    AttributeConstants.CalculationBCalculationARelationship
                });
            return entities.Select(_ => new Entity<Calculation, IRelationship>
            {
                Node = _.Node,
                Relationships = _.Relationships
            });
        }
    }
}