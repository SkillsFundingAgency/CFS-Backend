﻿using CalculateFunding.Common.Models;
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

        [JsonProperty("canAdministerFundingStream")]
        public bool CanAdministerFundingStream { get; set; }

        [JsonProperty("canCreateSpecification")]
        public bool CanCreateSpecification { get; set; }

        [JsonProperty("canEditSpecification")]
        public bool CanEditSpecification { get; set; }

        [JsonProperty("canApproveSpecification")]
        public bool CanApproveSpecification { get; set; }

        [JsonProperty("canDeleteSpecification")]
        public bool CanDeleteSpecification { get; set; }

        [JsonProperty("canEditCalculations")]
        public bool CanEditCalculations { get; set; }

        [JsonProperty("canDeleteCalculations")]
        public bool CanDeleteCalculations { get; set; }

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

        [JsonProperty("canCreateQaTests")]
        public bool CanCreateQaTests { get; set; }

        [JsonProperty("canEditQaTests")]
        public bool CanEditQaTests { get; set; }

        [JsonProperty("canDeleteQaTests")]
        public bool CanDeleteQaTests { get; set; }

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
                fundingStreamPermission.CanAdministerFundingStream == CanAdministerFundingStream &&
                fundingStreamPermission.CanCreateQaTests == CanCreateQaTests &&
                fundingStreamPermission.CanEditQaTests == CanEditQaTests &&
                fundingStreamPermission.CanApproveSpecification == CanApproveSpecification &&
                fundingStreamPermission.CanDeleteCalculations == CanDeleteCalculations &&
                fundingStreamPermission.CanDeleteQaTests == CanDeleteQaTests;
        }
    }
}
