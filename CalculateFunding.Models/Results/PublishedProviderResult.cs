using System;
using System.Collections.Generic;
using System.Text;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Specs;
using Newtonsoft.Json;

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
        public string Summary
        {
            get
            {
                return $"{FundingStreamResult.AllocationLineResult.Current.Provider.ProviderProfileIdType}: {FundingStreamResult.AllocationLineResult.Current.Provider.Id}, version {FundingStreamResult.AllocationLineResult.Current.VersionNumber}";
            }
        }

        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{SpecificationId}{ProviderId}{FundingStreamResult.AllocationLineResult.AllocationLine.Id}"));
            }
        }

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("fundingStreamResult")]
        public PublishedFundingStreamResult FundingStreamResult { get; set; }

        [JsonProperty("fundingPeriod")]
        public Period FundingPeriod { get; set; }

        [JsonProperty("profilePeriods")]
        public ProfilingPeriod[] ProfilingPeriods { get; set; }

        [JsonProperty("financialEnvelopes")]
        public IEnumerable<FinancialEnvelope> FinancialEnvelopes { get; set; }
    }
}
