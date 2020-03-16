using System;
using System.Collections.Generic;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Reporting.FundingLines
{
    public class PublishedFundingPredicateBuilder : IPublishedFundingPredicateBuilder
    {
        private static readonly IDictionary<FundingLineCsvGeneratorJobType, string> _predicates = new Dictionary<FundingLineCsvGeneratorJobType, string>
        {
            {FundingLineCsvGeneratorJobType.CurrentState, "1 = 1"},
            {FundingLineCsvGeneratorJobType.Released, "c.content.current.status = 'Released'"},
            {FundingLineCsvGeneratorJobType.CurrentProfileValues, "fl.name = @fundingLineCode"}
        };

        private static readonly IDictionary<FundingLineCsvGeneratorJobType, string> _joinPredicates = new Dictionary<FundingLineCsvGeneratorJobType, string>
        {
            {FundingLineCsvGeneratorJobType.CurrentProfileValues, "JOIN fl IN c.content.current.fundingLines"}
        };

        public string BuildJoinPredicate(FundingLineCsvGeneratorJobType jobType)
        {
            return _joinPredicates.TryGetValue(jobType, out string predicate) ? predicate : string.Empty;
        }

        public string BuildPredicate(FundingLineCsvGeneratorJobType jobType)
        {
            return _predicates.TryGetValue(jobType, out string predicate) ? predicate : throw new ArgumentOutOfRangeException();
        }
    }
}