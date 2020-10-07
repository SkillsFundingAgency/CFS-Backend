﻿using System;
using System.Collections.Generic;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.Models;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Profiling.Models
{
    public class FundingStreamPeriodProfilePattern : IIdentifiable
    {

        public FundingStreamPeriodProfilePattern()
        {

        }

        public FundingStreamPeriodProfilePattern(string fundingPeriodId,
            string fundingStreamId, 
            string fundingLineId,
            DateTime fundingStreamPeriodStartDate,
            DateTime fundingStreamPeriodEndDate,
            bool reProfilePastPeriods, 
            bool calculateBalancingPayment,
            bool allowUserToEditProfilePattern,
            ProfilePeriodPattern[] profilePattern,
            string profilePatternDisplayName,
            string profilePatternDescription)
        {
            FundingPeriodId = fundingPeriodId;
            FundingStreamId = fundingStreamId;
            FundingLineId = fundingLineId;
            FundingStreamPeriodStartDate = fundingStreamPeriodStartDate;
            FundingStreamPeriodEndDate = fundingStreamPeriodEndDate;
            ReProfilePastPeriods = reProfilePastPeriods;
            CalculateBalancingPayment = calculateBalancingPayment;
            AllowUserToEditProfilePattern = allowUserToEditProfilePattern;
            ProfilePattern = profilePattern;
            ProfilePatternDisplayName = profilePatternDisplayName;
            ProfilePatternDescription = profilePatternDescription;
        }

        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }

        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [JsonProperty("fundingLineId")]
        public string FundingLineId { get; set; }
        
        [JsonProperty("profilePatternKey")]
        public string ProfilePatternKey { get; set; }

        [JsonProperty("fundingStreamPeriodStartDate")]
        public DateTime FundingStreamPeriodStartDate { get; set; }

        [JsonProperty("fundingStreamPeriodEndDate")]
        public DateTime FundingStreamPeriodEndDate { get; set; }

        [JsonProperty("reProfilePastPeriods")]
        public bool ReProfilePastPeriods { get; set; }

        [JsonProperty("calculateBalancingPayment")]
        public bool CalculateBalancingPayment { get; set; }

        [JsonProperty("allowUserToEditProfilePattern")]
        public bool AllowUserToEditProfilePattern { get; set; }

        [JsonProperty("profilePattern")]
        public ProfilePeriodPattern[] ProfilePattern { get; set; }

        [JsonProperty("profilePatternDisplayName")]
        public string ProfilePatternDisplayName { get; set; }

        [JsonProperty("profilePatternDescription")]
        public string ProfilePatternDescription { get; set; }

        [JsonProperty("providerTypeSubTypes")]
        public IEnumerable<ProviderTypeSubType> ProviderTypeSubTypes { get; set; }

        [JsonProperty("id")]
        public string Id => $"{FundingPeriodId}-{FundingStreamId}-{FundingLineId}{ProfilePatternKeyString}";

        private string ProfilePatternKeyString => ProfilePatternKey.IsNullOrEmpty() ? null : $"-{ProfilePatternKey}";
    }
}