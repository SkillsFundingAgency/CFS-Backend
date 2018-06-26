using CalculateFunding.Models.Calcs;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Results
{
    public class PublishedCalculationResult
    {
        [JsonProperty("current")]
        public PublishedProviderCalculationResultCalculationVersion Current { get; set; }

        [JsonProperty("published")]
        public PublishedProviderCalculationResultCalculationVersion Published { get; set; }

        [JsonProperty("history")]
        public List<PublishedProviderCalculationResultCalculationVersion> History { get; set; }

        [JsonProperty("calculationSpecification")]
        public Reference CalculationSpecification { get; set; }

        [JsonProperty("calculationType")]
        public Models.Specs.CalculationType CalculationType { get; set; }

        [JsonProperty("isPublic")]
        public bool IsPublic { get; set; }

        public int GetNextVersion()
        {
            if (History == null || !History.Any())
                return 1;

            int maxVersion = History.Max(m => m.Version);

            return maxVersion + 1;
        }
    }

}
