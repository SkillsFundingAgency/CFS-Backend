using System.Collections.Generic;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Providers;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Policy.FundingPolicy
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

        [JsonProperty("errorDetectors")]
        public IEnumerable<string> ErrorDetectors { get; set; }
        
        [JsonProperty("approvalMode")]
        public ApprovalMode ApprovalMode { get; set; }

        [JsonProperty("providerSource")]
        public ProviderSource ProviderSource { get; set; }

        [JsonProperty("paymentOrganisationSource")]
        public PaymentOrganisationSource PaymentOrganisationSource { get; set; }

        [JsonProperty("updateCoreProviderVersion")]
        public UpdateCoreProviderVersion UpdateCoreProviderVersion { get; set; }

        [JsonProperty("enableUserEditableCustomProfiles")]
        public bool EnableUserEditableCustomProfiles { get; set; }

        [JsonProperty("enableUserEditableRuleBasedProfiles")]
        public bool EnableUserEditableRuleBasedProfiles { get; set; }
    }
}
