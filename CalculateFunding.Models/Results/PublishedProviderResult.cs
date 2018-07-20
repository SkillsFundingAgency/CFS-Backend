using CalculateFunding.Models.Specs;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Models.Results
{
    public class PublishedProviderResult : IIdentifiable
    {
        public PublishedProviderResult()
        {
            ProfilingPeriods = Enumerable.Empty<ProfilingPeriod>();
        }

        [JsonProperty("providerId")]
        public string ProviderId
        {
            get
            {
                return Provider.Id;
            }
        }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("summary")]
        public string Summary { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("provider")]
        public ProviderSummary Provider { get; set; }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("fundingStreamResult")]
        public PublishedFundingStreamResult FundingStreamResult { get; set; }

        [JsonProperty("fundingPeriod")]
        public FundingPeriod FundingPeriod { get; set; }

        [JsonProperty("profilePeriods")]
        public IEnumerable<ProfilingPeriod> ProfilingPeriods { get; set; }
    }
}
