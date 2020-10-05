using CalculateFunding.Common.Graph.Interfaces;
using CalculateFunding.Common.Graph;
using CalculateFunding.Models.Graph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                new[] {
                    AttributeConstants.FundingLineCalculationRelationshipId,
                    AttributeConstants.CalculationFundingLineRelationshipId
                });

            IEnumerable<IRelationship> relationships = entities.SelectMany(_ => _.Relationships);

            foreach (IRelationship relationship in relationships)
            {
                if (relationship.Type == AttributeConstants.FundingLineCalculationRelationshipId.ToLower())
                {
                    await DeleteRelationship<FundingLine, Calculation>(AttributeConstants.FundingLineCalculationRelationshipId, (AttributeConstants.FundingLineId, (relationship.One[AttributeConstants.FundingLineId.ToLower()])), (AttributeConstants.CalculationId, relationship.Two[AttributeConstants.CalculationId.ToLower()]));
                }

                if (relationship.Type == AttributeConstants.CalculationFundingLineRelationshipId.ToLower())
                {
                    await DeleteRelationship<Calculation, FundingLine>(AttributeConstants.CalculationFundingLineRelationshipId, (AttributeConstants.CalculationId, (relationship.One[AttributeConstants.CalculationId.ToLower()])), (AttributeConstants.FundingLineId, relationship.Two[AttributeConstants.FundingLineId.ToLower()]));
                }
            }

            await DeleteNode<FundingLine>(AttributeConstants.FundingLineId, fundingLineId);
        }

        public async Task UpsertFundingLines(IEnumerable<FundingLine> fundingLines)
        {
            await UpsertNodes(fundingLines, AttributeConstants.FundingLineId);
        }

        public async Task UpsertFundingLineCalculationRelationship(string fundingLineId, string calculationId)
        {
            await UpsertRelationship<FundingLine, Calculation>(AttributeConstants.FundingLineCalculationRelationshipId,
                (AttributeConstants.FundingLineId, fundingLineId),
                (AttributeConstants.CalculationId, calculationId));
        }

        public async Task UpsertCalculationFundingLineRelationship(string calculationId, string fundingLineId)
        {
            await UpsertRelationship<Calculation, FundingLine>(AttributeConstants.CalculationFundingLineRelationshipId,
                (AttributeConstants.CalculationId, calculationId),
                (AttributeConstants.FundingLineId, fundingLineId));
        }

        public async Task DeleteFundingLineCalculationRelationship(string fundingLineId, string calculationId)
        {
            await DeleteRelationship<FundingLine, Calculation>(AttributeConstants.FundingLineCalculationRelationshipId,
                (AttributeConstants.FundingLineId, fundingLineId),
                (AttributeConstants.CalculationId, calculationId));
        }

        public async Task DeleteCalculationFundingLineRelationship(string calculationId, string fundingLineId)
        {
            await DeleteRelationship<Calculation, FundingLine>(AttributeConstants.CalculationFundingLineRelationshipId,
                (AttributeConstants.CalculationId, calculationId),
                (AttributeConstants.FundingLineId, fundingLineId));
        }

        public async Task<IEnumerable<Entity<FundingLine, IRelationship>>> GetAllEntities(string fundingLineId)
        {
            IEnumerable<Entity<FundingLine>> entities = await GetAllEntities<FundingLine>(AttributeConstants.FundingLineId,
                fundingLineId,
                new[] {
                    AttributeConstants.FundingLineCalculationRelationshipId,
                    AttributeConstants.CalculationFundingLineRelationshipId
                });
            return entities.Select(_ => new Entity<FundingLine, IRelationship> { Node = _.Node, Relationships = _.Relationships });
        }
    }
}
