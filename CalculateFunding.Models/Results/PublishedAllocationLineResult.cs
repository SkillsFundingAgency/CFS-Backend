using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Results
{
    public class PublishedAllocationLineResult
    {
        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }

        [JsonProperty("current")]
        public PublishedAllocationLineResultVersion Current { get; set; }

        [JsonProperty("published")]
        public PublishedAllocationLineResultVersion Published { get; set; }

        [JsonProperty("history")]
        public List<PublishedAllocationLineResultVersion> History { get; set; }

        public int GetNextVersion()
        {
            if (History == null || !History.Any())
                return 1;

            int maxVersion = History.Max(m => m.Version);

            return maxVersion + 1;
        }
    }
}
