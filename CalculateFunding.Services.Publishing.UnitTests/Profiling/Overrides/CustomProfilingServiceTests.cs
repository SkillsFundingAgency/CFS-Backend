using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Profiling.Custom;
using CalculateFunding.Tests.Common.Builders;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Polly;
using Serilog.Core;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Generators.OrganisationGroup.Enums;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;

namespace CalculateFunding.Services.Publishing.UnitTests.Profiling.Overrides
{
    [TestClass]
    public class CustomProfilingServiceTests : CustomProfileRequestTestBase
    {
        private CustomProfilingService _service;
        private Mock<IPublishedProviderStatusUpdateService> _publishedProviderVersionCreation;
        private Mock<IValidator<ApplyCustomProfileRequest>> _validator;
        private Mock<IPublishedFundingRepository> _publishedFunding;
        private Mock<IPublishedFundingCsvJobsService> _publishedFundingCsvJobsService;

        private Mock<ISpecificationService> _specificationService;
        private Mock<IOrganisationGroupService> _organisationGroupService;
        private Mock<IPoliciesService> _policiesService;
        private Mock<IProviderService> _providerService;


        private readonly string CorrelationId = "123";

        [TestInitialize]
        public void SetUp()
        {
            _publishedProviderVersionCreation = new Mock<IPublishedProviderStatusUpdateService>();
            _validator = new Mock<IValidator<ApplyCustomProfileRequest>>();
            _publishedFunding = new Mock<IPublishedFundingRepository>();
            _publishedFundingCsvJobsService = new Mock<IPublishedFundingCsvJobsService>();
            
            _specificationService = new Mock<ISpecificationService>();
            _organisationGroupService = new Mock<IOrganisationGroupService>();
            _policiesService = new Mock<IPoliciesService>();
            _providerService = new Mock<IProviderService>();

            _service = new CustomProfilingService(_publishedProviderVersionCreation.Object,
                _validator.Object,
                _publishedFunding.Object,
                new ResiliencePolicies
                {
                    PublishedFundingRepository = Policy.NoOpAsync(),
                    SpecificationsApiClient = Policy.NoOpAsync()
                },
                _publishedFundingCsvJobsService.Object,
                Logger.None,
                _specificationService.Object,
                _organisationGroupService.Object,
                _policiesService.Object,
                _providerService.Object
                );
        }

        [TestMethod]
        public async Task ExitsEarlyIfRequestDoesntPassValidation()
        {
            ApplyCustomProfileRequest request = NewApplyCustomProfileRequest();

            GivenTheValidationResultForTheRequest(NewValidationResult(_ =>
                    _.WithValidationFailures(NewValidationFailure())),
                request);

            IActionResult result = await WhenTheCustomProfileIsApplied(request, NewAuthor());

            result
                .Should()
                .BeOfType<BadRequestObjectResult>();

            AndNoNewVersionWasCreated();
        }

