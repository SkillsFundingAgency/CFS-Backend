using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Result
{
    public class FundingStructure
    {
        [JsonProperty("items")]
        public IEnumerable<FundingStructureItem> Items { get; set; }
        
        [JsonProperty("lastUpdated")]
        public DateTimeOffset LastModified { get; set; }
    }
}