using System;
using System.Collections.Generic;
using CalculateFunding.Services.Publishing.Interfaces;

namespace CalculateFunding.Services.Publishing.Reporting
{
    public class PublishedFundingPredicateBuilder : IPublishedFundingPredicateBuilder
    {
        private static readonly IDictionary<FundingLineCsvGeneratorJobType, string> _predicates = new Dictionary<FundingLineCsvGeneratorJobType, string>
        {
            {FundingLineCsvGeneratorJobType.CurrentState, "1 = 1"},
            {FundingLineCsvGeneratorJobType.Released, "c.content.current.status = 'Released'"}
        };

        public string BuildPredicate(FundingLineCsvGeneratorJobType jobType)
        {
            return _predicates.TryGetValue(jobType, out string predicate) ? predicate : throw new ArgumentOutOfRangeException();
        }
    }
}