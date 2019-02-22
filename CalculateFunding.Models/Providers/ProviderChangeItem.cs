using System.Collections.Generic;
using CalculateFunding.Models.Results;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Providers
{
    public class ProviderChangeItem
    {
        [JsonProperty("hasProviderDataChanged")]
        public bool HasProviderDataChanged { get; set; }

        [JsonProperty("doesProviderHaveSuccessor")]
        public bool DoesProviderHaveSuccessor { get; set; }

        [JsonProperty("hasProviderClosed")]
        public bool HasProviderClosed { get; set; }

        [JsonProperty("hasProviderOpened")]
        public bool HasProviderOpened { get; set; }

        /// <summary>
        /// Gets or sets the GIAS reasons for the variation
        /// </summary>
        [JsonProperty("providerReasonCode")]
        public string ProviderReasonCode { get; set; }

        /// <summary>
        /// Gets or sets the CFS specific reasons for the variation
        /// </summary>
        [JsonProperty("variationReasons")]
        public IEnumerable<VariationReason> VariationReasons { get; set; }

        [JsonProperty("successorProviderId")]
        public string SuccessorProviderId { get; set; }

        /// <summary>
        /// Latest (current) provider information which is being compared against the prior provider information
        /// </summary>
        [JsonProperty("updatedProvider")]
        public ProviderSummary UpdatedProvider { get; set; }

        /// <summary>
        /// Provider information in it's prior state used for this comparison
        /// </summary>
        [JsonProperty("priorProviderState")]
        public ProviderSummary PriorProviderState { get; set; }
    }
}
