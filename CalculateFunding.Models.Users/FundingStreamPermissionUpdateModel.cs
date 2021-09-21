using Newtonsoft.Json;

namespace CalculateFunding.Models.Users
{
    public class FundingStreamPermissionUpdateModel
    {
        [JsonProperty("canAdministerFundingStream")]
        public bool CanAdministerFundingStream { get; set; }

        [JsonProperty("canCreateSpecification")]
        public bool CanCreateSpecification { get; set; }

        [JsonProperty("canEditSpecification")]
        public bool CanEditSpecification { get; set; }

        [JsonProperty("canApproveSpecification")]
        public bool CanApproveSpecification { get; set; }

        [JsonProperty("canEditCalculations")]
        public bool CanEditCalculations { get; set; }

        [JsonProperty("canMapDatasets")]
        public bool CanMapDatasets { get; set; }

        [JsonProperty("canChooseFunding")]
        public bool CanChooseFunding { get; set; }

        [JsonProperty("canRefreshFunding")]
        public bool CanRefreshFunding { get; set; }

        [JsonProperty("canApproveFunding")]
        public bool CanApproveFunding { get; set; }

        [JsonProperty("canReleaseFunding")]
        public bool CanReleaseFunding { get; set; }

        [JsonProperty("canCreateTemplates")]
        public bool CanCreateTemplates { get; set; }

        [JsonProperty("canEditTemplates")]
        public bool CanEditTemplates { get; set; }

        [JsonProperty("canApproveTemplates")]
        public bool CanApproveTemplates { get; set; }

        [JsonProperty("canCreateProfilePattern")]
        public bool CanCreateProfilePattern { get; set; }

        [JsonProperty("canEditProfilePattern")]
        public bool CanEditProfilePattern { get; set; }

        [JsonProperty("canAssignProfilePattern")]
        public bool CanAssignProfilePattern { get; set; }

        [JsonProperty("canApplyCustomProfilePattern")]
        public bool CanApplyCustomProfilePattern { get; set; }

        [JsonProperty("canApproveCalculations")]
        public bool CanApproveCalculations { get; set; }

        [JsonProperty("canApproveAnyCalculations")]
        public bool CanApproveAnyCalculations { get; set; }

        [JsonProperty("canApproveAllCalculations")]
        public bool CanApproveAllCalculations { get; set; }

        [JsonProperty("canRefreshPublishedQa")]
        public bool CanRefreshPublishedQa { get; set; }

        [JsonProperty("canUploadDataSourceFiles")]
        public bool CanUploadDataSourceFiles { get; set; }
    }
}
