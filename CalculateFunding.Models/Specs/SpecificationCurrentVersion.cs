using System;
using System.Collections.Generic;
using System.Linq;
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
        public DateTime LastUpdatedDate { get; set; }

        [JsonProperty("fundingStreams")]
        public IEnumerable<FundingStream> FundingStreams { get; set; }
    }
}
