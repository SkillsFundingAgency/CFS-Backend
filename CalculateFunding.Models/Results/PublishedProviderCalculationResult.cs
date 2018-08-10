using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Models.Results
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

        [JsonProperty("policy")]
        public PolicySummary Policy { get; set; }

        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }

        [JsonProperty("fundingPeriod")]
        public Reference FundingPeriod { get; set; }

        [JsonProperty("parentPolicy")]
        public PolicySummary ParentPolicy { get; set; }

        [JsonProperty("isPublic")]
        public bool IsPublic { get; set; }

        [JsonProperty("status")]
        public PublishStatus Status { get; set; }

        [JsonProperty("current")]
        public PublishedProviderCalculationResultCalculationVersion Current { get; set; }

        [JsonProperty("published")]
        public PublishedProviderCalculationResultCalculationVersion Published { get; set; }

        [JsonProperty("approved")]
        public PublishedProviderCalculationResultCalculationVersion Approved { get; set; }
    }

}
