using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling
{
    public class FundingStreamPeriodProfilePatternBuilder : TestEntityBuilder
    {
        private IEnumerable<ProfilePeriodPattern> _patterns;
        private string _profilePatternDisplayName;
        private string _profilePatternKey;
        private string _fundingStreamId;
        private string _fundingPeriodId;
        private string _fundingLineId;
        private bool _noPatternKey;
        
        public FundingStreamPeriodProfilePatternBuilder WithPeriods(params ProfilePeriodPattern[] patterns)
        {
            _patterns = patterns;

            return this;
        }

        public FundingStreamPeriodProfilePatternBuilder WithProfilePatternDisplayName(string profilePatternDisplayName)
        {
            _profilePatternDisplayName = profilePatternDisplayName;
            return this;
        }
        public FundingStreamPeriodProfilePatternBuilder WithProfilePatternKey(string profilePatternKey)
        {
            _profilePatternKey = profilePatternKey;

            if (_profilePatternKey == null)
            {
                return WithNoPatternKey();
            }
            
            return this;
        }

        public FundingStreamPeriodProfilePatternBuilder WithFundingStreamId(string fundingStreamId)
        {
            _fundingStreamId = fundingStreamId;

            return this;
        }
        public FundingStreamPeriodProfilePatternBuilder WithFundingPeriodId(string fundingPeriodId)
        {
            _fundingPeriodId = fundingPeriodId;

            return this;
        }
        
        public FundingStreamPeriodProfilePatternBuilder WithFundingLineId(string fundingLineId)
        {
            _fundingLineId = fundingLineId;

            return this;
        }

        public FundingStreamPeriodProfilePatternBuilder WithNoPatternKey()
        {
            _noPatternKey = true;

            return this;
        }
        
        public FundingStreamPeriodProfilePattern Build()
        {
            return new FundingStreamPeriodProfilePattern
            {
                FundingPeriodId = _fundingPeriodId ?? NewRandomString(),
                FundingStreamId = _fundingStreamId ?? NewRandomString(),
                FundingLineId = _fundingLineId ?? NewRandomString(),
                ProfilePatternKey = _profilePatternKey ?? (_noPatternKey ? null : NewRandomString()),
                FundingStreamPeriodStartDate = DateTime.Today.AddDays(-1),
                FundingStreamPeriodEndDate = DateTime.Today,
                AllowUserToEditProfilePattern = false,
                ProfilePattern = _patterns?.ToArray() ?? Array.Empty<ProfilePeriodPattern>(),
                ProfilePatternDisplayName = _profilePatternDisplayName ?? NewRandomString(),
            };
        }
    }
}