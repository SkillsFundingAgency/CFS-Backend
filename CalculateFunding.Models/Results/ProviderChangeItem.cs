using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
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

        [JsonProperty("updatedProvider")]
        public ProviderSummary UpdatedProvider { get; set; }
    }
}
