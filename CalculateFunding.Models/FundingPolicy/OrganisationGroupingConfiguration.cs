using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CalculateFunding.Models.FundingPolicy
{
    public class OrganisationGroupingConfiguration
    {
        /// <summary>
        /// Grouping organisation 
        /// </summary>
        [EnumDataType(typeof(OrganisationIdentifierType))]
        [JsonProperty("identifierType")]
        public OrganisationIdentifierType IdentifierType { get; set; }

        [EnumDataType(typeof(GroupingReason))]
        [JsonProperty("groupingReason")]
        public GroupingReason GroupingReason { get; set; }

        [EnumDataType(typeof(OrganisationGroupingType))]
        [JsonProperty("organisationGroupingType")]
        public OrganisationGroupingType OrganisationGroupingType { get; set; }

        [JsonProperty("providerTypeMatch")]
        public IEnumerable<ProviderTypeMatch> ProviderTypeMatch { get; set; }
    }
}