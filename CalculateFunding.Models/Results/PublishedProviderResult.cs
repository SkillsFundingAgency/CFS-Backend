using CalculateFunding.Models.Specs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Models.Results
{
    public class PublishedProviderResult : IIdentifiable
    {
        public PublishedProviderResult()
        {
            ProfilingPeriods = new ProfilingPeriod[0];
        }

        [JsonProperty("providerId")]
        public string ProviderId { get; set; }
      
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{SpecificationId}{ProviderId}{FundingStreamResult.AllocationLineResult.AllocationLine}"));
            }
        }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("fundingStreamResult")]
        public PublishedFundingStreamResult FundingStreamResult { get; set; }

        [JsonProperty("fundingPeriod")]
        public FundingPeriod FundingPeriod { get; set; }

        [JsonProperty("profilePeriods")]
        public ProfilingPeriod[] ProfilingPeriods { get; set; }
    }
}
