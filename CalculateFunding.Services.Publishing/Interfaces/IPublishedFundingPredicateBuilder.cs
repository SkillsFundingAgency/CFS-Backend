using CalculateFunding.Services.Publishing.Reporting.FundingLines;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingPredicateBuilder
    {
        string BuildPredicate(FundingLineCsvGeneratorJobType jobType);

        string BuildJoinPredicate(FundingLineCsvGeneratorJobType jobType);
    }
}