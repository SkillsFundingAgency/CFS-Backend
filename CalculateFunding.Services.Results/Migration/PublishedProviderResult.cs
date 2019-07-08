using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Results;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Results.Migration
{
    public class PublishedProviderResult : IIdentifiable
    {
        public PublishedProviderResult()
        {
            ProfilingPeriods = Enumerable.Empty<ProfilingPeriod>();
            FinancialEnvelopes = Enumerable.Empty<FinancialEnvelope>();
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

        //The below are for migration purposes only

        [JsonProperty("profilePeriods")]
        public IEnumerable<ProfilingPeriod> ProfilingPeriods { get; set; }

        [JsonProperty("financialEnvelopes")]
        public IEnumerable<FinancialEnvelope> FinancialEnvelopes { get; set; }
    }
}
