using System;
using System.Text;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Policy;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    public class PublishedProviderResult : IIdentifiable
    {
        public PublishedProviderResult()
        {
        }

        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("summary")]
        public string Summary => 
             $"{FundingStreamResult.AllocationLineResult.Current.Provider.ProviderProfileIdType}: {FundingStreamResult.AllocationLineResult.Current.Provider.Id}, version {FundingStreamResult.AllocationLineResult.Current.VersionNumber}";

        [JsonProperty("id")]
        public string Id =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes($"{SpecificationId}{ProviderId}{FundingStreamResult.AllocationLineResult.AllocationLine.Id}"));

        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("fundingStreamResult")]
        public PublishedFundingStreamResult FundingStreamResult { get; set; }

        [JsonProperty("fundingPeriod")]
        public Period FundingPeriod { get; set; }
    }
}
