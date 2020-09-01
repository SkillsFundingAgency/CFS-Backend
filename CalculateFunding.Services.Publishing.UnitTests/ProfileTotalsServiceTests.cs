using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
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
        private Mock<ISpecificationService> _specificationService;
        private Mock<IPoliciesService> _policiesService;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFunding = new Mock<IPublishedFundingRepository>();
            _specificationService = new Mock<ISpecificationService>();
            _policiesService = new Mock<IPoliciesService>();

            _service = new ProfileTotalsService(
                _publishedFunding.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync() 
                },
                _specificationService.Object,
                _policiesService.Object);
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
        [DynamicData(nameof(MissingIdExamples), DynamicDataSourceType.Method)]
        public void WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine_GuardsAgainstMissingSpecificationId(
            string specificationId)
        {
            Func<Task<IActionResult>> invocation = () => WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
                specificationId,
                NewRandomString(),
                NewRandomString(),
                NewRandomString());

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be(nameof(specificationId));
        }

        [TestMethod]
        [DynamicData(nameof(MissingIdExamples), DynamicDataSourceType.Method)]
        public void WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine_GuardsAgainstMissingProviderId(
            string providerId)
        {
            Func<Task<IActionResult>> invocation = () => WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
                NewRandomString(),
                providerId,
                NewRandomString(),
                NewRandomString());

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
        public void WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine_GuardsAgainstMissingFundingStreamId(
            string fundingStreamId)
        {
            Func<Task<IActionResult>> invocation = () => WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
                NewRandomString(),
                NewRandomString(),
                fundingStreamId,
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
        public void WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine_GuardsAgainstMissingFundingLineId(
    string fundingLineId)
        {
            Func<Task<IActionResult>> invocation = () => WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                fundingLineId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be(nameof(fundingLineId));
        }

        [TestMethod]
        public async Task Returns404ResponseIfNoPublishedProviderBySpecificationIdLocated()
        {
            IActionResult result = await WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                NewRandomString());

            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task Returns404ResponseIfNoProfileVariationPointerLocated()
        {
            string specificationId = NewRandomString();
            string providerId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingLineId = NewRandomString();

            GivenTheLatestPublishedProviderVersionBySpecificationId(
                specificationId,
                providerId,
                fundingStreamId,
                NewPublishedProviderVersion());

            IActionResult result = await WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
                specificationId,
                providerId,
                fundingStreamId,
                fundingLineId);

            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task ConstructsFundingLineProfileWhenValidInputProvided()
        {
            string specificationId = NewRandomString();
            string providerId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingLineId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string providerName = NewRandomString();
            string profilePatternKey = NewRandomString();
            DateTime profileAuditDate = NewRandomDateTime();
            string userId = NewRandomString();
            Reference profileAuditUser = NewReference(u => u.WithId(userId));
            DateTimeOffset fundingDatePatternJune = NewRandomDateTime();
            DateTimeOffset fundingDatePatternJuly = NewRandomDateTime();

            GivenTheLatestPublishedProviderVersionBySpecificationId(
                specificationId,
                fundingStreamId,
                providerId,
                NewPublishedProviderVersion(_ => _.WithFundingPeriodId(fundingPeriodId)
                    .WithProvider(NewProvider(p => p.WithName(providerName)))
                    .WithProfilePatternKeys(
                        NewProfilePatternKeys(ppk => ppk
                            .WithFundingLineCode(fundingLineId)
                            .WithKey(profilePatternKey))
                        .ToArray())
                    .WithCarryOvers(
                        NewProfilingCarryOvers(pco => pco
                            .WithFundingLineCode(fundingLineId)
                            .WithAmount(100))
                        .ToArray())
                    .WithProfilingAudits(
                        NewProfilingAudits(pa => pa
                            .WithFundingLineCode(fundingLineId)
                            .WithDate(profileAuditDate)
                            .WithUser(profileAuditUser)).ToArray())
                    .WithFundingLines(
                        NewFundingLines(fl => fl
                            .WithOrganisationGroupingReason(OrganisationGroupingReason.Payment)
                            .WithFundingLineCode(fundingLineId)
                            .WithValue(1500)
                            .WithDistributionPeriods(
                                NewDistributionPeriod(dp => dp
                                    .WithProfilePeriods(
                                        NewProfilePeriod(pp => pp
                                            .WithType(ProfilePeriodType.CalendarMonth)
                                            .WithYear(2020)
                                            .WithTypeValue("May")
                                            .WithOccurence(1)
                                            .WithAmount(500)))),
                                NewDistributionPeriod(dp => dp
                                    .WithProfilePeriods(
                                        NewProfilePeriod(pp => pp
                                            .WithType(ProfilePeriodType.CalendarMonth)
                                            .WithYear(2020)
                                            .WithTypeValue("June")
                                            .WithOccurence(1)
                                            .WithAmount(1000)))))
                            ).ToArray())));

            GivenProfileVariationPointer(
                specificationId,
                NewProfileVariationPointers(pvp => pvp
                    .WithFundingStreamId(fundingStreamId)
                    .WithFundingLineId(fundingLineId)
                    .WithYear(2020)
                    .WithTypeValue("June")).ToArray());

            GivenFundingDate(
                fundingStreamId,
                fundingPeriodId,
                fundingLineId,
                NewFundingDate(_ => _
                    .WithPatterns(
                        new[] {
                            NewFundingDatePattern(fdp => fdp
                                .WithOccurrence(1)
                                .WithPeriodYear(2020)
                                .WithPeriod("May")
                                .WithPaymentDate(fundingDatePatternJune)),
                            NewFundingDatePattern(fdp => fdp
                                .WithOccurrence(1)
                                .WithPeriodYear(2020)
                                .WithPeriod("June")
                                .WithPaymentDate(fundingDatePatternJuly))})));

            IActionResult result = await WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
                specificationId,
                providerId,
                fundingStreamId,
                fundingLineId);

            result
                .Should()
                .BeOfType<OkObjectResult>()
                .And
                .NotBeNull();

            OkObjectResult objectResult = result as OkObjectResult;

            objectResult
                .Value
                .Should()
                .BeOfType<FundingLineProfile>()
                .And
                .NotBeNull();

            FundingLineProfile actualFundingLineProfile = 
                objectResult.Value as FundingLineProfile;

            FundingLineProfile expectedFundingLineProfile = NewFundingLineProfile(_ => _
                        .WithAmountAlreadyPaid(500)
                        .WithCarryOverAmount(100)
                        .WithLastUpdatedDate(profileAuditDate)
                        .WithLastUpdatedUser(profileAuditUser)
                        .WithProfilePatternKey(profilePatternKey)
                        .WithRemainingAmount(1000)
                        .WithTotalAllocation(1500)
                        .WithProfileTotalAmount(1500)
                        .WithProviderName(providerName)
                        .WithProfileTotals(new[] {
                            NewProfileTotal(pt => pt
                                .WithYear(2020)
                                .WithTypeValue("May")
                                .WithOccurrence(1)
                                .WithValue(500)
                                .WithIsPaid(true)
                                .WithActualDate(fundingDatePatternJune)
                                .WithInstallmentNumber(1)),
                            NewProfileTotal(pt => pt
                                .WithYear(2020)
                                .WithTypeValue("June")
                                .WithOccurrence(1)
                                .WithValue(1000)
                                .WithIsPaid(false)
                                .WithActualDate(fundingDatePatternJuly)
                                .WithInstallmentNumber(2)
                                .WithProfileRemainingPercentage(100))}));

            actualFundingLineProfile
                .Should()
                .BeEquivalentTo(expectedFundingLineProfile);

            actualFundingLineProfile
                .ProfileTotals
                .FirstOrDefault()
                .Should()
                .BeEquivalentTo(expectedFundingLineProfile.ProfileTotals.FirstOrDefault());

            actualFundingLineProfile
                .ProfileTotals
                .LastOrDefault()
                .Should()
                .BeEquivalentTo(expectedFundingLineProfile.ProfileTotals.LastOrDefault());

            _publishedFunding.VerifyAll();
            _specificationService.VerifyAll();
            _policiesService.VerifyAll();
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

        [TestMethod]
        public async Task LocatesAllPublishedProviderVersionsThenGroupsItsProfileValuesAndSumsThem()
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

            GivenAllThePublishedProviderVersions(fundingStreamId, fundingPeriodId, providerId, publishedProviderVersion);

            IActionResult result = await WhenAllTheProfileTotalsAreQueried(fundingStreamId,
                fundingPeriodId,
                providerId);

            OkObjectResult objectResult = result as OkObjectResult;

            objectResult?.Value
                .Should()
                .BeEquivalentTo((new[] { publishedProviderVersion }).ToDictionary(_ => _.Version, 
                    _ => new ProfilingVersion
                    {
                        Date = _.Date,
                        ProfileTotals = new[] { NewProfileTotal(ptbldr => ptbldr.WithOccurrence(1)
                        .WithYear(2012)
                        .WithTypeValue("January")
                        .WithValue(123)) },
                        Version = _.Version
                    }));

            _publishedFunding.VerifyAll();
        }

        private string NewRandomString() => new RandomString();
        private DateTime NewRandomDateTime() => new RandomDateTime();

        private async Task<IActionResult> WhenTheProfileTotalsAreQueried(string fundingStreamId,
            string fundingPeriodId,
            string providerId)
        {
            return await _service.GetPaymentProfileTotalsForFundingStreamForProvider(fundingStreamId,
                fundingPeriodId,
                providerId);
        }

        private async Task<IActionResult> WhenAllTheProfileTotalsAreQueried(string fundingStreamId,
            string fundingPeriodId,
            string providerId)
        {
            return await _service.GetAllReleasedPaymentProfileTotalsForFundingStreamForProvider(fundingStreamId,
                fundingPeriodId,
                providerId);
        }

        private async Task<IActionResult> WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
            string specificationId,
            string providerId,
            string fundingStreamId,
            string fundingLineId)
        {
            return await _service.GetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
                specificationId,
                providerId,
                fundingStreamId,
                fundingLineId);
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

        private void GivenTheLatestPublishedProviderVersionBySpecificationId(
            string specificationId,
            string fundingStreamId,
            string providerId,
            PublishedProviderVersion publishedProviderVersion)
        {
            _publishedFunding
                .Setup(_ => _.GetLatestPublishedProviderVersionBySpecificationId(
                    specificationId,
                    fundingStreamId,
                    providerId))
                .ReturnsAsync(publishedProviderVersion)
                .Verifiable();
        }

        private void GivenProfileVariationPointer(
            string specificationId,
            IEnumerable<ProfileVariationPointer> profileVariationPointers)
        {
            _specificationService
                .Setup(_ => _.GetProfileVariationPointers(specificationId))
                .ReturnsAsync(profileVariationPointers)
                .Verifiable();
        }

        private void GivenFundingDate(
            string fundingStreamId,
            string fundingPeriodId,
            string fundingLineId,
            FundingDate fundingDate)
        {
            _policiesService
                .Setup(_ => _.GetFundingDate(fundingStreamId, fundingPeriodId, fundingLineId))
                .ReturnsAsync(fundingDate)
                .Verifiable();
        }

        private void GivenAllThePublishedProviderVersions(string fundingStreamId,
            string fundingPeriodId,
            string providerId,
            PublishedProviderVersion publishedProviderVersion)
        {
            _publishedFunding.Setup(_ => _.GetPublishedProviderVersions(fundingStreamId,
                    fundingPeriodId,
                    providerId,
                    "Released"))
                .ReturnsAsync(new[] { publishedProviderVersion })
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

        private FundingLineProfile NewProfileTotal(Action<FundingLineProfileBuilder> setUp = null)
        {
            FundingLineProfileBuilder fundingLineProfileBuilder = new FundingLineProfileBuilder();

            setUp?.Invoke(fundingLineProfileBuilder);

            return fundingLineProfileBuilder.Build();
        }
    }
}