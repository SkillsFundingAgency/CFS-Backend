using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Models.Results
{

    public class ProviderProfilingResponseModel
    {
        public ProviderProfilingResponseModel()
        {
            ProfilePeriods = Enumerable.Empty<ProfilingPeriod>();

            AllocationvaluesByDistributionPeriod = Enumerable.Empty<AllocationPeriodValue>();
        }

        public AllocationOrganisation AllocationOrganisation { get; set; }

        public string FundingStreamPeriod { get; set; }

        public IEnumerable<ProfilingPeriod> ProfilePeriods { get; set; }

        public IEnumerable<AllocationPeriodValue> AllocationvaluesByDistributionPeriod { get; set; }
    }
}
