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
    public class FundingLineRepository : GraphRepositoryBase, IFundingLineRepository
    {
        public FundingLineRepository(IGraphRepository graphRepository)
            : base(graphRepository)
        {
        }

        public async Task DeleteFundingLine(string fundingLineId)
        {
            IEnumerable<Entity<FundingLine>> entities = await GetAllEntities<FundingLine>(AttributeConstants.FundingLineId,
                fundingLineId,
                new[]
                {
                    AttributeConstants.FundingLineCalculationRelationshipId,
                    AttributeConstants.CalculationFundingLineRelationshipId
                });

            IEnumerable<IRelationship> relationships = entities.SelectMany(_ => _.Relationships);

            await DeleteRelationships<FundingLine, Calculation>(relationships.Select(_ => (AttributeConstants.FundingLineCalculationRelationshipId,
                    (AttributeConstants.FundingLineId, (string) _.One[AttributeConstants.FundingLineId.ToLower()]),
                    (AttributeConstants.CalculationId, (string) _.Two[AttributeConstants.CalculationId.ToLower()])))
                .ToArray());

            await DeleteRelationships<Calculation, FundingLine>(relationships.Select(_ => (AttributeConstants.CalculationFundingLineRelationshipId,
                    (AttributeConstants.CalculationId, (string) _.Two[AttributeConstants.CalculationId.ToLower()]),
                    (AttributeConstants.FundingLineId, (string) _.One[AttributeConstants.FundingLineId.ToLower()])))
                .ToArray());

            await DeleteNode<FundingLine>(AttributeConstants.FundingLineId, fundingLineId);
        }

        public async Task DeleteFundingLines(IEnumerable<string> fundingLineIds)
        {
            await DeleteNodes<FundingLine>(fundingLineIds.Select(_ => (AttributeConstants.FundingLineId, _)).ToArray());
        }

        public async Task UpsertFundingLines(IEnumerable<FundingLine> fundingLines)
        {
            await UpsertNodes(fundingLines, AttributeConstants.FundingLineId);
        }

        public async Task UpsertFundingLineCalculationRelationship(string fundingLineId,
            string calculationId)
        {
            await UpsertRelationship<FundingLine, Calculation>(AttributeConstants.FundingLineCalculationRelationshipId,
                (AttributeConstants.FundingLineId, fundingLineId),
                (AttributeConstants.CalculationId, calculationId));
        }

        public async Task UpsertFundingLineCalculationRelationships(params (string fundingLineId, string calculationId)[] relationships)
        {
            await UpsertRelationships<FundingLine, Calculation>(relationships.Select(
                    _ => (AttributeConstants.FundingLineCalculationRelationshipId,
                        (AttributeConstants.FundingLineId, _.fundingLineId),
                        (AttributeConstants.CalculationId, _.calculationId)))
                .ToArray());
        }

        public async Task UpsertCalculationFundingLineRelationship(string calculationId,
            string fundingLineId)
        {
            await UpsertRelationship<Calculation, FundingLine>(AttributeConstants.CalculationFundingLineRelationshipId,
                (AttributeConstants.CalculationId, calculationId),
                (AttributeConstants.FundingLineId, fundingLineId));
        }

        public async Task UpsertCalculationFundingLineRelationships(params (string calculationId, string fundingLineId)[] relationships)
        {
            await UpsertRelationships<Calculation, FundingLine>(relationships.Select(_ => (AttributeConstants.CalculationFundingLineRelationshipId,
                    (AttributeConstants.CalculationId, _.calculationId),
                    (AttributeConstants.FundingLineId, _.fundingLineId)))
                .ToArray());
        }

        public async Task DeleteFundingLineCalculationRelationship(string fundingLineId,
            string calculationId)
        {
            await DeleteRelationship<FundingLine, Calculation>(AttributeConstants.FundingLineCalculationRelationshipId,
                (AttributeConstants.FundingLineId, fundingLineId),
                (AttributeConstants.CalculationId, calculationId));
        }

        public async Task DeleteFundingLineCalculationRelationships(params (string fundingLineId, string calculationId)[] relationships)
        {
            await DeleteRelationships<FundingLine, Calculation>(relationships.Select(_ => (AttributeConstants.FundingLineCalculationRelationshipId,
                    (AttributeConstants.FundingLineId, _.fundingLineId),
                    (AttributeConstants.CalculationId, _.calculationId)))
                .ToArray());
        }

        public async Task DeleteCalculationFundingLineRelationship(string calculationId,
            string fundingLineId)
        {
            await DeleteRelationship<Calculation, FundingLine>(AttributeConstants.CalculationFundingLineRelationshipId,
                (AttributeConstants.CalculationId, calculationId),
                (AttributeConstants.FundingLineId, fundingLineId));
        }

        public async Task DeleteCalculationFundingLineRelationships(params (string calculationId, string fundingLineId)[] relationships)
        {
            await DeleteRelationships<Calculation, FundingLine>(relationships.Select(_ => (AttributeConstants.CalculationFundingLineRelationshipId,
                    (AttributeConstants.CalculationId, _.calculationId),
                    (AttributeConstants.FundingLineId, _.fundingLineId)))
                .ToArray());
        }
        
        public async Task<IEnumerable<Entity<FundingLine, IRelationship>>> GetAllEntitiesForAll(params string[] fundingLineIds)
        {
            IEnumerable<Entity<FundingLine>> entities = await GetAllEntitiesForAll<FundingLine>(AttributeConstants.FundingLineId,
                fundingLineIds,
                new[]
                {
                    AttributeConstants.FundingLineCalculationRelationshipId,
                    AttributeConstants.CalculationFundingLineRelationshipId
                });
            return entities.Select(_ => new Entity<FundingLine, IRelationship>
            {
                Node = _.Node,
                Relationships = _.Relationships
            });
        }

        public async Task<IEnumerable<Entity<FundingLine, IRelationship>>> GetAllEntities(string fundingLineId)
        {
            IEnumerable<Entity<FundingLine>> entities = await GetAllEntities<FundingLine>(AttributeConstants.FundingLineId,
                fundingLineId,
                new[]
                {
                    AttributeConstants.FundingLineCalculationRelationshipId,
                    AttributeConstants.CalculationFundingLineRelationshipId
                });
            return entities.Select(_ => new Entity<FundingLine, IRelationship>
            {
                Node = _.Node,
                Relationships = _.Relationships
            });
        }
    }
}