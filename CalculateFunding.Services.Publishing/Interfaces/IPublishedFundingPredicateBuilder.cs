using CalculateFunding.Services.Publishing.Reporting;

namespace CalculateFunding.Services.Publishing.Interfaces
{
    public interface IPublishedFundingPredicateBuilder
    {
        string BuildPredicate(FundingLineCsvGeneratorJobType jobType);
    }
}