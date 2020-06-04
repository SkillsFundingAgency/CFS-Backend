using System.Collections.Generic;
using CalculateFunding.Models.Policy;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Policy.Models
{
    public class FundingStreamWithPeriods
    {
        public FundingStream FundingStream { get; set; }
        
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public IEnumerable<FundingPeriod> FundingPeriods { get; set; }
    }
}