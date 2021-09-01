using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Policy.FundingPolicy
{
    public class VariationType
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("order")]
        public int Order { get; set; }

        [JsonProperty("fundingLineCodes")]
        public IEnumerable<string> FundingLineCodes { get; set; }
    }
}