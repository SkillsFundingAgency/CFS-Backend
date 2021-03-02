using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Profiling.Models;

namespace CalculateFunding.Services.Publishing.SqlExport
{
    public class SchemaContext
    {
        public IDictionary<string, ProfilePeriodPattern[]> FundingLineProfilePatterns { get; private set; }

        public void AddFundingLineProfilePatterns(string fundingLineCode,
            ProfilePeriodPattern[] profilePeriodPatterns)
        {
            FundingLineProfilePatterns ??= new Dictionary<string, ProfilePeriodPattern[]>();

            FundingLineProfilePatterns[fundingLineCode] = profilePeriodPatterns;
        }
    }
}