using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Users
{
    public class FundingStreamPermission : IIdentifiable
    {
        [JsonProperty("id")]
        public string Id => $"{UserId}_{FundingStreamId}";

        [JsonProperty("userId")]
        public string UserId { get; set; }

        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

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

        [JsonProperty("canReleaseFundingForStatement")]
        public bool CanReleaseFundingForStatement { get; set; }

        [JsonProperty("canReleaseFundingForPaymentOrContract")]
        public bool CanReleaseFundingForPaymentOrContract { get; set; }

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

        public bool HasSamePermissions(FundingStreamPermission fundingStreamPermission)
        {
            return fundingStreamPermission.CanCreateSpecification == CanCreateSpecification &&
                fundingStreamPermission.CanEditSpecification == CanEditSpecification &&
                fundingStreamPermission.CanEditCalculations == CanEditCalculations &&
                fundingStreamPermission.CanMapDatasets == CanMapDatasets &&
                fundingStreamPermission.CanChooseFunding == CanChooseFunding &&
                fundingStreamPermission.CanRefreshFunding == CanRefreshFunding &&
                fundingStreamPermission.CanApproveFunding == CanApproveFunding &&
                fundingStreamPermission.CanReleaseFunding == CanReleaseFunding &&
                fundingStreamPermission.CanReleaseFundingForStatement == CanReleaseFundingForStatement &&
                fundingStreamPermission.CanReleaseFundingForPaymentOrContract == CanReleaseFundingForPaymentOrContract &&
                fundingStreamPermission.CanAdministerFundingStream == CanAdministerFundingStream &&
                fundingStreamPermission.CanApproveSpecification == CanApproveSpecification &&
                fundingStreamPermission.CanCreateTemplates == CanCreateTemplates &&
                fundingStreamPermission.CanEditTemplates == CanEditTemplates &&
                fundingStreamPermission.CanApproveTemplates == CanApproveTemplates &&
                fundingStreamPermission.CanCreateProfilePattern == CanCreateProfilePattern &&
                fundingStreamPermission.CanEditProfilePattern == CanEditProfilePattern &&
                fundingStreamPermission.CanAssignProfilePattern == CanAssignProfilePattern &&
                fundingStreamPermission.CanApplyCustomProfilePattern == CanApplyCustomProfilePattern &&
                fundingStreamPermission.CanApproveCalculations == CanApproveCalculations &&
                fundingStreamPermission.CanApproveAnyCalculations == CanApproveAnyCalculations &&
                fundingStreamPermission.CanApproveAllCalculations == CanApproveAllCalculations &&
                fundingStreamPermission.CanRefreshPublishedQa == CanRefreshPublishedQa &&
                fundingStreamPermission.CanUploadDataSourceFiles == CanUploadDataSourceFiles;
        }

        public bool HasAnyPermissions()
        {
            return CanCreateSpecification
                || CanEditSpecification
                || CanEditCalculations
                || CanMapDatasets
                || CanChooseFunding
                || CanRefreshFunding
                || CanApproveFunding
                || CanReleaseFunding
                || CanReleaseFundingForStatement
                || CanReleaseFundingForPaymentOrContract
                || CanAdministerFundingStream
                || CanApproveSpecification
                || CanCreateTemplates
                || CanEditTemplates
                || CanApproveTemplates
                || CanCreateProfilePattern
                || CanEditProfilePattern
                || CanAssignProfilePattern
                || CanApplyCustomProfilePattern
                || CanApproveCalculations
                || CanApproveAnyCalculations
                || CanApproveAllCalculations
                || CanRefreshPublishedQa
                || CanUploadDataSourceFiles;
        }
    }
}
