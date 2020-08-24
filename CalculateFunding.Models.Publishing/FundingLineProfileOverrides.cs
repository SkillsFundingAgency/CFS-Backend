using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    public class FundingLineProfileOverrides
    {
        public string FundingLineCode { get; set; }
        
        public IEnumerable<DistributionPeriod> DistributionPeriods { get; set; }
        
        public decimal? CarryOver { get; set; }

        [JsonIgnore]
        public bool HasCarryOver => CarryOver.HasValue;
    }
}