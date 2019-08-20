using System;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    [Obsolete]
    public class PublishedAllocationLineResult
    {
        [JsonProperty("allocationLine")]
        public PublishedAllocationLineDefinition AllocationLine { get; set; }

        [JsonProperty("current")]
        public PublishedAllocationLineResultVersion Current { get; set; }

        [JsonProperty("published")]
        public PublishedAllocationLineResultVersion Published { get; set; }

        /// <summary>
        /// Gets or sets a flag indicating whether the result has been altered due to a variation
        /// </summary>
        [JsonProperty("hasResultBeenVaried")]
        public bool HasResultBeenVaried { get; set; }
    }
}
