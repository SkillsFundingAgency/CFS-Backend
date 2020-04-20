using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Policy.FundingPolicy
{
    public class OrganisationGroupingConfiguration
    {
        [JsonProperty("groupTypeIdentifier")]
        public OrganisationGroupTypeIdentifier GroupTypeIdentifier { get; set; }

        [JsonProperty("groupingReason")]
        public GroupingReason GroupingReason { get; set; }

        [JsonProperty("groupTypeClassification")]
        public OrganisationGroupTypeClassification GroupTypeClassification { get; set; }

        [JsonProperty("organisationGroupTypeCode")]
        public OrganisationGroupTypeCode OrganisationGroupTypeCode { get; set; }

        [JsonProperty("providerTypeMatch")]
        public IEnumerable<ProviderTypeMatch> ProviderTypeMatch { get; set; }
    }
}