using System;
using System.Text;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Results.Migration
{
    public class PublishedProviderCalculationResult : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{Specification.Id}{ProviderId}{CalculationSpecification.Id}"));
            }
        }

        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [JsonProperty("specification")]
        public Reference Specification { get; set; }

        [JsonProperty("calculationSpecification")]
        public Reference CalculationSpecification { get; set; }

        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }

        [JsonProperty("fundingPeriod")]
        public Reference FundingPeriod { get; set; }

        [JsonProperty("status")]
        public PublishStatus Status { get; set; }

        [JsonProperty("current")]
        public PublishedProviderCalculationResultVersion Current { get; set; }
    }
}
