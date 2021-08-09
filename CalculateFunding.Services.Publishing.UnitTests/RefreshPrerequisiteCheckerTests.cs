﻿using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Generators.OrganisationGroup.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using Polly;
using CalculateFunding.Services.Publishing.UnitTests.Profiling;
using ApiProfileVariationPointer = CalculateFunding.Common.ApiClient.Specifications.Models.ProfileVariationPointer;
using CalculateFunding.Services.Publishing.UnitTests.Variations.Changes;
using ApiPeriodType = CalculateFunding.Common.ApiClient.Profiling.Models.PeriodType;
using CalculateFunding.Common.ApiClient.Calcs.Models.ObsoleteItems;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class RefreshPrerequisiteCheckerTests
    {
        private ISpecificationFundingStatusService _specificationFundingStatusService;
        private ISpecificationService _specificationService;
        private IJobsRunning _jobsRunning;
        private ICalculationPrerequisiteCheckerService _calculationApprovalCheckerService;
        private IJobManagement _jobManagement;
        private ILogger _logger;
        private RefreshPrerequisiteChecker _refreshPrerequisiteChecker;
        private IPoliciesService _policiesService;
        private IProfilingService _profilingService;
        private ICalculationsService _calculationsService;
        private IFundingStreamPaymentDatesRepository _fundingStreamPaymentDatesRepository;
        private ResiliencePolicies _resiliencePolicies;

        [TestInitialize]
        public void SetUp()
        {
            _specificationFundingStatusService = Substitute.For<ISpecificationFundingStatusService>();
            _specificationService = Substitute.For<ISpecificationService>();
            _jobsRunning = Substitute.For<IJobsRunning>();
            _calculationApprovalCheckerService = Substitute.For<ICalculationPrerequisiteCheckerService>();
            _jobManagement = Substitute.For<IJobManagement>();
            _logger = Substitute.For<ILogger>();
            _policiesService = Substitute.For<IPoliciesService>();
            _profilingService = Substitute.For<IProfilingService>();
            _calculationsService = Substitute.For<ICalculationsService>();
            _fundingStreamPaymentDatesRepository = Substitute.For<IFundingStreamPaymentDatesRepository>();

            _resiliencePolicies = new ResiliencePolicies
            {
                FundingStreamPaymentDatesRepository = Policy.NoOpAsync()
            };

            _refreshPrerequisiteChecker = new RefreshPrerequisiteChecker(
                _specificationFundingStatusService, 
                _specificationService,
                _jobsRunning,
                _calculationApprovalCheckerService,
                _jobManagement,
                _logger,
                _policiesService,
                _profilingService,
                _calculationsService,
                _resiliencePolicies,
                _fundingStreamPaymentDatesRepository);
        }

        [TestMethod]
        public void ThrowsArgumentNullException()
        {
            // Arrange

            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(null, null, null);

            // Assert
            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Where(_ =>
                    _.Message == $"Value cannot be null. (Parameter 'specification')");
        }

        [TestMethod]
        public void ReturnsErrorMessageWhenSpecificationPublishStatusIsNotApproved()
        {
            // Arrange
            string specificationId = "specId01";
            SpecificationSummary specificationSummary = new SpecificationSummary { Id = specificationId, ApprovalStatus = PublishStatus.Archived };

            string errorMessage = "Specification failed refresh prerequisite check. Reason: must be approved";

            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(specificationSummary, Enumerable.Empty<PublishedProvider>(), Enumerable.Empty<Provider>());

            // Assert
            invocation
                .Should()
                .Throw<JobPrereqFailedException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{specificationSummary.Id} has prerequisites which aren't complete.");

            _logger
                .Received()
                .Error(errorMessage);

        }


        [TestMethod]
        public void ReturnsErrorMessageWhenSpecificationHasNoScopedProviders()
        {
            // Arrange
            string specificationId = "specId01";
            SpecificationSummary specificationSummary = new SpecificationSummary { Id = specificationId, ApprovalStatus = PublishStatus.Approved };

            string errorMessage = "Specification failed refresh prerequisite check. Reason: no scoped providers";

            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(specificationSummary, Enumerable.Empty<PublishedProvider>(), Enumerable.Empty<Provider>());

            // Assert
            invocation
                .Should()
                .Throw<JobPrereqFailedException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{specificationSummary.Id} has prerequisites which aren't complete.");

            _logger
                .Received()
                .Error(errorMessage);

        }

        [TestMethod]
        public void ReturnsErrorMessageWhenSpecificationHasObsoleteItems()
        {
            // Arrange
            string specificationId = "specId01";
            SpecificationSummary specificationSummary = new SpecificationSummary { Id = specificationId, ApprovalStatus = PublishStatus.Approved };
            IEnumerable<Provider> providers = new[] { new Provider { ProviderId = "ProviderId" } };

            string errorMessage = "Funding can not be refreshed due to calculation errors.";

            IEnumerable<ObsoleteItem> obsoleteItems = new List<ObsoleteItem>
            {
                new ObsoleteItem()
            };

            GivenObsoleteItemsForSpecification(specificationSummary.Id, obsoleteItems);

            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(specificationSummary, Enumerable.Empty<PublishedProvider>(), providers);

            // Assert
            invocation
                .Should()
                .Throw<JobPrereqFailedException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{specificationSummary.Id} has prerequisites which aren't complete.");

            _logger
                .Received()
                .Error(errorMessage);

        }

        [TestMethod]
        public void ReturnsErrorMessageWhenSharesAlreadyChoseFundingStream()
        {
            // Arrange
            string specificationId = "specId01";
            SpecificationSummary specificationSummary = new SpecificationSummary { Id = specificationId, ApprovalStatus = PublishStatus.Approved };
            IEnumerable<Provider> providers = new[] { new Provider { ProviderId = "ProviderId" } };

            string errorMessage = $"Specification with id: '{specificationId} already shares chosen funding streams";
            
            GivenTheSpecificationFundingStatusForTheSpecification(specificationSummary, SpecificationFundingStatus.SharesAlreadyChosenFundingStream);

            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(specificationSummary, Enumerable.Empty<PublishedProvider>(), providers);

            // Assert
            invocation
                .Should()
                .Throw<JobPrereqFailedException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{specificationSummary.Id} has prerequisites which aren't complete.");

            _logger
                .Received()
                .Error(errorMessage);

        }

        [TestMethod]
        public void ReturnsErrorMessageWhenCanChooseSpecificationFundingAndErrorSelectingSpecificationForFunding()
        {
            // Arrange
            string specificationId = "specId01";
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                Id = specificationId,
                ApprovalStatus = PublishStatus.Approved,
                FundingStreams = new[] { NewReference(_ => _.WithId(fundingStreamId)) },
                FundingPeriod = NewReference(_ => _.WithId(fundingPeriodId))
            };

            IEnumerable<Provider> providers = new[] { new Provider { ProviderId = "ProviderId" } };

            string errorMessage = "Generic error message";

            GivenTheSpecificationFundingStatusForTheSpecification(specificationSummary, SpecificationFundingStatus.CanChoose);
            GivenExceptionThrownForSelectSpecificationForFunding(specificationId, new Exception(errorMessage));
            GivenProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId, fundingPeriodId, new[] { new FundingStreamPeriodProfilePattern { } });

            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(specificationSummary, Enumerable.Empty<PublishedProvider>(), providers);

            // Assert
            invocation
                .Should()
                .Throw<JobPrereqFailedException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{specificationSummary.Id} has prerequisites which aren't complete.");

            _logger
                .Received()
                .Error(errorMessage);
        }

        [TestMethod]
        public void ReturnsErrorMessageWhenJobsRunning()
        {
            // Arrange
            string specificationId = "specId01";
            SpecificationSummary specificationSummary = new SpecificationSummary { Id = specificationId };

            string errorMessage = $"{JobConstants.DefinitionNames.CreateInstructAllocationJob} is still running";

            GivenTheSpecificationFundingStatusForTheSpecification(specificationSummary, SpecificationFundingStatus.AlreadyChosen);
            GivenCalculationEngineRunningStatusForTheSpecification(specificationId, JobConstants.DefinitionNames.CreateInstructAllocationJob);
            GivenValidationErrorsForTheSpecification(specificationSummary, Enumerable.Empty<string>());

            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(specificationSummary, Enumerable.Empty<PublishedProvider>(), Enumerable.Empty<Provider>());

            // Assert
            invocation
                .Should()
                .Throw<JobPrereqFailedException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{specificationSummary.Id} has prerequisites which aren't complete.");

            _logger
                .Received()
                .Error(errorMessage);
        }

        [TestMethod]
        public void ReturnsErrorMessageWhenProfilingConfigPrequisitesNotMet()
        {
            // Arrange
            string specificationId = "specId01";
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingLineCodeOne = NewRandomString();
            string fundingLineCodeTwo = NewRandomString();
            string templateId = NewRandomString();

            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                Id = specificationId,
                ApprovalStatus = PublishStatus.Approved,
                FundingStreams = new[] { NewReference(_ => _.WithId(fundingStreamId)) },
                FundingPeriod = NewReference(_ => _.WithId(fundingPeriodId)),
                TemplateIds = new Dictionary<string, string>
                {
                    { fundingStreamId, templateId }
                }
            };
            IEnumerable<Provider> providers = new[] { new Provider { ProviderId = "ProviderId" } };

            string errorMessage = $"Profiling configuration missing for funding lines {fundingLineCodeOne},{fundingLineCodeTwo}";

            TemplateMetadataDistinctFundingLinesContents templateMetadataDistinctFundingLinesContents = new TemplateMetadataDistinctFundingLinesContents
            {
                FundingLines = new List<TemplateMetadataFundingLine>
                {
                    new TemplateMetadataFundingLine
                    {
                        FundingLineCode = fundingLineCodeOne
                    },
                    new TemplateMetadataFundingLine
                    {
                        FundingLineCode = fundingLineCodeTwo
                    }
                }
            };
            
            GivenTheSpecificationFundingStatusForTheSpecification(specificationSummary, SpecificationFundingStatus.AlreadyChosen);
            GivenCalculationEngineRunningStatusForTheSpecification(specificationId);
            GivenProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId, fundingPeriodId, Array.Empty<FundingStreamPeriodProfilePattern>());
            GivenDistinctTemplateMetadataFundingLinesContents(fundingStreamId, specificationSummary, templateMetadataDistinctFundingLinesContents);

            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(specificationSummary, Enumerable.Empty<PublishedProvider>(), providers);

            // Assert
            invocation
                .Should()
                .Throw<JobPrereqFailedException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{specificationSummary.Id} has prerequisites which aren't complete.");

            _logger
                .Received()
                .Error(errorMessage);
        }

        [TestMethod]
        public void ReturnsErrorMessageWhenCalculationPrequisitesNotMet()
        {
            // Arrange
            string specificationId = "specId01";
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                Id = specificationId,
                ApprovalStatus = PublishStatus.Approved,
                FundingStreams = new[] { NewReference(_ => _.WithId(fundingStreamId)) },
                FundingPeriod = NewReference(_ => _.WithId(fundingPeriodId))
            };
            IEnumerable<Provider> providers = new[] { new Provider { ProviderId = "ProviderId" } };

            string errorMessage = "Error message";

            GivenTheSpecificationFundingStatusForTheSpecification(specificationSummary, SpecificationFundingStatus.AlreadyChosen);
            GivenCalculationEngineRunningStatusForTheSpecification(specificationId);
            GivenProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId, fundingPeriodId, new[] { new FundingStreamPeriodProfilePattern { } });
            GivenValidationErrorsForTheSpecification(specificationSummary, new List<string> { errorMessage });

            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(specificationSummary, Enumerable.Empty<PublishedProvider>(), providers);

            // Assert
            invocation
                .Should()
                .Throw<JobPrereqFailedException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{specificationSummary.Id} has prerequisites which aren't complete.");

            _logger
                .Received()
                .Error(errorMessage);
        }

        [TestMethod]
        public async Task ReturnsErrorMessageWhenTrustIdMisatch()
        {
            // Arrange
            string specificationId = NewRandomString();
            string providerVersionId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            string providerId1 = NewRandomString();
            int majorVersion1 = NewRandomInt();
            int minorVersion1 = NewRandomInt();
            string providerId2 = NewRandomString();
            int majorVersion2 = NewRandomInt();
            int minorVersion2 = NewRandomInt();
            string identifierValue1 = NewRandomString();
            string identifierValue2 = NewRandomString();
            string fundingConfigurationId = NewRandomString();
            Generators.OrganisationGroup.Enums.OrganisationGroupTypeIdentifier groupTypeIdentifier = Generators.OrganisationGroup.Enums.OrganisationGroupTypeIdentifier.UKPRN;

            string errorMessage = $"TrustIds {groupTypeIdentifier}-{identifierValue2} not matched.";
            
            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _
                .WithId(specificationId)
                .WithProviderVersionId(providerVersionId)
                .WithPublishStatus(PublishStatus.Approved));
            IEnumerable<PublishedProvider> publishedProviders = new[] 
            {
                NewPublishedProvider(_ => _
                .WithCurrent(NewPublishedProviderVersion(pv => pv.WithFundingStreamId(fundingStreamId)
                                                                .WithFundingPeriodId(fundingPeriodId)))
                .WithReleased(NewPublishedProviderVersion(pv => pv.WithFundingStreamId(fundingStreamId)
                                                                .WithFundingPeriodId(fundingPeriodId)
                                                                .WithProviderId(providerId1)
                                                                .WithMajorVersion(majorVersion1)
                                                                .WithMinorVersion(minorVersion1)))),
                NewPublishedProvider(_ => _
                .WithCurrent(NewPublishedProviderVersion(pv => pv.WithFundingStreamId(fundingStreamId)
                                                                .WithFundingPeriodId(fundingPeriodId)))
                .WithReleased(NewPublishedProviderVersion(pv => pv.WithFundingStreamId(fundingStreamId)
                                                                .WithFundingPeriodId(fundingPeriodId)
                                                                .WithProviderId(providerId2)
                                                                .WithMajorVersion(majorVersion2)
                                                                .WithMinorVersion(minorVersion2))))
            };

            IEnumerable<Provider> providers = new[]
            {
                NewProvider(_ => _.WithProviderId(providerId1)),
                 NewProvider(_ => _.WithProviderId(providerId2))
            };

            IEnumerable<OrganisationGroupResult> organisationGroupResults = new[]
            {
               NewOrganisationGroupResult(_ => _
               .WithIdentifiers(new [] 
               {
                   NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(identifierValue1)),
                   NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(NewRandomString()))
               })),
               NewOrganisationGroupResult(_ => _
               .WithIdentifiers(new []
               {
                   NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(identifierValue2)),
                   NewOrganisationIdentifier(i => i.WithType(groupTypeIdentifier).WithValue(NewRandomString()))
               }))
            };

            IEnumerable<PublishedFunding> publishedFundings = new[]
            {
                NewPublishedFunding(_ => _
                .WithCurrent(NewPublishedFundingVersion(fv => fv.WithProviderFundings(new []
                                                                        {
                                                                            $"{fundingStreamId}-{fundingPeriodId}-{providerId1}-{majorVersion1}_{minorVersion1}",
                                                                            $"{fundingStreamId}-{fundingPeriodId}-{providerId2}-{majorVersion2}_{minorVersion2}"
                                                                        })
                                                                .WithGroupReason(CalculateFunding.Models.Publishing.GroupingReason.Payment)
                                                                .WithOrganisationGroupIdentifierValue(identifierValue1)
                                                                .WithOrganisationGroupTypeIdentifier(groupTypeIdentifier)))),
                NewPublishedFunding(_ => _
                .WithCurrent(NewPublishedFundingVersion(fv => fv.WithProviderFundings(new []{ $"{fundingStreamId}-{fundingPeriodId}-{providerId1}-{majorVersion1}_{minorVersion1}", NewRandomString() })
                                                                .WithGroupReason(CalculateFunding.Models.Publishing.GroupingReason.Payment)
                                                                .WithOrganisationGroupIdentifierValue(identifierValue2)
                                                                .WithOrganisationGroupTypeIdentifier(groupTypeIdentifier))))
            };

            // Act
            await WhenThePreRequisitesAreChecked(specificationSummary, publishedProviders, providers);

            // Assert
            publishedProviders
                .All(_ => _.Current.HasErrors);
        }

        [TestMethod]
        public async Task ReturnsErrorMessageWhenVariationPointerSetEarlierThanTheLastPaymentDate()
        {
            // Arrange
            string errorMessage =
                $"There are payment funding lines with variation instalments set earlier than the current profile instalment.{Environment.NewLine}#VariationInstallmentLink# must be set later or equal to the current profile instalment.";

            string specificationId = NewRandomString();
            string providerVersionId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            string providerIdOne = NewRandomString();

            DateTime paymentDate = DateTime.Today.ToUniversalTime();
            DateTime variationPointerDate = paymentDate.AddMonths(-1);

            SpecificationSummary specificationSummary = NewSpecificationSummary(_ => _
                .WithId(specificationId)
                .WithProviderVersionId(providerVersionId)
                .WithPublishStatus(PublishStatus.Approved)
                .WithFundingPeriodId(fundingPeriodId)
                .WithFundingStreamIds(fundingStreamId));

            FundingStreamPaymentDates fundingStreamPaymentDates = NewFundingStreamPaymentDates(_ => _
                .WithPaymentDates(
                    NewFundingStreamPaymentDate(pd => pd
                    .WithYear(paymentDate.Year)
                    .WithType(ProfilePeriodType.CalendarMonth)
                    .WithTypeValue(paymentDate.ToString("MMMM")))));

            IEnumerable<Provider> providers = new[]
            {
                NewProvider(_ => _.WithProviderId(providerIdOne)),
            };

            IEnumerable<ApiProfileVariationPointer> profileVariationPointers = new[]
            {
                NewProfileVariationPointer(_ => _
                    .WithFundingStreamId(fundingStreamId)
                    .WithPeriodType(ApiPeriodType.CalendarMonth.ToString())
                    .WithYear(variationPointerDate.Year)
                    .WithTypeValue(variationPointerDate.ToString("MMMM"))
                    .WithOccurence(1))
            };

            GivenProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId, fundingPeriodId, new[] { new FundingStreamPeriodProfilePattern { } });
            AndUpdateDates(fundingStreamId, fundingPeriodId, fundingStreamPaymentDates);
            AndGetProfileVariationPointers(specificationId, profileVariationPointers);

            // Act
            Func<Task> invocation
                = () => WhenThePreRequisitesAreChecked(specificationSummary, Enumerable.Empty<PublishedProvider>(), providers);

            // Assert
            invocation
                .Should()
                .Throw<JobPrereqFailedException>()
                .Where(_ =>
                    _.Message == $"Specification with id: '{specificationSummary.Id} has prerequisites which aren't complete.");

            _logger
                .Received()
                .Error(errorMessage);
        }

        private void AndUpdateDates(string fundingStreamId, string fundingPeriodId, FundingStreamPaymentDates fundingStreamPaymentDates)
        {
            _fundingStreamPaymentDatesRepository
                .GetUpdateDates(fundingStreamId, fundingPeriodId)
                .Returns(fundingStreamPaymentDates);
        }

        private void AndGetProfileVariationPointers(string specificationId, IEnumerable<ApiProfileVariationPointer> apiProfileVariationPointers)
        {
            _specificationService
                .GetProfileVariationPointers(specificationId)
                .Returns(apiProfileVariationPointers);
        }

        private void GivenTheSpecificationFundingStatusForTheSpecification(SpecificationSummary specification, SpecificationFundingStatus specificationFundingStatus)
        {
            _specificationFundingStatusService.CheckChooseForFundingStatus(specification)
                .Returns(specificationFundingStatus);
        }

        private void GivenObsoleteItemsForSpecification(
            string specificationId, 
            IEnumerable<ObsoleteItem> obsoleteItems)
        {
            _calculationsService.GetObsoleteItemsForSpecification(specificationId)
                .Returns(obsoleteItems);
        }

        private void GivenExceptionThrownForSelectSpecificationForFunding(string specificationId, Exception ex)
        {
            _specificationService.SelectSpecificationForFunding(specificationId)
                .Throws(ex);
        }

        private void GivenCalculationEngineRunningStatusForTheSpecification(string specificationId, params string[] jobDefinitions)
        {
            _jobsRunning.GetJobTypes(specificationId, Arg.Any<string[]>())
                .Returns(jobDefinitions);
        }

        private void GivenValidationErrorsForTheSpecification(SpecificationSummary specification, IEnumerable<string> validationErrors)
        {
            _calculationApprovalCheckerService.VerifyCalculationPrerequisites(specification)
                .Returns(validationErrors);
        }

        private void GivenProfilePatternsForFundingStreamAndFundingPeriod(
            string fundingStreamId, 
            string fundingPeriodId,
            IEnumerable<FundingStreamPeriodProfilePattern> fundingStreamPeriodProfilePatterns)
        {
            _profilingService.GetProfilePatternsForFundingStreamAndFundingPeriod(fundingStreamId, fundingPeriodId)
                .Returns(fundingStreamPeriodProfilePatterns);
        }

        private void GivenDistinctTemplateMetadataFundingLinesContents(
            string fundingStreamId,
            SpecificationSummary specificationSummary,
            TemplateMetadataDistinctFundingLinesContents templateMetadataDistinctFundingLinesContents)
        {
            _policiesService.GetDistinctTemplateMetadataFundingLinesContents(
                fundingStreamId,
                specificationSummary.FundingPeriod.Id,
                specificationSummary.TemplateIds[fundingStreamId])
                .Returns(templateMetadataDistinctFundingLinesContents);
        }

        private async Task WhenThePreRequisitesAreChecked(SpecificationSummary specification, IEnumerable<PublishedProvider> publishedProviders, IEnumerable<Provider> providers)
        {
            await _refreshPrerequisiteChecker.PerformChecks(specification, null, publishedProviders, providers);
        }

        private static Reference NewReference(Action<ReferenceBuilder> setUp = null)
        {
            ReferenceBuilder referenceBuilder = new ReferenceBuilder();

            setUp?.Invoke(referenceBuilder);

            return referenceBuilder.Build();
        }

        private static FundingStreamPaymentDates NewFundingStreamPaymentDates(Action<FundingStreamPaymentDatesBuilder> setUp = null)
        {
            FundingStreamPaymentDatesBuilder fundingStreamPaymentDatesBuilder = new FundingStreamPaymentDatesBuilder();

            setUp?.Invoke(fundingStreamPaymentDatesBuilder);

            return fundingStreamPaymentDatesBuilder.Build();
        }

        private static FundingStreamPaymentDate NewFundingStreamPaymentDate(Action<FundingStreamPaymentDateBuilder> setUp = null)
        {
            FundingStreamPaymentDateBuilder fundingStreamPaymentDateBuilder = new FundingStreamPaymentDateBuilder();

            setUp?.Invoke(fundingStreamPaymentDateBuilder);

            return fundingStreamPaymentDateBuilder.Build();
        }

        private static ApiProfileVariationPointer NewProfileVariationPointer(Action<ProfileVariationPointerBuilder> setUp = null)
        {
            ProfileVariationPointerBuilder profileVariationPointerBuilder = new ProfileVariationPointerBuilder();

            setUp?.Invoke(profileVariationPointerBuilder);

            return profileVariationPointerBuilder.Build();
        }

        private static PublishedProviderVersion NewPublishedProviderVersion(Action<PublishedProviderVersionBuilder> setUp = null)
        {
            PublishedProviderVersionBuilder publishedProviderVersionBuilder = new PublishedProviderVersionBuilder();

            setUp?.Invoke(publishedProviderVersionBuilder);

            return publishedProviderVersionBuilder.Build();
        }

        private static PublishedProvider NewPublishedProvider(Action<PublishedProviderBuilder> setUp = null)
        {
            PublishedProviderBuilder publishedProviderBuilder = new PublishedProviderBuilder();

            setUp?.Invoke(publishedProviderBuilder);

            return publishedProviderBuilder.Build();
        }

        private static Provider NewProvider(Action<ProviderBuilder> setUp = null)
        {
            ProviderBuilder providerBuilder = new ProviderBuilder();

            setUp?.Invoke(providerBuilder);

            return providerBuilder.Build();
        }

        private static PublishedFunding NewPublishedFunding(Action<PublishedFundingBuilder> setUp = null)
        {
            PublishedFundingBuilder publishedFundingBuilder = new PublishedFundingBuilder();

            setUp?.Invoke(publishedFundingBuilder);

            return publishedFundingBuilder.Build();
        }

        private static PublishedFundingVersion NewPublishedFundingVersion(Action<PublishedFundingVersionBuilder> setUp = null)
        {
            PublishedFundingVersionBuilder publishedFundingVersionBuilder = new PublishedFundingVersionBuilder();

            setUp?.Invoke(publishedFundingVersionBuilder);

            return publishedFundingVersionBuilder.Build();
        }

        private static SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder specificationSummaryBuilder = new SpecificationSummaryBuilder();

            setUp?.Invoke(specificationSummaryBuilder);

            return specificationSummaryBuilder.Build();
        }

        private static OrganisationGroupResult NewOrganisationGroupResult(Action<OrganisationGroupResultBuilder> setUp = null)
        {
            OrganisationGroupResultBuilder organisationGroupResultBuilder = new OrganisationGroupResultBuilder();

            setUp?.Invoke(organisationGroupResultBuilder);

            return organisationGroupResultBuilder.Build();
        }

        private static OrganisationIdentifier NewOrganisationIdentifier(Action<OrganisationIdentifierBuilder> setUp = null)
        {
            OrganisationIdentifierBuilder organisationIdentifierBuilder = new OrganisationIdentifierBuilder();

            setUp?.Invoke(organisationIdentifierBuilder);

            return organisationIdentifierBuilder.Build();
        }

        private static FundingConfiguration NewFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder fundingConfigurationBuilder = new FundingConfigurationBuilder();

            setUp?.Invoke(fundingConfigurationBuilder);

            return fundingConfigurationBuilder.Build();
        }

        private static string NewRandomString() => new RandomString();
        private static int NewRandomInt() => new RandomNumberBetween(1, 10);
    }
}
