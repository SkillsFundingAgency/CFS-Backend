using Newtonsoft.Json;
using System.Collections.Generic;

namespace CalculateFunding.Models.Policy.FundingPolicy
{
    public class FundingConfigurationChannel
    {
        /// <summary>
        /// Organisational groupings for funding feed publishing
        /// </summary>
        [JsonProperty("organisationGroupings")]
        public IEnumerable<OrganisationGroupingConfiguration> OrganisationGroupings { get; set; }

        /// <summary>
        /// Channel code - human readable key for identifying the channel eg Contracts or Statements.
        /// This code should match the created channel code in the publishing microservice when releasing funding
        /// </summary>
        [JsonProperty("channelCode")]
        public string ChannelCode { get; set; }

        /// <summary>
        /// Filter providers to include in this channel by provider types
        /// </summary>
        [JsonProperty("providerTypeMatch")]
        public IEnumerable<ProviderTypeMatch> ProviderTypeMatch { get; set; }

        /// <summary>
        /// Filter providers in include in this channel by provider status
        /// </summary>
        [JsonProperty("providerStatus")]
        public IEnumerable<string> ProviderStatus { get; set; }

        /// <summary>
        /// Determines whether the channel is visible in the UI and can therefore be queried against
        /// </summary>
        [JsonProperty("isVisible")]
        public bool IsVisible { get; set; }
    }
}
