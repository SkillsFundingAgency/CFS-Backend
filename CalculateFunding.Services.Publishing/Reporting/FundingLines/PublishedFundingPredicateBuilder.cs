using System;
using System.Collections.Generic;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedFundingPredicateBuilder : IPublishedFundingPredicateBuilder
    {
        private static readonly IDictionary<FundingLineCsvGeneratorJobType, string> Predicates = new Dictionary<FundingLineCsvGeneratorJobType, string>
        {
            {FundingLineCsvGeneratorJobType.CurrentState, "1 = 1"},
            {FundingLineCsvGeneratorJobType.Released, "NOT IS_NULL(c.content.released)"},
            {FundingLineCsvGeneratorJobType.CurrentProfileValues, "1 = 1" },
            {FundingLineCsvGeneratorJobType.HistoryProfileValues, "1 = 1" },
            {FundingLineCsvGeneratorJobType.History, "1 = 1"},
            {FundingLineCsvGeneratorJobType.PublishedProviderStateSummary, "1 = 1" }
        };

        private static readonly IDictionary<FundingLineCsvGeneratorJobType, string> Joins = new Dictionary<FundingLineCsvGeneratorJobType, string>
        {
            {FundingLineCsvGeneratorJobType.CurrentProfileValues, "WHERE NOT IS_NULL(fundingLine['value']) and fundingLine.name = @fundingLineName"},
            {FundingLineCsvGeneratorJobType.HistoryProfileValues, "WHERE NOT IS_NULL(fundingLine['value']) and fundingLine.name = @fundingLineName"}
        };

        public string BuildJoinPredicate(FundingLineCsvGeneratorJobType jobType)
        {
            return Joins.TryGetValue(jobType, out string join) ? join : string.Empty;
        }

        public string BuildPredicate(FundingLineCsvGeneratorJobType jobType)
        {
            return Predicates.TryGetValue(jobType, out string predicate) ? predicate : throw new ArgumentOutOfRangeException();
        }
    }
}