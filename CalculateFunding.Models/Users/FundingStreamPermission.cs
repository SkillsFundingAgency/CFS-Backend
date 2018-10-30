using Newtonsoft.Json;

namespace CalculateFunding.Models.Users
{
    public class FundingStreamPermission : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return $"{UserId}_{FundingStreamId}";
            }
        }

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("canCreateSpecification")]
        public bool CanCreateSpecification { get; set; }

        [JsonProperty("canEditSpecification")]
        public bool CanEditSpecification { get; set; }

        [JsonProperty("canEditCalculations")]
        public bool CanEditCalculations { get; set; }

        [JsonProperty("canMapDatasets")]
        public bool CanMapDatasets { get; set; }

        [JsonProperty("canChooseFunding")]
        public bool CanChooseFunding { get; set; }

        [JsonProperty("canApproveFunding")]
        public bool CanApproveFunding { get; set; }

        [JsonProperty("canPublishFunding")]
        public bool CanPublishFunding { get; set; }

        public bool HasSamePermissions(FundingStreamPermission fundingStreamPermission)
        {
            return fundingStreamPermission.CanCreateSpecification == CanCreateSpecification &&
                fundingStreamPermission.CanEditSpecification == CanEditSpecification &&
                fundingStreamPermission.CanEditCalculations == CanEditCalculations &&
                fundingStreamPermission.CanMapDatasets == CanMapDatasets &&
                fundingStreamPermission.CanChooseFunding == CanChooseFunding &&
                fundingStreamPermission.CanApproveFunding == CanApproveFunding &&
                fundingStreamPermission.CanPublishFunding == CanPublishFunding;
        }
    }
}
