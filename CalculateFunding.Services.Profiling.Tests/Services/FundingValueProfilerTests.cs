using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Services;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Services.Profiling.Tests.Services
{
    [TestClass]
    public class FundingValueProfilerTests
    {
        [TestMethod]
        public void ProfileAllocation_ShouldSetProfilePatternKeyAndDisplayNameInResponse()
        {
            string profilePatternKey = NewRandomString();
            string profilePatternDisplayName = NewRandomString();

            ProfileBatchRequest profileRequest = NewProfileBatchRequest(_ => _.WithFundingStreamId(NewRandomString()));
            FundingStreamPeriodProfilePattern profilePattern = NewFundingStreamPeriodProfilePattern(_ => 
                                                        _.WithProfilePattern()
                                                        .WithProfilePatternKey(profilePatternKey)
                                                        .WithProfilePatternDisplayName(profilePatternDisplayName));

            IFundingValueProfiler profiler = new FundingValueProfiler();

            AllocationProfileResponse response =  profiler.ProfileAllocation(profileRequest, profilePattern, decimal.MinValue);

            response.Should()
                .NotBeNull();
            response.ProfilePatternKey
                .Should().Be(profilePatternKey);
            response.ProfilePatternDisplayName
                .Should().Be(profilePatternDisplayName);
        }

        private ProfileBatchRequest NewProfileBatchRequest(Action<ProfileBatchRequestBuilder> setUp = null)
        {
            ProfileBatchRequestBuilder profileBatchRequestBuilder = new ProfileBatchRequestBuilder();

            setUp?.Invoke(profileBatchRequestBuilder);

            return profileBatchRequestBuilder.Build();
        }

        private FundingStreamPeriodProfilePattern NewFundingStreamPeriodProfilePattern(Action<FundingStreamPeriodProfilePatternBuilder> setUp = null)
        {
            FundingStreamPeriodProfilePatternBuilder fundingStreamPeriodProfilePatternBuilder = new FundingStreamPeriodProfilePatternBuilder();

            setUp?.Invoke(fundingStreamPeriodProfilePatternBuilder);

            return fundingStreamPeriodProfilePatternBuilder.Build();
        }

        private static string NewRandomString() => new RandomString();
    }
}
