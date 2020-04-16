using System.Collections.Generic;

namespace CalculateFunding.Models.Publishing
{
    public class FundingLineProfileOverrides
    {
        public string FundingLineCode { get; set; }
        
        public IEnumerable<DistributionPeriod> DistributionPeriods { get; set; }
    }
}