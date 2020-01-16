using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class Specification : Reference
    {
        [JsonProperty("isSelectedForFunding")]
        public bool IsSelectedForFunding { get; set; }

        [JsonProperty("current")]
        public SpecificationVersion Current { get; set; }
    }
}