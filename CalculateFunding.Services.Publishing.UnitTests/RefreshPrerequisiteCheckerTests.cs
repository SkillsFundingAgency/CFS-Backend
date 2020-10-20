using CalculateFunding.Common.ApiClient.Specifications.Models;
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

        [TestInitialize]
        public void SetUp()
        {
            _specificationFundingStatusService = Substitute.For<ISpecificationFundingStatusService>();
            _specificationService = Substitute.For<ISpecificationService>();
            _jobsRunning = Substitute.For<IJobsRunning>();
            _calculationApprovalCheckerService = Substitute.For<ICalculationPrerequisiteCheckerService>();
            _jobManagement = Substitute.For<IJobManagement>();
            _logger = Substitute.For<ILogger>();

            _refreshPrerequisiteChecker = new RefreshPrerequisiteChecker(
                _specificationFundingStatusService, 
                _specificationService,
                _jobsRunning,
                _calculationApprovalCheckerService,
                _jobManagement,
                _logger);
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
        public void ReturnsErrorMessageWhenSharesAlreadyChoseFundingStream()
        {
            // Arrange
            string specificationId = "specId01";
            SpecificationSummary specificationSummary = new SpecificationSummary { Id = specificationId, ApprovalStatus = PublishStatus.Approved };

            string errorMessage = $"Specification with id: '{specificationId} already shares chosen funding streams";
            
            GivenTheSpecificationFundingStatusForTheSpecification(specificationSummary, SpecificationFundingStatus.SharesAlreadyChosenFundingStream);

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
        public void ReturnsErrorMessageWhenCanChooseSpecificationFundingAndErrorSelectingSpecificationForFunding()
        {
            // Arrange
            string specificationId = "specId01";
            SpecificationSummary specificationSummary = new SpecificationSummary { Id = specificationId, ApprovalStatus = PublishStatus.Approved };

            string errorMessage = "Generic error message";

            GivenTheSpecificationFundingStatusForTheSpecification(specificationSummary, SpecificationFundingStatus.CanChoose);
            GivenExceptionThrownForSelectSpecificationForFunding(specificationId, new Exception(errorMessage));

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
        public void ReturnsErrorMessageWhenCalculationPrequisitesNotMet()
        {
            // Arrange
            string specificationId = "specId01";
            SpecificationSummary specificationSummary = new SpecificationSummary { Id = specificationId, ApprovalStatus = PublishStatus.Approved };

            string errorMessage = "Error message";

            GivenTheSpecificationFundingStatusForTheSpecification(specificationSummary, SpecificationFundingStatus.AlreadyChosen);
            GivenCalculationEngineRunningStatusForTheSpecification(specificationId);
            GivenValidationErrorsForTheSpecification(specificationSummary, new List<string> { errorMessage });

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

        private void GivenTheSpecificationFundingStatusForTheSpecification(SpecificationSummary specification, SpecificationFundingStatus specificationFundingStatus)
        {
            _specificationFundingStatusService.CheckChooseForFundingStatus(specification)
                .Returns(specificationFundingStatus);
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

        private async Task WhenThePreRequisitesAreChecked(SpecificationSummary specification, IEnumerable<PublishedProvider> publishedProviders, IEnumerable<Provider> providers)
        {
            await _refreshPrerequisiteChecker.PerformChecks(specification, null, publishedProviders, providers);
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
