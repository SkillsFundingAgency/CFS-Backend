using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationCurrentVersion : Reference
    {
        [JsonProperty("fundingPeriod")]
        public Reference FundingPeriod { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("policies")]
        public IEnumerable<Policy> Policies { get; set; } = Enumerable.Empty<Policy>();

        [JsonProperty("dataDefinitionRelationshipIds")]
        public IEnumerable<string> DataDefinitionRelationshipIds { get; set; }

        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset LastUpdatedDate { get; set; }

        [JsonProperty("fundingStreams")]
        public IEnumerable<FundingStream> FundingStreams { get; set; }

        [JsonProperty("publishStatus")]
        public PublishStatus PublishStatus { get; set; }

        [JsonProperty("isSelectedForFunding")]
        public bool IsSelectedForFunding { get; set; }

        [JsonProperty("publishedResultsRefreshedAt")]
        public DateTimeOffset? PublishedResultsRefreshedAt { get; set; }

		[JsonProperty("variationDate")]
		public DateTimeOffset? VariationDate { get; set; }

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
