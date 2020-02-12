using System.Collections.Generic;

namespace CalculateFunding.Publishing.AcceptanceTests.Models
{
    public class ExpectedFundingLineProfileValues
    {
        public string FundingLineCode { get; set; }

        public IEnumerable<ExpectedDistributionPeriod> ExpectedDistributionPeriods { get; set; }
    }
}