using CalculateFunding.Models.Specs;
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
            AllocationValueByDistributionPeriod = Enumerable.Empty<AllocationPeriodValue>();
        }

        public string FundingStreamPeriod { get; set; }

        public IEnumerable<AllocationPeriodValue> AllocationValueByDistributionPeriod { get; set; }
    }
}