        [TestMethod]
        public void ExitsEarlyIfRequestDoesntPassVerifyProfileAmountsMatchFundingLineValue()
        {
            string fundingLineOne = NewRandomString();
            decimal carryOver = 200;
            decimal periodProfileAmount1 = 1000;
            decimal periodProfileAmount2 = 2000;
            decimal fundingLineTotal = periodProfileAmount1 + periodProfileAmount2;

            ProfilePeriod profilePeriod1 = NewProfilePeriod(_ => _.WithDistributionPeriodId("FY-2021").WithAmount(periodProfileAmount1));
            ProfilePeriod profilePeriod2 = NewProfilePeriod(_ => _.WithDistributionPeriodId("FY-2022").WithAmount(periodProfileAmount2));

            ApplyCustomProfileRequest request = NewApplyCustomProfileRequest(_ => _
                .WithFundingLineCode(fundingLineOne)
                .WithProfilePeriods(profilePeriod1, profilePeriod2)
                .WithCarryOver(carryOver));

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(ppv =>
                ppv.WithPublishedProviderStatus(PublishedProviderStatus.Draft)
                    .WithFundingLines(NewFundingLine(fl =>
                        fl.WithFundingLineCode(fundingLineOne)
                            .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                dp.WithDistributionPeriodId("FY-2021")
                                    .WithProfilePeriods(profilePeriod1)),
                                    NewDistributionPeriod(dp =>
                                dp.WithDistributionPeriodId("FY-2022")
                                    .WithProfilePeriods(profilePeriod2)))
                            .WithValue(profilePeriod1.ProfiledValue + profilePeriod2.ProfiledValue)))
                        )));

            Reference author = NewAuthor();

            GivenTheValidationResultForTheRequest(NewValidationResult(), request);
            AndThePublishedProvider(request.PublishedProviderId, publishedProvider);

            Func<Task> Invocation = () => WhenTheCustomProfileIsApplied(request, author);

            decimal reProfileFundingLineTotal = fundingLineTotal;

            Invocation
                .Should()
                .Throw<InvalidOperationException>()
                .WithMessage($"Profile amounts ({fundingLineTotal}) and carry over amount ({carryOver}) does not equal funding line total requested ({reProfileFundingLineTotal}) from strategy.");
        }

        [TestMethod]
        public async Task ShouldNotExitsEarlyIfUpdatingPastProfilePeriodsForContractedProvider()
        {
            int? carryOver = 2;
            PublishedProviderStatus currentStatus = PublishedProviderStatus.Draft;

            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string providerVersionId = NewRandomString();
            int? providerSnapshotId = NewRandomNumber();

            string fundingLineOne = NewRandomString();
            ProfilePeriod profilePeriod1 = NewProfilePeriod(_ => _.WithDistributionPeriodId("FY-2021").WithYear(2021).WithTypeValue("May"));
            ProfilePeriod profilePeriod2 = NewProfilePeriod(_ => _.WithDistributionPeriodId("FY-2022").WithYear(2022).WithTypeValue("April"));

            ApplyCustomProfileRequest request = NewApplyCustomProfileRequest(_ => _
                .WithFundingLineCode(fundingLineOne)
                .WithProfilePeriods(profilePeriod1, profilePeriod2)
                .WithCarryOver(carryOver));

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(ppv =>
                ppv
                    .WithSpecificationId(specificationId)
                    .WithPublishedProviderStatus(currentStatus)
                    .WithCustomProfiles(
                            new[] {
                                    new FundingLineProfileOverrides {
                                        FundingLineCode = fundingLineOne,
                                        DistributionPeriods = new List<DistributionPeriod>()
                                    }
                                })
                    .WithFundingLines(NewFundingLine(fl =>
                        fl.WithFundingLineCode(fundingLineOne)
                            .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                dp.WithDistributionPeriodId("FY-2021")
                                    .WithProfilePeriods(profilePeriod1)),
                                    NewDistributionPeriod(dp =>
                                dp.WithDistributionPeriodId("FY-2022")
                                    .WithProfilePeriods(profilePeriod2)))
                            .WithValue(profilePeriod1.ProfiledValue + profilePeriod2.ProfiledValue + carryOver.GetValueOrDefault()))
                        ))));

            Reference author = NewAuthor();

            GivenTheValidationResultForTheRequest(NewValidationResult(), request);
            AndThePublishedProvider(request.PublishedProviderId, publishedProvider);

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _
                .WithId(specificationId)
                .WithFundingStreamIds(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithProviderVersionId(providerVersionId)
                .WithProviderSnapshotId(providerSnapshotId));
            AndGetSpecificationSummaryById(specificationId, specificationSummary);

            Provider provider = NewProvider();
            IDictionary<string, Provider> scopedProviders = new Dictionary<string, Provider>()
            {
                {string.Empty, provider }
            };
            AndGetScopedProvidersForSpecification(specificationId, providerVersionId, scopedProviders);

            FundingConfiguration fundingConfiguration = NewFundingConfiguration();
            AndGetFundingConfiguration(fundingStreamId, fundingPeriodId, fundingConfiguration);

            OrganisationGroupResult organisationGroupResult = NewOrganisationGroupResult(_ => _.WithGroupReason(OrganisationGroupingReason.Contracting));

            ProfileVariationPointer profileVariationPointer = NewProfileVariationPointer(_ => _.WithYear(2021).WithTypeValue("June"));
            GetProfileVariationPointers(specificationId, new[] { profileVariationPointer });

            Dictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData = new Dictionary<string, IEnumerable<OrganisationGroupResult>>
            {
                {string.Empty, new[]{ organisationGroupResult } }
            };
            AndGenerateOrganisationGroups(
                scopedProviders.Values.FirstOrDefault(),
                publishedProvider,
                fundingConfiguration,
                providerVersionId,
                providerSnapshotId,
                organisationGroupResultsData);

            IActionResult result = await WhenTheCustomProfileIsApplied(request, author);
            result
                .Should()
                .BeOfType<NoContentResult>();
        }

        [TestMethod]
        public async Task ShouldNotExitsEarlyIfUpdatingFutureProfilePeriodsForNonContractedProvider()
        {
            int? carryOver = 2;
            PublishedProviderStatus currentStatus = PublishedProviderStatus.Draft;

            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string providerVersionId = NewRandomString();
            int? providerSnapshotId = NewRandomNumber();

            string fundingLineOne = NewRandomString();
            ProfilePeriod profilePeriod1 = NewProfilePeriod(_ => _.WithDistributionPeriodId("FY-2021").WithYear(2022).WithTypeValue("May"));
            ProfilePeriod profilePeriod2 = NewProfilePeriod(_ => _.WithDistributionPeriodId("FY-2022").WithYear(2022).WithTypeValue("April"));

            ApplyCustomProfileRequest request = NewApplyCustomProfileRequest(_ => _
                .WithFundingLineCode(fundingLineOne)
                .WithProfilePeriods(profilePeriod1, profilePeriod2)
                .WithCarryOver(carryOver));

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(ppv =>
                ppv
                    .WithSpecificationId(specificationId)
                    .WithPublishedProviderStatus(currentStatus)
                    .WithCustomProfiles(
                            new[] {
                                    new FundingLineProfileOverrides {
                                        FundingLineCode = fundingLineOne,
                                        DistributionPeriods = new List<DistributionPeriod>()
                                    }
                                })
                    .WithFundingLines(NewFundingLine(fl =>
                        fl.WithFundingLineCode(fundingLineOne)
                            .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                dp.WithDistributionPeriodId("FY-2021")
                                    .WithProfilePeriods(profilePeriod1)),
                                    NewDistributionPeriod(dp =>
                                dp.WithDistributionPeriodId("FY-2022")
                                    .WithProfilePeriods(profilePeriod2)))
                            .WithValue(profilePeriod1.ProfiledValue + profilePeriod2.ProfiledValue + carryOver.GetValueOrDefault()))
                        ))));

            Reference author = NewAuthor();

            GivenTheValidationResultForTheRequest(NewValidationResult(), request);
            AndThePublishedProvider(request.PublishedProviderId, publishedProvider);

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _
                .WithId(specificationId)
                .WithFundingStreamIds(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithProviderVersionId(providerVersionId)
                .WithProviderSnapshotId(providerSnapshotId));
            AndGetSpecificationSummaryById(specificationId, specificationSummary);

            Provider provider = NewProvider();
            IDictionary<string, Provider> scopedProviders = new Dictionary<string, Provider>()
            {
                {string.Empty, provider }
            };
            AndGetScopedProvidersForSpecification(specificationId, providerVersionId, scopedProviders);

            FundingConfiguration fundingConfiguration = NewFundingConfiguration();
            AndGetFundingConfiguration(fundingStreamId, fundingPeriodId, fundingConfiguration);

            OrganisationGroupResult organisationGroupResult = NewOrganisationGroupResult(_ => _.WithGroupReason(OrganisationGroupingReason.Information));

            ProfileVariationPointer profileVariationPointer = NewProfileVariationPointer(_ => _.WithYear(2021).WithTypeValue("June"));
            GetProfileVariationPointers(specificationId, new[] { profileVariationPointer });

            Dictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData = new Dictionary<string, IEnumerable<OrganisationGroupResult>>
            {
                {string.Empty, new[]{ organisationGroupResult } }
            };
            AndGenerateOrganisationGroups(
                scopedProviders.Values.FirstOrDefault(),
                publishedProvider,
                fundingConfiguration,
                providerVersionId,
                providerSnapshotId,
                organisationGroupResultsData);

            IActionResult result = await WhenTheCustomProfileIsApplied(request, author);
            result
                .Should()
                .BeOfType<NoContentResult>();
        }

        [TestMethod]
        public async Task ExitsEarlyIfUpdatingPastProfilePeriodsForNonContractedProvider()
        {
            int? carryOver = 2;
            PublishedProviderStatus currentStatus = PublishedProviderStatus.Draft;

            string specificationId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string providerVersionId = NewRandomString();
            int? providerSnapshotId = NewRandomNumber();

            string fundingLineOne = NewRandomString();
            ProfilePeriod profilePeriod1 = NewProfilePeriod(_ => _.WithDistributionPeriodId("FY-2021").WithYear(2021).WithTypeValue("May"));
            ProfilePeriod profilePeriod2 = NewProfilePeriod(_ => _.WithDistributionPeriodId("FY-2022").WithYear(2022).WithTypeValue("April"));

            ApplyCustomProfileRequest request = NewApplyCustomProfileRequest(_ => _
                .WithFundingLineCode(fundingLineOne)
                .WithProfilePeriods(profilePeriod1, profilePeriod2)
                .WithCarryOver(carryOver));

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(ppv =>
                ppv
                    .WithSpecificationId(specificationId)
                    .WithPublishedProviderStatus(currentStatus)
                    .WithCustomProfiles(
                            new[] {
                                    new FundingLineProfileOverrides {
                                        FundingLineCode = fundingLineOne,
                                        DistributionPeriods = new List<DistributionPeriod>()
                                    }
                                })
                    .WithFundingLines(NewFundingLine(fl =>
                        fl.WithFundingLineCode(fundingLineOne)
                            .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                dp.WithDistributionPeriodId("FY-2021")
                                    .WithProfilePeriods(profilePeriod1)),
                                    NewDistributionPeriod(dp =>
                                dp.WithDistributionPeriodId("FY-2022")
                                    .WithProfilePeriods(profilePeriod2)))
                            .WithValue(profilePeriod1.ProfiledValue + profilePeriod2.ProfiledValue + carryOver.GetValueOrDefault()))
                        ))));

            Reference author = NewAuthor();

            GivenTheValidationResultForTheRequest(NewValidationResult(), request);
            AndThePublishedProvider(request.PublishedProviderId, publishedProvider);

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _
                .WithId(specificationId)
                .WithFundingStreamIds(fundingStreamId)
                .WithFundingPeriodId(fundingPeriodId)
                .WithProviderVersionId(providerVersionId)
                .WithProviderSnapshotId(providerSnapshotId));
            AndGetSpecificationSummaryById(specificationId, specificationSummary);

            Provider provider = NewProvider();
            IDictionary<string, Provider> scopedProviders = new Dictionary<string, Provider>()
            {
                {string.Empty, provider }
            };
            AndGetScopedProvidersForSpecification(specificationId, providerVersionId, scopedProviders);

            FundingConfiguration fundingConfiguration = NewFundingConfiguration();
            AndGetFundingConfiguration(fundingStreamId, fundingPeriodId, fundingConfiguration);

            OrganisationGroupResult organisationGroupResult = NewOrganisationGroupResult(_ => _.WithGroupReason(OrganisationGroupingReason.Information));

            ProfileVariationPointer profileVariationPointer = NewProfileVariationPointer(_ => _.WithYear(2021).WithTypeValue("June"));
            GetProfileVariationPointers(specificationId, new[] { profileVariationPointer });

            Dictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData = new Dictionary<string, IEnumerable<OrganisationGroupResult>>
            {
                {string.Empty, new[]{ organisationGroupResult } }
            };
            AndGenerateOrganisationGroups(
                scopedProviders.Values.FirstOrDefault(),
                publishedProvider,
                fundingConfiguration,
                providerVersionId,
                providerSnapshotId,
                organisationGroupResultsData);

            IActionResult result = await WhenTheCustomProfileIsApplied(request, author);
            result
                .Should()
                .BeOfType<BadRequestObjectResult>();
        }

        [TestMethod]
        [DataRow(PublishedProviderStatus.Draft, PublishedProviderStatus.Draft, null, true)]
        [DataRow(PublishedProviderStatus.Draft, PublishedProviderStatus.Draft, 2, true)]
        [DataRow(PublishedProviderStatus.Updated, PublishedProviderStatus.Updated, null, true)]
        [DataRow(PublishedProviderStatus.Approved, PublishedProviderStatus.Updated, null, true)]
        [DataRow(PublishedProviderStatus.Released, PublishedProviderStatus.Updated, null, true)]
        [DataRow(PublishedProviderStatus.Draft, PublishedProviderStatus.Draft, null, false)]
        [DataRow(PublishedProviderStatus.Draft, PublishedProviderStatus.Draft, 2, false)]
        [DataRow(PublishedProviderStatus.Updated, PublishedProviderStatus.Updated, null, false)]
        [DataRow(PublishedProviderStatus.Approved, PublishedProviderStatus.Updated, null, false)]
        [DataRow(PublishedProviderStatus.Released, PublishedProviderStatus.Updated, null, false)]
        public async Task OverridesProfilePeriodsOnPublishedProviderVersionAndGeneratesNewVersion(PublishedProviderStatus currentStatus,
            PublishedProviderStatus expectedRequestedStatus,
            int? carryOver,
            bool withExistingCustomProfile)
        {
            string fundingLineOne = NewRandomString();
            ProfilePeriod profilePeriod1 = NewProfilePeriod(_ => _.WithDistributionPeriodId("FY-2021"));
            ProfilePeriod profilePeriod2 = NewProfilePeriod(_ => _.WithDistributionPeriodId("FY-2022"));

            ApplyCustomProfileRequest request = NewApplyCustomProfileRequest(_ => _
                .WithFundingLineCode(fundingLineOne)
                .WithProfilePeriods(profilePeriod1, profilePeriod2)
                .WithCarryOver(carryOver));

            PublishedProvider publishedProvider = NewPublishedProvider(_ => _.WithCurrent(
                NewPublishedProviderVersion(ppv =>
                ppv.WithPublishedProviderStatus(currentStatus)
                    .WithCustomProfiles(withExistingCustomProfile ? 
                            new[] { 
                                    new FundingLineProfileOverrides { 
                                        FundingLineCode = fundingLineOne,
                                        DistributionPeriods = new List<DistributionPeriod>()
                                    } 
                                } : 
                            null)
                    .WithFundingLines(NewFundingLine(fl =>
                        fl.WithFundingLineCode(fundingLineOne)
                            .WithDistributionPeriods(NewDistributionPeriod(dp =>
                                dp.WithDistributionPeriodId("FY-2021")
                                    .WithProfilePeriods(profilePeriod1)),
                                    NewDistributionPeriod(dp =>
                                dp.WithDistributionPeriodId("FY-2022")
                                    .WithProfilePeriods(profilePeriod2)))
                            .WithValue(profilePeriod1.ProfiledValue + profilePeriod2.ProfiledValue + carryOver.GetValueOrDefault()))
                        ))));

            Reference author = NewAuthor();

            GivenTheValidationResultForTheRequest(NewValidationResult(), request);
            AndThePublishedProvider(request.PublishedProviderId, publishedProvider);

            IActionResult result = await WhenTheCustomProfileIsApplied(request, author);

            result
                .Should()
                .BeOfType<NoContentResult>();

            IEnumerable<ProfilePeriod> profilePeriods = request.ProfilePeriods;
            FundingLine fundingLine = publishedProvider.Current.FundingLines.Single(fl => fl.FundingLineCode == fundingLineOne);

            AndTheCustomProfilePeriodsWereUsedOn(fundingLine, profilePeriods);
            AndANewProviderVersionWasCreatedFor(publishedProvider, expectedRequestedStatus, author);
            AndProfilingAuditUpdatedForFundingLines(publishedProvider, new[] { fundingLineOne }, author);
            AndPublishCsvReportsJobCreated();
        }

        private void AndProfilingAuditUpdatedForFundingLines(PublishedProvider publishedProvider, string[] fundingLines, Reference author)
        {
            foreach (string fundingLineCode in fundingLines)
            {
                publishedProvider
                    .Current
                    .ProfilingAudits
                    .Should()
                    .Contain(a => a.FundingLineCode == fundingLineCode
                                && a.User != null
                                && a.User.Id == author.Id
                                && a.User.Name == author.Name
                                && a.Date.Date == DateTime.Today);
            }
        }

        private void AndTheCustomProfilePeriodsWereUsedOn(FundingLine fundingLine, IEnumerable<ProfilePeriod> profilePeriods)
        {
            fundingLine
                .DistributionPeriods.SelectMany(_ => _.ProfilePeriods)
                .Should()
                .BeEquivalentTo(profilePeriods);
        }

        private void GivenTheValidationResultForTheRequest(ValidationResult result, ApplyCustomProfileRequest request)
        {
            _validator.Setup(_ => _.ValidateAsync(request, default))
                .ReturnsAsync(result);
        }

        private void AndANewProviderVersionWasCreatedFor(PublishedProvider publishedProvider, PublishedProviderStatus newStatus, Reference author)
        {
            _publishedProviderVersionCreation.Verify(_ => _.UpdatePublishedProviderStatus(new[] { publishedProvider },
                author,
                newStatus,
                null,
                CorrelationId,
                true),
                Times.Once);
        }

        private void AndThePublishedProvider(string id, PublishedProvider publishedProvider)
        {
            _publishedFunding.Setup(_ => _.GetPublishedProviderById(id, id))
                .ReturnsAsync(publishedProvider);
        }

        private async Task<IActionResult> WhenTheCustomProfileIsApplied(ApplyCustomProfileRequest request, Reference author)
        {
            return await _service.ApplyCustomProfile(request, author, CorrelationId);
        }

        private Reference NewAuthor(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        private void AndGetSpecificationSummaryById(
            string specificationId,
            SpecificationSummary specificationSummary)
        {
            _specificationService
                .Setup(_ => _.GetSpecificationSummaryById(specificationId))
                .ReturnsAsync(specificationSummary);
        }

        private void AndGetScopedProvidersForSpecification(
            string specificationId,
            string providerVersionId,
            IDictionary<string, Provider> scopedProviders)
        {
            _providerService
                .Setup(_ => _.GetScopedProvidersForSpecification(specificationId, providerVersionId))
                .ReturnsAsync(scopedProviders);
        }

        private void AndGenerateOrganisationGroups(
            Provider provider,
            PublishedProvider publishedProvider,
            FundingConfiguration fundingConfiguration,
            string providerVersionId,
            int? providerSnapshotId,
            Dictionary<string, IEnumerable<OrganisationGroupResult>> organisationGroupResultsData)
        {
            _organisationGroupService
                .Setup(_ => _.GenerateOrganisationGroups(
                    It.Is<IEnumerable<Provider>>(_ => _.FirstOrDefault() == provider),
                    It.Is<IEnumerable<PublishedProvider>>(_ => _.FirstOrDefault() == publishedProvider),
                    fundingConfiguration,
                    providerVersionId,
                    providerSnapshotId))
                .ReturnsAsync(organisationGroupResultsData);
        }

        private void GetProfileVariationPointers(
            string specificationId,
            IEnumerable<ProfileVariationPointer> profileVariationPointers)
        {
            _specificationService
                .Setup(_ => _.GetProfileVariationPointers(specificationId))
                .ReturnsAsync(profileVariationPointers);
        }

        private void AndGetFundingConfiguration(
            string fundingStreamId,
            string fundingPeriodId,
            FundingConfiguration fundingConfiguration)
        {
            _policiesService
                .Setup(_ => _.GetFundingConfiguration(fundingStreamId, fundingPeriodId))
                .ReturnsAsync(fundingConfiguration);
        }

        private void AndNoNewVersionWasCreated()
        {
            _publishedProviderVersionCreation.Verify(_ => _.UpdatePublishedProviderStatus(It.IsAny<IEnumerable<PublishedProvider>>(),
                It.IsAny<Reference>(),
                It.IsAny<PublishedProviderStatus>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                true),
                Times.Never);
        }

        private void AndPublishCsvReportsJobCreated()
        {
            _publishedFundingCsvJobsService.Verify(_ => _.QueueCsvJobs(GeneratePublishingCsvJobsCreationAction.Refresh,
                It.IsAny<string>(),
                CorrelationId,
                It.IsAny<Reference>()), Times.Once);
        }

        private ValidationResult NewValidationResult(Action<ValidationResultBuilder> setUp = null)
        {
            ValidationResultBuilder resultBuilder = new ValidationResultBuilder();

            setUp?.Invoke(resultBuilder);

            return resultBuilder.Build();
        }

        private ValidationFailure NewValidationFailure(Action<ValidationFailureBuilder> setUp = null)
        {
            ValidationFailureBuilder failureBuilder = new ValidationFailureBuilder();

            setUp?.Invoke(failureBuilder);

            return failureBuilder.Build();
        }


    }
}