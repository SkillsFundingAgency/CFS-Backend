using System;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Result.ViewModels
{
    public class UpdateFundingStructureLastModifiedRequest
    {
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }
        
        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }
        
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }
        
        [JsonProperty("lastModified")]
        public DateTimeOffset LastModified { get; set; }
    }
}