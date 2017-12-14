using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class FundingStream : Reference
    {

        [JsonProperty("allocationLines")]
        public List<AllocationLine> AllocationLines { get; set; }
    }
}