using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Models.Results
{
    public class ProviderProfilingRequestModel
    {
        public ProviderProfilingRequestModel()
        {
            AllocationValuesByDistributionPeriod = Enumerable.Empty<AllocationPeriodValue>();
        }

        public AllocationOrganisation AllocationOrganisation { get; set; }

        public string FundingStreamPeriod { get; set; }

        public DateTimeOffset AllocationStartDate { get; set; }

        public DateTimeOffset AllocationEndDate { get; set; }

        public IEnumerable<AllocationPeriodValue> AllocationValuesByDistributionPeriod { get; set; }
    }
}
