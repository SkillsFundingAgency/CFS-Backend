using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.UnitTests.Profiling;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class ProfileTotalsServiceTests : ProfilingTestBase
    {
        private ProfileTotalsService _service;
        private Mock<IPublishedFundingRepository> _publishedFunding;
        
        [TestInitialize]
        public void SetUp()
        {
            _publishedFunding = new Mock<IPublishedFundingRepository>();
            
            _service = new ProfileTotalsService(_publishedFunding.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync() 
                });
        }

        [TestMethod]
        [DynamicData(nameof(MissingIdExamples), DynamicDataSourceType.Method)]
        public void GuardsAgainstMissingFundingStreamId(string fundingStreamId)
        {
            Func<Task<IActionResult>> invocation = () => WhenTheProfileTotalsAreQueried(fundingStreamId,
                NewRandomString(),
                NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be(nameof(fundingStreamId));
        }
        
        [TestMethod]
        [DynamicData(nameof(MissingIdExamples), DynamicDataSourceType.Method)]
        public void GuardsAgainstMissingProviderId(string providerId)
        {
            Func<Task<IActionResult>> invocation = () => WhenTheProfileTotalsAreQueried(NewRandomString(),
                NewRandomString(),
                providerId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be(nameof(providerId));
        }
        
        [TestMethod]
        [DynamicData(nameof(MissingIdExamples), DynamicDataSourceType.Method)]
        public void GuardsAgainstMissingFundingPeriodId(string fundingPeriodId)
        {
            Func<Task<IActionResult>> invocation = () => WhenTheProfileTotalsAreQueried(NewRandomString(),
                fundingPeriodId,
                NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be(nameof(fundingPeriodId));
        }

        [TestMethod]
        public async Task Returns404ResponseIfNoPublishedProviderLocated()
        {
            IActionResult result = await WhenTheProfileTotalsAreQueried(NewRandomString(),
                NewRandomString(),
                NewRandomString());

            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task LocatesPublishedProviderVersionThenGroupsItsProfileValuesAndSumsThem()
        {
            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ =>
                _.WithFundingLines(NewFundingLine(fl => fl.WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                    .WithDistributionPeriods(NewDistributionPeriod(dp => dp.WithProfilePeriods(
                        NewProfilePeriod(pp => pp.WithAmount(123)
                            .WithTypeValue("January")
                            .WithYear(2012)
                            .WithOccurence(1))))))));

            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string providerId = NewRandomString();
            
            GivenThePublishedProviderVersion(fundingStreamId, fundingPeriodId, providerId, publishedProviderVersion);

            IActionResult result = await WhenTheProfileTotalsAreQueried(fundingStreamId, 
                fundingPeriodId, 
                providerId);
            
            OkObjectResult objectResult = result as OkObjectResult;
            
            objectResult?.Value
                .Should()
                .BeEquivalentTo(new [] { NewProfileTotal(_ => _.WithOccurrence(1)
                    .WithYear(2012)
                    .WithTypeValue("January")
                    .WithValue(123)) });
                
            _publishedFunding.VerifyAll();
        }

        private string NewRandomString() => new RandomString();
        private async Task<IActionResult> WhenTheProfileTotalsAreQueried(string fundingStreamId,
            string fundingPeriodId,
            string providerId)
        {
            return await _service.GetPaymentProfileTotalsForFundingStreamForProvider(fundingStreamId,
                fundingPeriodId,
                providerId);
        }

        private void GivenThePublishedProviderVersion(string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            PublishedProviderVersion publishedProviderVersion)
        {
            _publishedFunding.Setup(_ => _.GetLatestPublishedProviderVersion(fundingStreamId,
                    fundingPeriodId,
                    providerId))
                .ReturnsAsync(publishedProviderVersion)
                .Verifiable();
        }
        
        private static IEnumerable<object[]> MissingIdExamples()
        {
            yield return new object[] {null};
            yield return new object[] {""};
            yield return new object[] {string.Empty};
        }
        
        private ProfileTotal NewProfileTotal(Action<ProfileTotalBuilder> setUp = null)
        {
            ProfileTotalBuilder profileTotalBuilder = new ProfileTotalBuilder();
            
            setUp?.Invoke(profileTotalBuilder);
            
            return profileTotalBuilder
                .Build();
        }
    }
}