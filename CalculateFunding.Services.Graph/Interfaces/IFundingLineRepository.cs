using CalculateFunding.Models.Graph;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Graph.Interfaces;

namespace CalculateFunding.Services.Graph.Interfaces
{
    public interface IFundingLineRepository
    {
        Task DeleteFundingLine(string fundingLineId);

        Task UpsertFundingLines(IEnumerable<FundingLine> fundingLines);

        Task UpsertFundingLineCalculationRelationship(string fundingLineId, string calculationId);

        Task UpsertCalculationFundingLineRelationship(string calculationId, string fundingLineId);

        Task DeleteFundingLineCalculationRelationship(string fundingLineId, string calculationId);

        Task DeleteCalculationFundingLineRelationship(string calculationId, string fundingLineId);

        Task<IEnumerable<Entity<FundingLine, IRelationship>>> GetAllEntities(string fundingLineId);
    }
}
