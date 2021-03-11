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
    public class EnumRepository : GraphRepositoryBase, IEnumRepository
    {
        public EnumRepository(IGraphRepository graphRepository)
            : base(graphRepository)
        {
        }

        public async Task DeleteEnum(string enumName)
        {
            await DeleteNode<Enum>(AttributeConstants.EnumId, enumName);
        }

        public async Task DeleteEnums(IEnumerable<string> enumNames)
        {
            await DeleteNodes<Enum>(enumNames.Select(_ => (AttributeConstants.EnumId, _)).ToArray());
        }

        public async Task UpsertEnums(IEnumerable<Enum> enums)
        {
            await UpsertNodes(enums, AttributeConstants.EnumId);
        }

        public async Task UpsertCalculationEnumRelationship(string calculationId,
            string enumNameValue)
        {
            await UpsertRelationship<Calculation, Enum>(AttributeConstants.CalculationEnumRelationshipId,
                (AttributeConstants.CalculationId, calculationId),
                (AttributeConstants.EnumId, enumNameValue));

            await UpsertRelationship<Enum, Calculation>(AttributeConstants.EnumCalculationRelationshipId,
                (AttributeConstants.EnumId, enumNameValue),
                (AttributeConstants.CalculationId, calculationId));
        }

        public async Task UpsertCalculationEnumRelationships(params (string calculationId, string enumId)[] relationships)
        {
            await UpsertRelationships<Calculation, Enum>(relationships.Select(_ => (AttributeConstants.CalculationEnumRelationshipId,
                    (AttributeConstants.CalculationId, _.calculationId),
                    (AttributeConstants.EnumId, _.enumId)))
                .ToArray());

            await UpsertRelationships<Enum, Calculation>(relationships.Select(_ => (AttributeConstants.EnumCalculationRelationshipId,
                    (AttributeConstants.EnumId, _.enumId),
                    (AttributeConstants.CalculationId, _.calculationId)))
                .ToArray());
        }

        public async Task DeleteCalculationEnumRelationship(string calculationId,
            string enumName)
        {
            await DeleteRelationship<Calculation, FundingLine>(AttributeConstants.CalculationEnumRelationshipId,
                (AttributeConstants.CalculationId, calculationId),
                (AttributeConstants.EnumId, enumName));
        }

        public async Task DeleteCalculationEnumRelationships(params (string calculationId, string enumNameValue)[] relationships)
        {
            await DeleteRelationships<Calculation, FundingLine>(relationships.Select(_ => (AttributeConstants.CalculationEnumRelationshipId,
                    (AttributeConstants.CalculationId, _.calculationId),
                    (AttributeConstants.EnumId, _.enumNameValue)))
                .ToArray());
        }
        
        public async Task<IEnumerable<Entity<Enum, IRelationship>>> GetAllEntitiesForAll(params string[] enumNameValues)
        {
            IEnumerable<Entity<Enum>> entities = await GetAllEntitiesForAll<Enum>(AttributeConstants.EnumId,
                enumNameValues,
                new[]
                {
                    AttributeConstants.CalculationEnumRelationshipId,
                    AttributeConstants.EnumCalculationRelationshipId
                });
            return entities.Select(_ => new Entity<Enum, IRelationship>
            {
                Node = _.Node,
                Relationships = _.Relationships
            });
        }

        public async Task<IEnumerable<Entity<Enum, IRelationship>>> GetAllEntities(string enumNameValue)
        {
            IEnumerable<Entity<Enum>> entities = await GetAllEntities<Enum>(AttributeConstants.EnumId,
                enumNameValue,
                new[]
                {
                    AttributeConstants.CalculationEnumRelationshipId,
                    AttributeConstants.EnumCalculationRelationshipId
                });
            return entities.Select(_ => new Entity<Enum, IRelationship>
            {
                Node = _.Node,
                Relationships = _.Relationships
            });
        }
    }
}