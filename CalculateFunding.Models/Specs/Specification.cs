using System;
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

        [JsonProperty("publishedResultsRefreshedAt")]
        public DateTimeOffset? PublishedResultsRefreshedAt { get; set; }

        [JsonProperty("lastCalculationUpdatedAt")]
        public DateTimeOffset? LastCalculationUpdatedAt { get; set; }

        [JsonIgnore]
        public bool ShouldRefresh
        {
            get
            {
                return !PublishedResultsRefreshedAt.HasValue || (LastCalculationUpdatedAt.HasValue && LastCalculationUpdatedAt.Value > PublishedResultsRefreshedAt.Value);
            }
        }
    }
}