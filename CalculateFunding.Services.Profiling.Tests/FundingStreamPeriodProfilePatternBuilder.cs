using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Tests.TestHelpers;

namespace CalculateFunding.Services.Profiling.Tests
{
    public class FundingStreamPeriodProfilePatternBuilder : TestEntityBuilder
    {
        private IEnumerable<ProfilePeriodPattern> _patterns;
        private string _profilePatternDisplayName;
        private IEnumerable<ProviderTypeSubType> _providerTypeSubTypes;
        private string _profilePatternKey;

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

        public FundingStreamPeriodProfilePatternBuilder WithProviderTypeSubTypes(IEnumerable<ProviderTypeSubType> providerTypeSubTypes)
        {
            _providerTypeSubTypes = providerTypeSubTypes;
            return this;
        }

        public FundingStreamPeriodProfilePatternBuilder WithProfilePatternKey(string profilePatternKey)
        {
            _profilePatternKey = profilePatternKey;
            return this;
        }

        public FundingStreamPeriodProfilePattern Build()
        {
            return new FundingStreamPeriodProfilePattern
            {
                FundingPeriodId = NewRandomString(),
                FundingStreamId = NewRandomString(),
                FundingLineId = NewRandomString(),
                ProfilePatternKey = _profilePatternKey ?? NewRandomString(),
                FundingStreamPeriodStartDate = DateTime.Today.AddDays(-1),
                FundingStreamPeriodEndDate = DateTime.Today,
                AllowUserToEditProfilePattern = false,
                ProfilePattern = _patterns?.ToArray() ?? Array.Empty<ProfilePeriodPattern>(),
                ProfilePatternDisplayName = _profilePatternDisplayName ?? NewRandomString(),
                ProviderTypeSubTypes = _providerTypeSubTypes?.ToArray() ?? Array.Empty<ProviderTypeSubType>(),
            };
        }
    }
}