using System;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Results.Models
{
    public class SpecificationInformation
    {
        [JsonProperty("id")]
        public string Id { get; set; }
            
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("lastEditDate")]
        public DateTimeOffset? LastEditDate { get; set; }
        
        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }
            
        [JsonProperty("fundingPeriodEnd")]
        public DateTimeOffset? FundingPeriodEnd { get; set; }

        public void MergeMutableInformation(SpecificationInformation specificationInformation)
        {
            LastEditDate = specificationInformation.LastEditDate;
            FundingPeriodEnd = specificationInformation.FundingPeriodEnd;
        }
    }
}