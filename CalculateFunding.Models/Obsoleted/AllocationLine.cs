using System;
using System.Collections.Generic;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Obsoleted
{
    [Obsolete]
    public class AllocationLine : Reference
    {
        public AllocationLine()
        {
            ProviderLookups = new List<ProviderLookup>();
        }

        [JsonProperty("fundingRoute")]
        public FundingRoute FundingRoute { get; set; }

        [JsonProperty("isContractRequired")]
        public bool IsContractRequired { get; set; }

        [JsonProperty("shortName")]
        public string ShortName { get; set; }

        [JsonProperty("providerLookups")]
        public IEnumerable<ProviderLookup> ProviderLookups { get; set; }
    }
}