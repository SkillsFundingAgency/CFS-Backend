using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationSummary : Reference
    {
        [JsonProperty("fundingPeriod")]
        public Reference FundingPeriod { get; set; }

        [JsonProperty("fundingStreams")]
        public IEnumerable<Reference> FundingStreams { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("isSelectedForFunding")]
        public bool IsSelectedForFunding { get; set; }

        [JsonProperty("approvalStatus")]
        public PublishStatus ApprovalStatus { get; set; }

        [JsonProperty("publishedResultsRefreshedAt")]
        public DateTimeOffset? PublishedResultsRefreshedAt { get; set; }

        [JsonProperty("lastCalculationUpdatedAt")]
        public DateTimeOffset? LastCalculationUpdatedAt { get; set; }
    }
}
