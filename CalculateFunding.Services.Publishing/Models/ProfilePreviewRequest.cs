using CalculateFunding.Common.ApiClient.Profiling.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Publishing.Models
{
    public class ProfilePreviewRequest
    {
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }
        
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }
        
        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }
        
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }
        
        [JsonProperty("fundingLineCode")]
        public string FundingLineCode { get; set; }
        
        [JsonProperty("profilePatternKey")]
        public string ProfilePatternKey { get; set; }
        
        [JsonProperty("configurationType")]
        public ProfileConfigurationType ConfigurationType { get; set; }

        public override string ToString()
            =>
                $"SpecificationId={SpecificationId}, FundingStreamId={FundingStreamId}, FundingPeriodId={FundingPeriodId}, ProviderId={ProviderId}, FundingLineCode={FundingLineCode}, ProfilePatternKey={ProfilePatternKey}, ConfigurationType={ConfigurationType}";
    }
}