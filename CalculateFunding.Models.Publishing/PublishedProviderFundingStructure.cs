using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Publishing
{
    public class PublishedProviderFundingStructure
    {
        [JsonProperty("items")]
        public IEnumerable<PublishedProviderFundingStructureItem> Items { get; set; }

        [JsonIgnore]
        public int PublishedProviderVersion { get; set; }
    }
}