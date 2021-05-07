using System;
using System.Collections.Generic;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Api.Profiling.IntegrationTests.ReProfiling
{
    public class ProfilePatternTemplateParametersBuilder : TestEntityBuilder
    {
        private string _id;
        private string _fundingPeriodId;
        private string _fundingStream;
        private string _fundingLineId;
        private bool? _reProfilingEnabled;
        private string _increasedAmountStrategyKey;
        private string _decreasedAmountStrategy;
        private string _sameAmountStrategy;
        private string _displayName;
        private DateTimeOffset? _fundingStreamPeriodStartDate;
        private DateTimeOffset? _fundingStreamPeriodEndDate;
        private IEnumerable<ProfilePeriodPattern> _profilePattern;

        public ProfilePatternTemplateParametersBuilder WithId(string id)
        {
            _id = id;

            return this;
        }
        
        public ProfilePatternTemplateParametersBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }
        
        public ProfilePatternTemplateParametersBuilder WithFundingStream(string fundingStream)
        {
            _fundingStream = fundingStream;

            return this;
        }
        
        public ProfilePatternTemplateParametersBuilder WithFundingLineId(string fundingLineId)
        {
            _fundingLineId = fundingLineId;

            return this;
        }
        
        public ProfilePatternTemplateParametersBuilder WithReProfilingEnabled(bool reProfilingEnabled)
        {
            _reProfilingEnabled = reProfilingEnabled;
            
            return this;
        }
        
        
        public ProfilePatternTemplateParametersBuilder WithIncreasedAmountStrategyKey(string increasedAmountStrategyKey)
        {
            _increasedAmountStrategyKey = increasedAmountStrategyKey;
            
            return this;
        }
        
        public ProfilePatternTemplateParametersBuilder WithDecreasedAmountStrategyKey(string decreasedAmountStrategyKey)
        {
            _decreasedAmountStrategy = decreasedAmountStrategyKey;
            
            return this;
        }
        
        public ProfilePatternTemplateParametersBuilder WithSameAmountStrategyKey(string sameAmountStrategyKey)
        {
            _sameAmountStrategy = sameAmountStrategyKey;
            
            return this;
        }
        
        public ProfilePatternTemplateParametersBuilder WithProfilePattern(params ProfilePeriodPattern[] profilePattern)
        {
            _profilePattern = profilePattern;
            
            return this;
        }
        
        public ProfilePatternTemplateParameters Build()
        {
            return new ProfilePatternTemplateParameters
            {
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStream = _fundingStream ?? NewRandomString(),
                FundingLineId = _fundingLineId ?? NewRandomString(),
                ReProfilingEnabled = _reProfilingEnabled.GetValueOrDefault(NewRandomFlag()).ToString().ToLower(),
                IncreasedAmountStrategyKey = _increasedAmountStrategyKey ?? NewRandomString(),
                DecreasedAmountStrategyKey = _decreasedAmountStrategy ?? NewRandomString(),
                SameAmountStrategyKey = _sameAmountStrategy ?? NewRandomString(),
                ProfilePattern = _profilePattern ?? new ProfilePeriodPattern[0],
                DisplayName = _displayName ?? NewRandomString(),
                FundingStreamPeriodStartDate = _fundingStreamPeriodStartDate.GetValueOrDefault(NewRandomDateTime()).ToString("o"),
                FundingStreamPeriodEndDate = _fundingStreamPeriodEndDate.GetValueOrDefault(NewRandomDateTime()).ToString("o"),
            };
        }
    }
}