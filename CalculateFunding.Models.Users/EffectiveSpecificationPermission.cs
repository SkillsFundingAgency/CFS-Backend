﻿using Newtonsoft.Json;

namespace CalculateFunding.Models.Users
{
    public class EffectiveSpecificationPermission
    {
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [JsonProperty("userId")]
        public string UserId { get; set; }

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

        [JsonProperty("canApproveCalculations")]
        public bool CanApproveCalculations { get; set; }

        [JsonProperty("canApproveAnyCalculations")]
        public bool CanApproveAnyCalculations { get; set; }

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

        [JsonProperty("canAssignProfilePattern")]
        public bool CanAssignProfilePattern { get; set; }

        [JsonProperty("canApplyCustomProfilePattern")]
        public bool CanApplyCustomProfilePattern { get; set; }

        [JsonProperty("canApproveAllCalculations")]
        public bool CanApproveAllCalculations { get; set; }

        [JsonProperty("canRefreshPublishedQa")]
        public bool CanRefreshPublishedQa { get; set; }
    }
}
