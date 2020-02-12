using System.Collections.Generic;

namespace CalculateFunding.Publishing.AcceptanceTests.Models
{
    public class ExpectedDistributionPeriod
    {
        public string DistributionPeriodId { get; set; }

        public IEnumerable<ExpectedProfileValue> ExpectedProfileValues { get; set; }
    }
}