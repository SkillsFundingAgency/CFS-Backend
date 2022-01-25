using System;
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

        //TODO: this constructor is ONLY for unit tests - replace with a builder
        public FundingStreamPeriodProfilePattern(string fundingPeriodId,
            string fundingStreamId, 
            string fundingLineId,
            DateTime fundingStreamPeriodStartDate,
            DateTime fundingStreamPeriodEndDate,
            bool allowUserToEditProfilePattern,
            ProfilePeriodPattern[] profilePattern,
            string profilePatternDisplayName,
            string profilePatternDescription,
            RoundingStrategy roundingStrategy)
        {
            FundingPeriodId = fundingPeriodId;
            FundingStreamId = fundingStreamId;
            FundingLineId = fundingLineId;
            FundingStreamPeriodStartDate = fundingStreamPeriodStartDate;
            FundingStreamPeriodEndDate = fundingStreamPeriodEndDate;
            AllowUserToEditProfilePattern = allowUserToEditProfilePattern;
            ProfilePattern = profilePattern;
            ProfilePatternDisplayName = profilePatternDisplayName;
            ProfilePatternDescription = profilePatternDescription;
            RoundingStrategy = roundingStrategy;
        }

        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }

        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }
        
        [JsonProperty("roundingStrategy")]
        public RoundingStrategy RoundingStrategy { get; set; }

        [JsonProperty("fundingLineId")]
        public string FundingLineId { get; set; }

        [JsonProperty("profilePatternKey")]
        public string ProfilePatternKey { get; set; }

        [JsonProperty("fundingStreamPeriodStartDate")]
        public DateTime FundingStreamPeriodStartDate { get; set; }

        [JsonProperty("fundingStreamPeriodEndDate")]
        public DateTime FundingStreamPeriodEndDate { get; set; }

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

        [JsonProperty("reProfilingConfiguration")]
        public ProfilePatternReProfilingConfiguration ReProfilingConfiguration { get; set; }

        [JsonProperty("profileCacheETag")]
        public string ProfileCacheETag { get; set; }

        [JsonProperty("eTag")]
        public string ETag => $"{ProfilePattern.AsJson()}-{ReProfilingConfiguration?.SameAmountStrategyKey}{ProfileCacheETag}".ComputeSHA1Hash();

        [JsonProperty("id")]
        public string Id => $"{FundingPeriodId}-{FundingStreamId}-{FundingLineId}{ProfilePatternKeyString}";

        private string ProfilePatternKeyString => ProfilePatternKey.IsNullOrEmpty() ? null : $"-{ProfilePatternKey}";

        public string GetReProfilingStrategyKeyForInitialFunding()
            => ReProfilingConfiguration?.InitialFundingStrategyKey;

        public string GetReProfilingStrategyKeyForInitialFundingWithCatchup()
            => ReProfilingConfiguration?.InitialFundingStrategyWithCatchupKey;
        
        public string GetReProfilingStrategyKeyForInitialClosureFunding()
            => ReProfilingConfiguration?.InitialClosureFundingStrategyKey;

        public string GetReProfilingStrategyKeyForConverterFunding()
            => ReProfilingConfiguration?.ConverterFundingStrategyKey;

        public string GetReProfilingStrategyKeyForFundingAmountChange(ReProfileRequest request)
            => ReProfilingConfiguration?.GetReProfilingStrategyKeyForFundingAmountChange(request);
    }
}