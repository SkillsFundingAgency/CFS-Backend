using CalculateFunding.Common.Models;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.FundingPolicy
{
    public class FundingConfiguration : IIdentifiable
    {
        /// <summary>
        /// Organisational groupings for funding feed publishing
        /// </summary>
        [JsonProperty("organisationGroupings")]
        public IEnumerable<OrganisationGroupingConfiguration> OrganisationGroupings { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }
        
        [JsonProperty("defaultTemplateVersion")]
        public string DefaultTemplateVersion { get; set; }
        
        [JsonProperty("variations")]
        public IEnumerable<VariationType> Variations { get; set; }
    }
}
