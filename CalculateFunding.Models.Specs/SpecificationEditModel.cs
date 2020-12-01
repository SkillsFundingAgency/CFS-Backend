using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationEditModel
    {
        /// <summary>
        /// Used to pass from the service to the validator for duplicate name lookup
        /// </summary>
        [JsonIgnore]
        public string SpecificationId { get; set; }

        [JsonProperty("providerVersionId")]
        public string ProviderVersionId { get; set; }

        [JsonProperty("providerSnapshotId")]
        public int? ProviderSnapshotId { get; set; }

        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }

        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("assignedTemplateIds")]
        public IDictionary<string, string> AssignedTemplateIds { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("coreProviderVersionUpdates")]
        public CoreProviderVersionUpdates CoreProviderVersionUpdates { get; set; }
    }
}
