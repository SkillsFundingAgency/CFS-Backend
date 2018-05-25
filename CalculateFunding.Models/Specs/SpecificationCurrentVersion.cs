using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationCurrentVersion : SpecificationSummary
    {
        [JsonProperty("policies")]
        public IEnumerable<Policy> Policies { get; set; } = Enumerable.Empty<Policy>();

        [JsonProperty("dataDefinitionRelationshipIds")]
        public IEnumerable<string> DataDefinitionRelationshipIds { get; set; }

        [JsonProperty("lastUpdatedDate")]
        public DateTime LastUpdatedDate { get; set; }

        public new IEnumerable<FundingStream> FundingStreams { get; set; }
    }
}
