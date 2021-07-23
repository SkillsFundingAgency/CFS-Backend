using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Profiling;
using CalculateFunding.Services.Publishing.UnitTests.Errors;
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
        private Mock<IProfilingService> _profilingService;

        [TestInitialize]
        public void SetUp()
        {
            _publishedFunding = new Mock<IPublishedFundingRepository>();
            _specificationService = new Mock<ISpecificationService>();
            _policiesService = new Mock<IPoliciesService>();
            _profilingService = new Mock<IProfilingService>();

            _service = new ProfileTotalsService(
                _publishedFunding.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync(),
                    ProfilingApiClient = Policy.NoOpAsync()
                },
                _specificationService.Object,
                _policiesService.Object,
                _profilingService.Object);
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
            Func<Task<ActionResult<FundingLineProfile>>> invocation = () => WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
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
            Func<Task<ActionResult<FundingLineProfile>>> invocation = () => WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
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
            Func<Task<ActionResult<FundingLineProfile>>> invocation = () => WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
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
            string fundingLineCode)
        {
            Func<Task<ActionResult<FundingLineProfile>>> invocation = () => WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                fundingLineCode);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be(nameof(fundingLineCode));
        }

        [TestMethod]
        public async Task Returns404IfNoPublishedProviderBySpecificationIdLocated()
        {
            ActionResult<FundingLineProfile> result = await WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                NewRandomString());

            result
                .Result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task Returns404IfNoProfileVariationPointerLocated()
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

            ActionResult<FundingLineProfile> result = await WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
                specificationId,
                providerId,
                fundingStreamId,
                fundingLineId);

            result
                .Result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task ConstructsFundingLineProfileWhenValidInputProvided()
        {
            string specificationId = NewRandomString();
            string providerId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string distributionPeriodId1 = NewRandomString();
            string distributionPeriodId2 = NewRandomString();
            string providerName = NewRandomString();
            string UKPRN = NewRandomString();
            string templateVersion = NewRandomString();
            string fundingLineName = NewRandomString();
            string profilePatternKey = NewRandomString();
            string profilePatternDisplayName = NewRandomString();
            string profilePatternDescription = NewRandomString();
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
                    .WithFundingStreamId(fundingStreamId)
                    .WithProviderId(providerId)
                    .WithProvider(NewProvider(p => p.WithName(providerName).WithUKPRN(UKPRN)))
                    .WithTemplateVersion(templateVersion)
                    .WithProfilePatternKeys(
                        NewProfilePatternKeys(ppk => ppk
                                .WithFundingLineCode(fundingLineCode)
                                .WithKey(profilePatternKey))
                            .ToArray())
                    .WithCarryOvers(
                        NewProfilingCarryOvers(pco => pco
                                .WithFundingLineCode(fundingLineCode)
                                .WithAmount(100))
                            .ToArray())
                    .WithProfilingAudits(
                        NewProfilingAudits(pa => pa
                            .WithFundingLineCode(fundingLineCode)
                            .WithDate(profileAuditDate)
                            .WithUser(profileAuditUser)).ToArray())
                    .WithFundingLines(
                        NewFundingLines(fl => fl
                            .WithFundingLineType(FundingLineType.Payment)
                            .WithFundingLineCode(fundingLineCode)
                            .WithValue(1500)
                            .WithDistributionPeriods(
                                NewDistributionPeriod(dp => dp
                                    .WithProfilePeriods(
                                        NewProfilePeriod(pp => pp
                                            .WithType(ProfilePeriodType.CalendarMonth)
                                            .WithYear(2020)
                                            .WithTypeValue("May")
                                            .WithOccurence(1)
                                            .WithDistributionPeriodId(distributionPeriodId1)
                                            .WithAmount(500)))),
                                NewDistributionPeriod(dp => dp
                                    .WithProfilePeriods(
                                        NewProfilePeriod(pp => pp
                                            .WithType(ProfilePeriodType.CalendarMonth)
                                            .WithYear(2020)
                                            .WithTypeValue("June")
                                            .WithOccurence(1)
                                            .WithDistributionPeriodId(distributionPeriodId2)
                                            .WithAmount(1000)))))
                        ).ToArray())));

            GivenProfileVariationPointer(
                specificationId,
                NewProfileVariationPointers(pvp => pvp
                    .WithFundingStreamId(fundingStreamId)
                    .WithFundingLineId(fundingLineCode)
                    .WithYear(2020)
                    .WithTypeValue("June")).ToArray());

            GivenFundingDate(
                fundingStreamId,
                fundingPeriodId,
                fundingLineCode,
                NewFundingDate(_ => _
                    .WithPatterns(
                        new[]
                        {
                            NewFundingDatePattern(fdp => fdp
                                .WithOccurrence(1)
                                .WithPeriodYear(2020)
                                .WithPeriod("May")
                                .WithPaymentDate(fundingDatePatternJune)),
                            NewFundingDatePattern(fdp => fdp
                                .WithOccurrence(1)
                                .WithPeriodYear(2020)
                                .WithPeriod("June")
                                .WithPaymentDate(fundingDatePatternJuly))
                        })));

            GivenDistinctTemplateMetadataFundingLinesContents(
                fundingStreamId,
                fundingPeriodId,
                templateVersion,
                new TemplateMetadataDistinctFundingLinesContents
                {
                    FundingLines = new[]
                    {
                        new TemplateMetadataFundingLine
                        {
                            FundingLineCode = fundingLineCode,
                            Name = fundingLineName
                        }
                    }
                });

            GivenGetProfilePatternsForFundingStreamAndFundingPeriod(
                fundingStreamId,
                fundingPeriodId,
                new List<FundingStreamPeriodProfilePattern>
                {
                    new FundingStreamPeriodProfilePattern
                    {
                        FundingLineId = fundingLineCode,
                        ProfilePatternKey = profilePatternKey,
                        ProfilePatternDisplayName = profilePatternDisplayName,
                        ProfilePatternDescription = profilePatternDescription
                    }
                }
            );

            ActionResult<FundingLineProfile> result = await WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
                specificationId,
                providerId,
                fundingStreamId,
                fundingLineCode);

            result
                .Should()
                .BeOfType<ActionResult<FundingLineProfile>>()
                .And
                .NotBeNull();

            result
                .Value
                .Should()
                .BeOfType<FundingLineProfile>()
                .And
                .NotBeNull();

            FundingLineProfile actualFundingLineProfile =
                result.Value as FundingLineProfile;

            FundingLineProfile expectedFundingLineProfile = NewFundingLineProfile(_ => _
                .WithAmountAlreadyPaid(500)
                .WithCarryOverAmount(100)
                .WithLastUpdatedDate(profileAuditDate)
                .WithLastUpdatedUser(profileAuditUser)
                .WithProfilePatternKey(profilePatternKey)
                .WithProfilePatternName(profilePatternDisplayName)
                .WithProfilePatternDescription(profilePatternDescription)
                .WithRemainingAmount(1000)
                .WithProfilePatternTotal(1500)
                .WithProfileTotalAmount(1500)
                .WithProfilePatternTotalWithCarryOver(1600)
                .WithProviderName(providerName)
                .WithProviderId(providerId)
                .WithUKPRN(UKPRN)
                .WithFundingLineName(fundingLineName)
                .WithFundingLineAmount(1500)
                .WithFundingLineCode(fundingLineCode)
                .WithProfileTotals(new[]
                {
                    NewProfileTotal(pt => pt
                        .WithYear(2020)
                        .WithTypeValue("May")
                        .WithPeriodType("CalendarMonth")
                        .WithOccurrence(1)
                        .WithValue(500)
                        .WithIsPaid(true)
                        .WithActualDate(fundingDatePatternJune)
                        .WithDistributionPeriod(distributionPeriodId1)
                        .WithInstallmentNumber(1)),
                    NewProfileTotal(pt => pt
                        .WithYear(2020)
                        .WithTypeValue("June")
                        .WithPeriodType("CalendarMonth")
                        .WithOccurrence(1)
                        .WithValue(1000)
                        .WithIsPaid(false)
                        .WithActualDate(fundingDatePatternJuly)
                        .WithInstallmentNumber(2)
                        .WithDistributionPeriod(distributionPeriodId2)
                        .WithProfileRemainingPercentage(100))
                }));

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

            actualFundingLineProfile
                .IsCustomProfile
                .Should()
                .BeFalse();

            _publishedFunding.VerifyAll();
            _specificationService.VerifyAll();
            _policiesService.VerifyAll();
        }

        [TestMethod]
        public async Task LocatesPublishedProviderVersionThenGroupsItsProfileValuesAndSumsThem()
        {
            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ =>
                _.WithFundingLines(NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment)
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
                .BeEquivalentTo(new[]
                {
                    NewProfileTotal(_ => _.WithOccurrence(1)
                        .WithYear(2012)
                        .WithTypeValue("January")
                        .WithValue(123))
                });

            _publishedFunding.VerifyAll();
        }

        [TestMethod]
        public async Task Returns404WhenNoReleasedPublishedProviderResultsForTheParameters()
        {
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string providerId = NewRandomString();


            IActionResult result = await WhenTheProfileTotalsAreQueried(fundingStreamId,
                fundingPeriodId,
                providerId);

            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task LocatesAllPublishedProviderVersionsThenGroupsItsProfileValuesAndSumsThem()
        {
            PublishedProviderVersion publishedProviderVersion = NewPublishedProviderVersion(_ =>
                _.WithFundingLines(NewFundingLine(fl => fl.WithFundingLineType(FundingLineType.Payment)
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
                .BeEquivalentTo(new[]
                {
                    publishedProviderVersion
                }.ToDictionary(_ => _.Version,
                    _ => new ProfilingVersion
                    {
                        Date = _.Date,
                        ProfileTotals = new[]
                        {
                            NewProfileTotal(ptbldr => ptbldr.WithOccurrence(1)
                                .WithYear(2012)
                                .WithTypeValue("January")
                                .WithValue(123))
                        },
                        Version = _.Version
                    }));

            _publishedFunding.VerifyAll();
        }

        [TestMethod]
        public async Task Returns404IfNoPublishedProviderForApprovalLocatedForPreviousRecordsExistsCheck()
        {
            IActionResult result = await WhenTheProfilesCheckForPreviousRecordExists(
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                NewRandomString());

            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task LocatesAllPublishedProviderVersionProfileChangesWhenCarryOverAmountChanged()
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string providerId = NewRandomString();

            IEnumerable<PublishedProviderVersion> publishedProviderVersions = new[]
            {
                NewPublishedProviderVersion(_ =>
                    _.WithCarryOvers(NewProfilingCarryOver(co => co.WithAmount(1).WithFundingLineCode(fundingLineCode)))),
                NewPublishedProviderVersion(_ =>
                    _.WithCarryOvers(NewProfilingCarryOver(co => co.WithAmount(2).WithFundingLineCode(fundingLineCode))))
            };

            GivenPublishedProviderVersionsForApproval(specificationId, providerId, fundingStreamId, publishedProviderVersions);

            IActionResult result = await WhenTheProfilesCheckForPreviousRecordExists(
                specificationId,
                providerId,
                fundingStreamId,
                fundingLineCode);

            OkObjectResult objectResult = result as OkObjectResult;

            objectResult?.Value
                .Should()
                .BeEquivalentTo(true);
        }

        [TestMethod]
        public async Task LocatesAllPublishedProviderVersionProfileChangesWhenFundingLineTotalAmountChanged()
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string providerId = NewRandomString();

            IEnumerable<PublishedProviderVersion> publishedProviderVersions = new[]
            {
                NewPublishedProviderVersion(_ =>
                    _.WithFundingLines(NewFundingLine(fl => fl.WithValue(1).WithFundingLineCode(fundingLineCode)))),
                NewPublishedProviderVersion(_ =>
                    _.WithFundingLines(NewFundingLine(fl => fl.WithValue(2).WithFundingLineCode(fundingLineCode))))
            };

            GivenPublishedProviderVersionsForApproval(specificationId, providerId, fundingStreamId, publishedProviderVersions);

            IActionResult result = await WhenTheProfilesCheckForPreviousRecordExists(
                specificationId,
                providerId,
                fundingStreamId,
                fundingLineCode);

            OkObjectResult objectResult = result as OkObjectResult;

            objectResult?.Value
                .Should()
                .BeEquivalentTo(true);
        }

        [TestMethod]
        public async Task LocatesNoPublishedProviderVersionProfileChangesWhenFundingLineTotalAmountAndCarryOverAmounNotChanged()
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string providerId = NewRandomString();

            IEnumerable<PublishedProviderVersion> publishedProviderVersions = new[]
            {
                NewPublishedProviderVersion(_ => _
                    .WithFundingLines(NewFundingLine(fl => fl.WithValue(100).WithFundingLineCode(fundingLineCode)))
                    .WithCarryOvers(NewProfilingCarryOver(co => co.WithAmount(1000).WithFundingLineCode(fundingLineCode)))),
                NewPublishedProviderVersion(_ => _
                    .WithFundingLines(NewFundingLine(fl => fl.WithValue(100).WithFundingLineCode(fundingLineCode)))
                    .WithCarryOvers(NewProfilingCarryOver(co => co.WithAmount(1000).WithFundingLineCode(fundingLineCode))))
            };

            GivenPublishedProviderVersionsForApproval(specificationId, providerId, fundingStreamId, publishedProviderVersions);

            IActionResult result = await WhenTheProfilesCheckForPreviousRecordExists(
                specificationId,
                providerId,
                fundingStreamId,
                fundingLineCode);

            OkObjectResult objectResult = result as OkObjectResult;

            objectResult?.Value
                .Should()
                .BeEquivalentTo(false);
        }

        [TestMethod]
        public async Task Returns404IfNoPublishedProviderForApprovalLocatedForPreviousRecords()
        {
            IActionResult result = await WhenTheProfilesCheckForPreviousRecords(
                NewRandomString(),
                NewRandomString(),
                NewRandomString(),
                NewRandomString());

            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task ReturnsPublishedProviderVersionProfileChangesWhenFundingLineTotalAmountAndCarryOverAmounChanged()
        {
            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string fundingLineName = NewRandomString();
            string providerId = NewRandomString();
            string fundingStreamName = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string templateVersion = NewRandomString();
            string distribtionPeriod1 = NewRandomString();
            string distribtionPeriod2 = NewRandomString();

            DateTimeOffset paymentDate = NewRandomDateTime();
            Reference user = NewReference();

            IEnumerable<PublishedProviderVersion> publishedProviderVersions = new[]
            {
                NewPublishedProviderVersion(_ => _
                    .WithFundingLines(NewFundingLine(fl => fl
                        .WithValue(200)
                        .WithFundingLineType(FundingLineType.Payment)
                        .WithFundingLineCode(fundingLineCode)
                        .WithDistributionPeriods(NewDistributionPeriod(dp => dp.WithProfilePeriods(NewProfilePeriod(pp => pp
                            .WithOccurence(1)
                            .WithTypeValue("August")
                            .WithYear(2020)
                            .WithType(ProfilePeriodType.CalendarMonth)
                            .WithDistributionPeriodId(distribtionPeriod1)
                            .WithAmount(200)))))))
                    .WithCarryOvers(NewProfilingCarryOver(co => co.WithAmount(2000).WithFundingLineCode(fundingLineCode)))
                    .WithFundingStreamId(fundingStreamId)
                    .WithDate("2020-01-02T00:00:00")
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithTemplateVersion(templateVersion)
                    .WithProfilingAudits(NewProfilingAudit(pa => pa
                        .WithFundingLineCode(fundingLineCode)
                        .WithDate(DateTime.Parse("2020-01-02T01:00:00"))
                        .WithUser(user)))),
                NewPublishedProviderVersion(_ => _
                    .WithFundingLines(NewFundingLine(fl => fl.WithValue(100).WithFundingLineCode(fundingLineCode)))
                    .WithCarryOvers(NewProfilingCarryOver(co => co.WithAmount(1000).WithFundingLineCode(fundingLineCode)))
                    .WithFundingStreamId(fundingStreamId)
                    .WithDate("2020-01-01T00:00:00"))
            };

            GivenPublishedProviderVersionsForApproval(specificationId, providerId, fundingStreamId, publishedProviderVersions);
            GivenFundingStreams(new[]
            {
                NewFundingStream(_ => _.WithId(fundingStreamId).WithName(fundingStreamName))
            });
            GivenFundingDate(
                fundingStreamId,
                fundingPeriodId,
                fundingLineCode,
                NewFundingDate(_ => _.WithPatterns(new[]
                {
                    NewFundingDatePattern(fdp => fdp.WithOccurrence(1).WithPeriod("August").WithPeriodYear(2020).WithPaymentDate(paymentDate))
                })));
            GivenDistinctTemplateMetadataFundingLinesContents(
                fundingStreamId,
                fundingPeriodId,
                templateVersion,
                new TemplateMetadataDistinctFundingLinesContents
                {
                    FundingLines = new[]
                    {
                        new TemplateMetadataFundingLine
                        {
                            FundingLineCode = fundingLineCode,
                            Name = fundingLineName
                        }
                    }
                });

            IActionResult result = await WhenTheProfilesCheckForPreviousRecords(
                specificationId,
                providerId,
                fundingStreamId,
                fundingLineCode);

            OkObjectResult objectResult = result as OkObjectResult;

            objectResult?.Value
                .Should()
                .BeOfType<List<FundingLineChange>>();

            List<FundingLineChange> fundingLineChanges = objectResult.Value as List<FundingLineChange>;

            (fundingLineChanges?.Count).GetValueOrDefault().Should().Be(1);

            FundingLineChange fundingLineChange = fundingLineChanges?.FirstOrDefault();

            FundingLineChange expectedFundingLineChange = NewFundingLineChange(_ => _
                .WithFundingLineTotal(200)
                .WithPreviousFundingLineTotal(100)
                .WithFundingStreamName(fundingStreamName)
                .WithCarryOverAmount(2000)
                .WithLastUpdatedUser(user)
                .WithLastUpdatedDate(DateTime.Parse("2020-01-02T01:00:00"))
                .WithFundingLineName(fundingLineName)
                .WithProfileTotals(new[]
                {
                    NewProfileTotal(fp => fp
                        .WithOccurrence(1)
                        .WithTypeValue("August")
                        .WithYear(2020)
                        .WithValue(200)
                        .WithInstallmentNumber(1)
                        .WithActualDate(paymentDate)
                        .WithDistributionPeriod(distribtionPeriod1)
                        .WithPeriodType(ProfilePeriodType.CalendarMonth.ToString()))
                }));

            fundingLineChange.Should().BeEquivalentTo(expectedFundingLineChange);

            fundingLineChange
                .ProfileTotals
                .FirstOrDefault()
                .Should()
                .BeEquivalentTo(expectedFundingLineChange.ProfileTotals.FirstOrDefault());
        }

        [TestMethod]
        [DynamicData(nameof(MissingIdExamples), DynamicDataSourceType.Method)]
        public void WhenGetCurrentProfileConfig_GuardsAgainstMissingSpecificationId(
            string specificationId)
        {
            Func<Task<IActionResult>> invocation = () => WhenGetCurrentProfileConfig(
                specificationId,
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
        public void WhenGetCurrentProfileConfig_GuardsAgainstMissingProviderId(
            string providerId)
        {
            Func<Task<IActionResult>> invocation = () => WhenGetCurrentProfileConfig(
                NewRandomString(),
                providerId,
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
        public void WhenGetCurrentProfileConfig_GuardsAgainstMissingFundingStreamId(
            string fundingStreamId)
        {
            Func<Task<IActionResult>> invocation = () => WhenGetCurrentProfileConfig(
                NewRandomString(),
                NewRandomString(),
                fundingStreamId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .And
                .ParamName
                .Should()
                .Be(nameof(fundingStreamId));
        }

        [TestMethod]
        public async Task Returns404IfCurrentProfileConfigNoPublishedProviderBySpecificationIdLocated()
        {
            IActionResult result = await WhenGetCurrentProfileConfig(
                NewRandomString(),
                NewRandomString(),
                NewRandomString());

            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task ReturnsIsCustomProfile()
        {
            IActionResult result = await WhenGetCurrentProfileConfig(
                NewRandomString(),
                NewRandomString(),
                NewRandomString());

            result
                .Should()
                .BeOfType<NotFoundResult>();
        }

        [TestMethod]
        public async Task ConstructsFundingLineProfileWhenValidInputProvidedToCurrentProfileConfig()
        {
            string specificationId = NewRandomString();
            string providerId = NewRandomString();
            string providerName = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingLineCode = NewRandomString();
            string customProfileFundingLineCode = NewRandomString();
            string fundingLineName = NewRandomString();
            string customProfileFundingLineName = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string profilePatternKey = NewRandomString();
            DateTime profileAuditDate = NewRandomDateTime();
            string userId = NewRandomString();
            Reference profileAuditUser = NewReference(u => u.WithId(userId));
            string templateVersion = NewRandomString();
            string profilePatternDisplayName = NewRandomString();
            string profilePatternDisplayDescription = NewRandomString();

            string distributionPeriod1 = NewRandomString();
            string distributionPeriod2 = NewRandomString();

            PublishedProviderError expectedError = NewPublishedProviderError(err =>
            err.WithIdentifier(fundingLineCode)
            .WithFundingLine(fundingLineCode)
            .WithFundingStreamId(fundingStreamId));

            GivenTheLatestPublishedProviderVersionBySpecificationId(
                specificationId,
                fundingStreamId,
                providerId,
                NewPublishedProviderVersion(_ => _
                    .WithFundingStreamId(fundingStreamId)
                    .WithFundingPeriodId(fundingPeriodId)
                    .WithTemplateVersion(templateVersion)
                    .WithProvider(NewProvider(p => p.WithName(providerName).WithProviderId(providerId).WithUKPRN(providerId)))
                    .WithErrors(expectedError,
                        NewPublishedProviderError())
                    .WithProfilePatternKeys(
                        NewProfilePatternKeys(ppk => ppk
                                .WithFundingLineCode(fundingLineCode)
                                .WithKey(profilePatternKey))
                            .ToArray())
                    .WithCustomProfiles(NewFundingLineOverrides(cp => cp.WithFundingLineCode(customProfileFundingLineCode)))
                    .WithCarryOvers(
                        NewProfilingCarryOvers(pco => pco
                                .WithFundingLineCode(fundingLineCode)
                                .WithAmount(100))
                            .ToArray())
                    .WithProfilingAudits(
                        NewProfilingAudits(pa => pa
                            .WithFundingLineCode(fundingLineCode)
                            .WithDate(profileAuditDate)
                            .WithUser(profileAuditUser)).ToArray())
                    .WithFundingLines(
                        NewFundingLines(fl => fl
                            .WithFundingLineType(FundingLineType.Payment)
                            .WithFundingLineCode(fundingLineCode)
                            .WithValue(1500)
                            .WithDistributionPeriods(
                                NewDistributionPeriod(dp => dp
                                    .WithProfilePeriods(
                                        NewProfilePeriod(pp => pp
                                            .WithType(ProfilePeriodType.CalendarMonth)
                                            .WithYear(2020)
                                            .WithTypeValue("May")
                                            .WithOccurence(1)
                                            .WithAmount(500)
                                            .WithDistributionPeriodId(distributionPeriod1)
                                            .WithType(ProfilePeriodType.CalendarMonth)))),
                                NewDistributionPeriod(dp => dp
                                    .WithProfilePeriods(
                                        NewProfilePeriod(pp => pp
                                            .WithType(ProfilePeriodType.CalendarMonth)
                                            .WithYear(2020)
                                            .WithTypeValue("June")
                                            .WithOccurence(1)
                                            .WithAmount(1000)
                                            .WithDistributionPeriodId(distributionPeriod2)
                                            .WithType(ProfilePeriodType.CalendarMonth))))),
                            f2 => f2
                            .WithFundingLineType(FundingLineType.Payment)
                            .WithFundingLineCode(customProfileFundingLineCode)
                            .WithValue(1500)
                            .WithDistributionPeriods(
                                NewDistributionPeriod(dp => dp
                                    .WithProfilePeriods(
                                        NewProfilePeriod(pp => pp
                                            .WithType(ProfilePeriodType.CalendarMonth)
                                            .WithYear(2020)
                                            .WithTypeValue("May")
                                            .WithOccurence(1)
                                            .WithAmount(500)
                                            .WithDistributionPeriodId(distributionPeriod1)
                                            .WithType(ProfilePeriodType.CalendarMonth)))),
                                NewDistributionPeriod(dp => dp
                                    .WithProfilePeriods(
                                        NewProfilePeriod(pp => pp
                                            .WithType(ProfilePeriodType.CalendarMonth)
                                            .WithYear(2020)
                                            .WithTypeValue("June")
                                            .WithOccurence(1)
                                            .WithAmount(1000)
                                            .WithDistributionPeriodId(distributionPeriod2)
                                            .WithType(ProfilePeriodType.CalendarMonth)))))
                        ).ToArray())));

            GivenGetDistinctTemplateMetadataFundingLinesContents(
                fundingStreamId,
                fundingPeriodId,
                templateVersion,
                new TemplateMetadataDistinctFundingLinesContents
                {
                    FundingLines = new List<TemplateMetadataFundingLine>
                    {
                        new TemplateMetadataFundingLine
                        {
                            FundingLineCode = fundingLineCode,
                            Name = fundingLineName
                        },
                         new TemplateMetadataFundingLine
                        {
                            FundingLineCode = customProfileFundingLineCode,
                            Name = customProfileFundingLineName
                        }
                    }
                });

            GivenGetProfilePatternsForFundingStreamAndFundingPeriod(
                fundingStreamId,
                fundingPeriodId,
                new List<FundingStreamPeriodProfilePattern>
                {
                    new FundingStreamPeriodProfilePattern
                    {
                        FundingLineId = fundingLineCode,
                        ProfilePatternKey = profilePatternKey,
                        ProfilePatternDisplayName = profilePatternDisplayName,
                        ProfilePatternDescription = profilePatternDisplayDescription
                    },
                    new FundingStreamPeriodProfilePattern
                    {
                        FundingLineId = customProfileFundingLineCode,
                        ProfilePatternKey = null
                    }
                }
            );

            IActionResult result = await WhenGetCurrentProfileConfig(
                specificationId,
                providerId,
                fundingStreamId);

            result
                .Should()
                .BeOfType<OkObjectResult>()
                .And
                .NotBeNull();

            OkObjectResult objectResult = result as OkObjectResult;

            objectResult
                .Value
                .Should()
                .BeOfType<List<FundingLineProfile>>()
                .And
                .NotBeNull();

            List<FundingLineProfile> actualFundingLineProfiles =
                objectResult.Value as List<FundingLineProfile>;

            actualFundingLineProfiles
                .Count()
                .Should()
                .Be(2);

            FundingLineProfile actualFundingLineProfile =
                actualFundingLineProfiles.FirstOrDefault(x => x.FundingLineCode == fundingLineCode);

            actualFundingLineProfile
                .Should()
                .NotBeNull();

            FundingLineProfile expectedFundingLineProfile = NewFundingLineProfile(_ => _
                .WithFundingLineCode(fundingLineCode)
                .WithFundingLineName(fundingLineName)
                .WithCarryOverAmount(100)
                .WithLastUpdatedUser(profileAuditUser)
                .WithLastUpdatedDate(profileAuditDate)
                .WithProfilePatternKey(profilePatternKey)
                .WithProfilePatternName(profilePatternDisplayName)
                .WithProfilePatternDescription(profilePatternDisplayDescription)
                .WithFundingLineAmount(1600)
                .WithProfileTotalAmount(1500)
                .WithProfilePatternTotal(1500)
                .WithErrors(expectedError)
                .WithFundingLineAmount(1500)
                .WithRemainingAmount(1500)
                .WithProviderId(providerId)
                .WithUKPRN(providerId)
                .WithProviderName(providerName)
                .WithProfilePatternTotalWithCarryOver(1600)
                .WithCustomProfile(false)
                .WithProfileTotals(new[]
                {
                    NewProfileTotal(pt => pt
                        .WithYear(2020)
                        .WithTypeValue("May")
                        .WithOccurrence(1)
                        .WithValue(500)
                        .WithDistributionPeriod(distributionPeriod1)
                        .WithInstallmentNumber(1)
                        .WithProfileRemainingPercentage(33.333333333333333333333333330M)
                        .WithPeriodType(ProfilePeriodType.CalendarMonth.ToString())),
                    NewProfileTotal(pt => pt
                        .WithYear(2020)
                        .WithTypeValue("June")
                        .WithOccurrence(1)
                        .WithInstallmentNumber(2)
                        .WithValue(1000)
                        .WithDistributionPeriod(distributionPeriod2)
                        .WithProfileRemainingPercentage(66.666666666666666666666666670M)
                        .WithPeriodType(ProfilePeriodType.CalendarMonth.ToString()))
                }));

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

            actualFundingLineProfile
                .IsCustomProfile
                .Should()
                .BeFalse();

            FundingLineProfile actualFundingLineCustomProfile =
                actualFundingLineProfiles.FirstOrDefault(x => x.FundingLineCode == customProfileFundingLineCode);

            actualFundingLineCustomProfile
                .Should()
                .NotBeNull();

            actualFundingLineCustomProfile.ProfilePatternName
                .Should()
                .Be("Custom Profile");

            actualFundingLineCustomProfile.ProfilePatternDescription
                .Should()
                .Be("Custom Profile");

            actualFundingLineCustomProfile.IsCustomProfile
                .Should()
                .BeTrue();

            _publishedFunding.VerifyAll();
            _specificationService.VerifyAll();
            _policiesService.VerifyAll();
        }

        private async Task<IActionResult> WhenTheProfileTotalsAreQueried(string fundingStreamId,
            string fundingPeriodId,
            string providerId) =>
            await _service.GetPaymentProfileTotalsForFundingStreamForProvider(fundingStreamId,
                fundingPeriodId,
                providerId);

        private async Task<IActionResult> WhenTheProfilesCheckForPreviousRecordExists(
            string specificationId,
            string providerId,
            string fundingStreamId,
            string fundingLineCode) =>
            await _service.PreviousProfileExistsForSpecificationForProviderForFundingLine(
                specificationId,
                providerId,
                fundingStreamId,
                fundingLineCode);

        private async Task<IActionResult> WhenTheProfilesCheckForPreviousRecords(
            string specificationId,
            string providerId,
            string fundingStreamId,
            string fundingLineCode) =>
            await _service.GetPreviousProfilesForSpecificationForProviderForFundingLine(
                specificationId,
                providerId,
                fundingStreamId,
                fundingLineCode);

        private async Task<IActionResult> WhenAllTheProfileTotalsAreQueried(string fundingStreamId,
            string fundingPeriodId,
            string providerId) =>
            await _service.GetAllReleasedPaymentProfileTotalsForFundingStreamForProvider(fundingStreamId,
                fundingPeriodId,
                providerId);

        private async Task<ActionResult<FundingLineProfile>> WhenGetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
            string specificationId,
            string providerId,
            string fundingStreamId,
            string fundingLineCode) =>
            await _service.GetPublishedProviderProfileTotalsForSpecificationForProviderForFundingLine(
                specificationId,
                providerId,
                fundingStreamId,
                fundingLineCode);

        private async Task<IActionResult> WhenGetCurrentProfileConfig(
            string specificationId,
            string providerId,
            string fundingStreamId) =>
            await _service.GetCurrentProfileConfig(
                specificationId,
                providerId,
                fundingStreamId);

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

        private void GivenGetProfilePatternsForFundingStreamAndFundingPeriod(
            string fundingStreamId,
            string fundingPeriodId,
            IEnumerable<FundingStreamPeriodProfilePattern> fundingStreamPeriodProfilePatterns)
        {
            _profilingService
                .Setup(_ => _.GetProfilePatternsForFundingStreamAndFundingPeriod(
                    fundingStreamId,
                    fundingPeriodId
                    ))
                .ReturnsAsync(fundingStreamPeriodProfilePatterns)
                .Verifiable();
        }

        private void GivenGetDistinctTemplateMetadataFundingLinesContents(
            string fundingStreamId,
            string fundingPeriodId,
            string templateVersion,
            TemplateMetadataDistinctFundingLinesContents templateMetadataDistinctFundingLinesContents)
        {
            _policiesService
                .Setup(_ => _.GetDistinctTemplateMetadataFundingLinesContents(
                    fundingStreamId,
                    fundingPeriodId,
                    templateVersion
                    ))
                .ReturnsAsync(templateMetadataDistinctFundingLinesContents)
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

        private void GivenFundingStreams(IEnumerable<FundingStream> fundingStreams)
        {
            _policiesService
                .Setup(_ => _.GetFundingStreams())
                .ReturnsAsync(fundingStreams)
                .Verifiable();
        }

        private void GivenDistinctTemplateMetadataFundingLinesContents(
            string fundingStreamId,
            string fundingPeriodId,
            string templateVersion,
            TemplateMetadataDistinctFundingLinesContents templateMetadataDistinctFundingLinesContents)
        {
            _policiesService
                .Setup(_ => _.GetDistinctTemplateMetadataFundingLinesContents(fundingStreamId, fundingPeriodId, templateVersion))
                .ReturnsAsync(templateMetadataDistinctFundingLinesContents)
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
                .ReturnsAsync(new[]
                {
                    publishedProviderVersion
                })
                .Verifiable();
        }

        private void GivenPublishedProviderVersionsForApproval(
            string specificationId,
            string providerId,
            string fundingStreamId,
            IEnumerable<PublishedProviderVersion> publishedProviderVersions)
        {
            _publishedFunding.Setup(_ => _.GetPublishedProviderVersionsForApproval(
                    specificationId,
                    fundingStreamId,
                    providerId))
                .ReturnsAsync(publishedProviderVersions)
                .Verifiable();
        }

        private static IEnumerable<object[]> MissingIdExamples()
        {
            yield return new object[]
            {
                null
            };
            yield return new object[]
            {
                ""
            };
            yield return new object[]
            {
                string.Empty
            };
        }

        private ProfileTotal NewProfileTotal(Action<ProfileTotalBuilder> setUp = null)
        {
            ProfileTotalBuilder profileTotalBuilder = new ProfileTotalBuilder();

            setUp?.Invoke(profileTotalBuilder);

            return profileTotalBuilder
                .Build();
        }

        private PublishedProviderError NewPublishedProviderError(Action<PublishedProviderErrorBuilder> setUp = null)
        {
            PublishedProviderErrorBuilder publishedProviderErrorBuilder = new PublishedProviderErrorBuilder();

            setUp?.Invoke(publishedProviderErrorBuilder);

            return publishedProviderErrorBuilder.Build();
        }

        private FundingLineProfile NewProfileTotal(Action<FundingLineProfileBuilder> setUp = null)
        {
            FundingLineProfileBuilder fundingLineProfileBuilder = new FundingLineProfileBuilder();

            setUp?.Invoke(fundingLineProfileBuilder);

            return fundingLineProfileBuilder.Build();
        }

        private string NewRandomString() => new RandomString();
        private DateTime NewRandomDateTime() => new RandomDateTime();
    }
}