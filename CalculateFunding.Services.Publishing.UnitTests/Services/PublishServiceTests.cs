using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.Azure.ServiceBus;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculationResult = CalculateFunding.Models.Publishing.CalculationResult;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class PublishServiceTests : ServiceTestsBase
    {
        private PublishService _publishService;
        private SpecificationSummary _specificationSummary;
        private IPublishedFundingStatusUpdateService _publishedFundingStatusUpdateService;
        private IPublishingResiliencePolicies _publishingResiliencePolicies;
        private ISpecificationService _specificationService;
        private IPrerequisiteCheckerLocator _prerequisiteCheckerLocator;
        private IPublishedFundingChangeDetectorService _publishedFundingChangeDetectorService;
        private IPublishedFundingGenerator _publishedFundingGenerator;
        private IPublishedProviderContentsGeneratorResolver _publishedProviderContentsGeneratorResolver;
        private IPublishedFundingContentsPersistenceService _publishedFundingContentsPersistenceService;
        private IPublishedProviderContentPersistenceService _publishedProviderContentsPersistenceService;
        private IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private ISearchRepository<PublishedFundingIndex> _publishedFundingSearchRepository;
        private ICalculationsApiClient _calculationsApiClient;
        private IProviderService _providerService;
        private IJobManagement _jobManagement;
        private IPublishedFundingCsvJobsService _publishFundingCsvJobsService;
        private IMapper _mapper;
        private ITransactionFactory _transactionFactory;
        private IPublishedProviderVersionService _publishedProviderVersionService;
        private IPublishedFundingService _publishedFundingService;
        private ILogger _logger;
        private ISpecificationsApiClient _specificationsApiClient;
        private TemplateMetadataContents _templateMetadataContents;
        private TemplateMapping _templateMapping;
        private TemplateFundingLine[] _fundingLines;
        private CalculationResult[] _calculationResults;
        private TemplateCalculation[] _calculationTemplateIds;
        private IEnumerable<PublishedProvider> _publishedProviders;
        private IPublishedFundingDataService _publishedFundingDataService;
        private IPoliciesService _policiesService;
        private IOrganisationGroupGenerator _organisationGroupGenerator;
        private IPublishedFundingDateService _publishedFundingDateService;
        private ISpecificationFundingStatusService _specificationFundingStatusService;
        private ICreatePublishIntegrityJob _createPublishIntegrityJob;
        private IJobsRunning _jobsRunning;
        private ICreatePublishDatasetsDataCopyJob _createPublishDatasetsDataCopyJob;
        private ICreateProcessDatasetObsoleteItemsJob _createProcessDatasetObsoleteItemsJob;
        private Reference _author;
        private RandomString _jobId;
        private RandomString _correlationId;
        private const string SpecificationId = "SpecificationId";
        private const string FundingPeriodId = "AY-2020";
        private const string JobId = "JobId";
        private const string FundingStreamId = "PSG";
        private const string CorrelationId = "CorrelationId";

        private string _publishedProviderId;
        private string[] _publishedProviderIds;

        [TestInitialize]
        public void Setup()
        {
            _publishedProviderId = NewRandomString();
            _publishedProviderIds = new[] { _publishedProviderId };

            _publishedFundingStatusUpdateService = Substitute.For<IPublishedFundingStatusUpdateService>();
            _publishingResiliencePolicies = new ResiliencePolicies
            {
                PublishedFundingRepository = Policy.NoOpAsync(),
                CalculationsApiClient = Policy.NoOpAsync(),
                SpecificationsApiClient = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
                PublishedIndexSearchResiliencePolicy = Policy.NoOpAsync()
            };
            _specificationsApiClient = Substitute.For<ISpecificationsApiClient>();
            _specificationService = new SpecificationService(_specificationsApiClient, _publishingResiliencePolicies);
            _specificationFundingStatusService = Substitute.For<ISpecificationFundingStatusService>();
            _jobsRunning = Substitute.For<IJobsRunning>();
            _logger = Substitute.For<ILogger>();
            _publishedFundingChangeDetectorService = Substitute.For<IPublishedFundingChangeDetectorService>();
            _publishedFundingGenerator = Substitute.For<IPublishedFundingGenerator>();
            _publishedProviderContentsGeneratorResolver = Substitute.For<IPublishedProviderContentsGeneratorResolver>();
            _publishedFundingContentsPersistenceService = Substitute.For<IPublishedFundingContentsPersistenceService>();
            _publishedProviderContentsPersistenceService = Substitute.For<IPublishedProviderContentPersistenceService>();
            _publishedProviderStatusUpdateService = Substitute.For<IPublishedProviderStatusUpdateService>();
            _publishedFundingSearchRepository = Substitute.For<ISearchRepository<PublishedFundingIndex>>();
            _calculationsApiClient = Substitute.For<ICalculationsApiClient>();
            _providerService = Substitute.For<IProviderService>();
            _jobManagement = Substitute.For<IJobManagement>();
            _prerequisiteCheckerLocator = Substitute.For<IPrerequisiteCheckerLocator>();
            _prerequisiteCheckerLocator.GetPreReqChecker(PrerequisiteCheckerType.ReleaseAllProviders)
                .Returns(new PublishAllPrerequisiteChecker(_specificationFundingStatusService, _jobsRunning, _jobManagement, _logger));
            _prerequisiteCheckerLocator.GetPreReqChecker(PrerequisiteCheckerType.ReleaseBatchProviders)
                .Returns(new PublishBatchPrerequisiteChecker(_specificationFundingStatusService, _jobsRunning, _jobManagement, _logger));
            _publishFundingCsvJobsService = Substitute.For<IPublishedFundingCsvJobsService>();
            _mapper = Substitute.For<IMapper>();
            _transactionFactory = new TransactionFactory(_logger, new TransactionResiliencePolicies { TransactionPolicy = Policy.NoOpAsync() });
            _publishedProviderVersionService = Substitute.For<IPublishedProviderVersionService>();
            _policiesService = Substitute.For<IPoliciesService>();
            _organisationGroupGenerator = Substitute.For<IOrganisationGroupGenerator>();
            _publishedFundingChangeDetectorService = Substitute.For<IPublishedFundingChangeDetectorService>();
            _publishedFundingDateService = Substitute.For<IPublishedFundingDateService>();
            _publishedFundingDataService = Substitute.For<IPublishedFundingDataService>();
            _createPublishIntegrityJob = Substitute.For<ICreatePublishIntegrityJob>();
            _createPublishDatasetsDataCopyJob = Substitute.For<ICreatePublishDatasetsDataCopyJob>();
            _createProcessDatasetObsoleteItemsJob = Substitute.For<ICreateProcessDatasetObsoleteItemsJob>();
            _author = new Reference(new RandomString(), new RandomString());
            _jobId = new RandomString();
            _correlationId = new RandomString();

            _publishedFundingService = new PublishedFundingService(_publishedFundingDataService,
                _publishingResiliencePolicies,
                _policiesService,
                _organisationGroupGenerator,
                _publishedFundingChangeDetectorService,
                _publishedFundingDateService,
                _mapper,
                _logger);
        
            _publishService = new PublishService(_publishedFundingStatusUpdateService,
                _publishingResiliencePolicies,
                _specificationService,
                _prerequisiteCheckerLocator,
                _publishedFundingChangeDetectorService,
                _publishedFundingGenerator,
                _publishedProviderContentsGeneratorResolver,
                _publishedFundingContentsPersistenceService,
                _publishedProviderContentsPersistenceService,
                _publishedProviderStatusUpdateService,
                _providerService,
                _publishedFundingSearchRepository,
                _calculationsApiClient,
                _logger,
                _jobManagement,
                _transactionFactory,
                _publishedProviderVersionService,
                _publishedFundingService,
                _createPublishIntegrityJob,
                _publishFundingCsvJobsService,
                _createPublishDatasetsDataCopyJob,
                _createProcessDatasetObsoleteItemsJob
            );
        }

        [TestMethod]
        public async Task PublishAllProviderFundingResults_AddsInitialAllocationVariationReasonsToAllNewProviderVersions()
        {
            GivenJobCanBeProcessed();
            AndSpecification(true);
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndPublishedProviders();
            AndTemplateMapping();

            await WhenPublishAllProvidersMessageReceivedWithJobId();

            ThenEachProviderVersionHasTheFollowingVariationReasons(VariationReason.FundingUpdated, VariationReason.ProfilingUpdated, VariationReason.AuthorityFieldUpdated);
            AndTheCsvGenerationJobsWereCreated(SpecificationId, FundingPeriodId, FundingStreamId);
            AndThePublishDatasetsDataCopyJobsWereCreated(SpecificationId);
            AndTheProcessDatasetObsoleteItemsJobsWereCreated(SpecificationId);
        }
        
        [TestMethod]
        public async Task PublishAllProviderFundingResults_DoesNotAddInitialAllocationVariationReasonsToExistingProviderVersions()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndPublishedProviders(wasReleased: true, minorVersion: 1);
            AndTemplateMapping();

            await WhenPublishAllProvidersMessageReceivedWithJobId();

            ThenEachProviderVersionHasTheFollowingVariationReasons(VariationReason.AuthorityFieldUpdated);
        }

        [TestMethod]
        public async Task PublishAllProviderFundingResults_AddsPublishedFundingVariationReasonsToAllPublishedProviders()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndPublishedProviders();
            AndTemplateMapping();
            AndGeneratePublishedFunding();

            await WhenPublishAllProvidersMessageReceivedWithJobId();

            ThenUpdatePublishedFundingStatusHasTheFollowingVariationReasons(VariationReason.AuthorityFieldUpdated);
        }

        [TestMethod]
        public async Task PublishAllProviderFundingResults_WhenAnUpdatePublishStatusThrowsException_TransactionCompensates()
        {
            string error = "Unable to update status.";

            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndPublishedProviders();
            AndUpdateStatusThrowsAnError(error);
            AndTemplateMapping();

            Func<Task> invocation = WhenPublishAllProvidersMessageReceivedWithJobId;

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be(error);

            await _publishedProviderVersionService
                .Received(1)
                .CreateReIndexJob(Arg.Any<Reference>(), Arg.Any<string>(), Arg.Is(SpecificationId), Arg.Is(JobId));

            await _publishedFundingSearchRepository
                .Received(0)
                .RunIndexer();
        }

        [TestMethod]
        public async Task PublishBatchProviderFundingResults_WhenAnUpdatePublishStatusThrowsException_TransactionCompensates()
        {
            string error = "Unable to update status.";

            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndPublishedProviders();
            AndUpdateStatusThrowsAnError(error);
            AndTemplateMapping();

            Func<Task> invocation = () => WhenPublishBatchProvidersMessageReceivedWithJobIdAndCorrelationId(BuildPublishProvidersRequest(_ => _.WithProviders(_publishedProviderIds)));

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be(error);

            await _publishedProviderVersionService
                .Received(1)
                .CreateReIndexJob(Arg.Any<Reference>(), Arg.Any<string>(), Arg.Is(SpecificationId), Arg.Is(JobId));

            await _publishedFundingSearchRepository
                .Received(0)
                .RunIndexer();
        }

        [TestMethod]
        public async Task PublishAllProviderFundingResults_WhenAnUpdatePublishFundingStatusThrowsException_TransactionCompensates()
        {
            string error = "Unable to update status.";

            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndPublishedProviders();
            AndUpdateFundingStatusThrowsAnError(error);
            AndTemplateMapping();

            Func<Task> invocation = WhenPublishAllProvidersMessageReceivedWithJobId;

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be(error);

            await _publishedProviderVersionService
                .Received(1)
                .CreateReIndexJob(Arg.Any<Reference>(), Arg.Any<string>(), Arg.Is(SpecificationId), Arg.Is(JobId));

            await _publishedFundingSearchRepository
                .Received(1)
                .RunIndexer();
        }

        [TestMethod]
        public async Task PublishBatchProviderFundingResults_WhenAnUpdatePublishFundingStatusThrowsException_TransactionCompensates()
        {
            string error = "Unable to update status.";

            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndPublishedProviders();
            AndUpdateFundingStatusThrowsAnError(error);
            AndTemplateMapping();

            Func<Task> invocation = () => WhenPublishBatchProvidersMessageReceivedWithJobIdAndCorrelationId(BuildPublishProvidersRequest(_ => _.WithProviders(_publishedProviderIds)));

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be(error);

            await _publishedProviderVersionService
                .Received(1)
                .CreateReIndexJob(Arg.Any<Reference>(), Arg.Any<string>(), Arg.Is(SpecificationId), Arg.Is(JobId));

            await _publishedFundingSearchRepository
                .Received(1)
                .RunIndexer();
        }

        [TestMethod]
        public void PublishAllProviderFundingResults_WhenNoTemplateMappingExists_ExceptionThrown()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndPublishedProviders();

            Func<Task> invocation = WhenPublishAllProvidersMessageReceivedWithJobId;

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be($"calculationMappingResult returned null for funding stream {FundingStreamId}");
        }

        [TestMethod]
        public void PublishBatchProviderFundingResults_WhenNoTemplateMappingExists_ExceptionThrown()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndPublishedProviders();

            Func<Task> invocation = () => WhenPublishBatchProvidersMessageReceivedWithJobIdAndCorrelationId(BuildPublishProvidersRequest(_ => _.WithProviders(_publishedProviderIds)));

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be($"calculationMappingResult returned null for funding stream {FundingStreamId}");
        }

        [TestMethod]
        public void PublishAllProviderFundingResults_GivenSpecificationDoesNotExist_ThrowsException()
        {
            GivenJobCanBeProcessed();

            Func<Task> invocation = WhenPublishAllProvidersMessageReceivedWithJobId;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .And
                .Message
                .Should()
                .Be($"Could not find specification with id '{SpecificationId}'");
        }

        [TestMethod]
        public void PublishBatchProviderFundingResults_GivenSpecificationDoesNotExist_ThrowsException()
        {
            GivenJobCanBeProcessed();

            Func<Task> invocation = () => WhenPublishBatchProvidersMessageReceivedWithJobIdAndCorrelationId(BuildPublishProvidersRequest(_ => _.WithProviders(_publishedProviderIds)));

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .And
                .Message
                .Should()
                .Be($"Could not find specification with id '{SpecificationId}'");
        }

        [TestMethod]
        public void PublishAllProviderFundingResults_GivenJobCannotBeProcessed_ThrowsException()
        {
            _jobManagement
                .RetrieveJobAndCheckCanBeProcessed(JobId)
                .Throws(new JobNotFoundException(string.Empty, JobId));

            Func<Task> invocation = WhenPublishAllProvidersMessageReceivedWithJobId;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .And
                .Message
                .Should()
                .Be($"Could not find the job with id: '{JobId}'");
        }

        [TestMethod]
        public void PublishBatchProviderFundingResults_GivenJobCannotBeProcessed_ThrowsException()
        {
            _jobManagement
                .RetrieveJobAndCheckCanBeProcessed(JobId)
                .Throws(new Exception());

            Func<Task> invocation = () => WhenPublishBatchProvidersMessageReceivedWithJobIdAndCorrelationId(BuildPublishProvidersRequest(_ => _.WithProviders(_publishedProviderIds)));

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .And
                .Message
                .Should()
                .Be($"Job can not be run '{JobId}'");
        }

        [TestMethod]
        public void CheckPrerequisitesForAllProvidersToBePublished_WhenPreReqsValidationErrors_ThrowsException()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndPublishedProviders();
            AndCalculationEngineRunningForPublishAllProviders();

            Func<Task> invocation = WhenPublishAllProvidersMessageReceivedWithJobId;

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be($"Publish All Providers with specification id: '{SpecificationId}' has prerequisites which aren't complete.");

            string[] prereqValidationErrors = new string[] { $"{JobConstants.DefinitionNames.RefreshFundingJob} is still running" };

            _jobManagement
                .Received(1)
                .UpdateJobStatus(JobId, 0, false, string.Join(", ", prereqValidationErrors));
        }

        [TestMethod]
        public void CheckPrerequisitesForBatchProvidersToBePublished_WhenPreReqsValidationErrors_ThrowsException()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndPublishedProviders();
            AndCalculationEngineRunningForPublishBatchProviders();

            Func<Task> invocation = () => WhenPublishBatchProvidersMessageReceivedWithJobIdAndCorrelationId(BuildPublishProvidersRequest(_ => _.WithProviders(_publishedProviderIds)));

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be($"Publish Batch Providers with specification id: '{SpecificationId}' has prerequisites which aren't complete.");

            string[] prereqValidationErrors = new string[] { $"{JobConstants.DefinitionNames.RefreshFundingJob} is still running" };

            _jobManagement
                .Received(1)
                .UpdateJobStatus(JobId, 0, false, string.Join(", ", prereqValidationErrors));
        }

        private void ThenEachProviderVersionHasTheFollowingVariationReasons(params VariationReason[] variationReasons)
        {
            foreach (PublishedProvider publishedProvider in _publishedProviders)
            {
                publishedProvider
                    .Current?
                    .VariationReasons
                    .Should()
                    .BeEquivalentTo(variationReasons, opt => opt.WithoutStrictOrdering());
            }
        }

        private void ThenUpdatePublishedFundingStatusHasTheFollowingVariationReasons(VariationReason variationReason)
        {
            _publishedFundingStatusUpdateService
                .Received(1)
                .UpdatePublishedFundingStatus(
                    Arg.Is<List<(PublishedFunding PublishedFunding, PublishedFundingVersion PublishedFundingVersion)>>(
                        _ => _.Select(item => item.PublishedFundingVersion)
                              .All(ppv => ppv.VariationReasons.Contains(variationReason))),
                    PublishedFundingStatus.Released
                );
        }

        private void GivenJobCanBeProcessed()
        {
            JobViewModel jobViewModel = NewJobViewModel(_ => _.WithJobId(JobId));

            _jobManagement.RetrieveJobAndCheckCanBeProcessed(JobId)
                .Returns(jobViewModel);
        }

        private void AndUpdateStatusThrowsAnError(string error)
        {
            _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(Arg.Any<IEnumerable<PublishedProvider>>(),
                    Arg.Any<Reference>(),
                    Arg.Any<PublishedProviderStatus>(),
                    Arg.Is(JobId),
                    Arg.Is(CorrelationId))
                .Throws(new Exception(error));
        }

        private void AndUpdateFundingStatusThrowsAnError(string error)
        {
            _publishedFundingStatusUpdateService.UpdatePublishedFundingStatus(Arg.Any<IEnumerable<(PublishedFunding, PublishedFundingVersion)>>(),
                    Arg.Is(PublishedFundingStatus.Released))
                .Throws(new Exception(error));
        }

        private void AndTheCsvGenerationJobsWereCreated(string specificationId, string fundingPeriodId, string fundingStreamId)
        {
            _publishFundingCsvJobsService.Received(1)
                .GenerateCsvJobs(GeneratePublishingCsvJobsCreationAction.Release,
                        Arg.Is(specificationId),
                        Arg.Is(fundingPeriodId),
                        Arg.Is<IEnumerable<string>>(_ => _.First() == fundingStreamId),
                        Arg.Any<string>(),
                        Arg.Any<Reference>());
        }

        private void AndThePublishDatasetsDataCopyJobsWereCreated(string specificationId)
        {
            _createPublishDatasetsDataCopyJob.Received(1)
                .CreateJob(Arg.Is(specificationId),
                        Arg.Any<Reference>(),
                        Arg.Is(CorrelationId),
                        Arg.Any<Dictionary<string, string>>(),
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<bool>());
        }

        private void AndTheProcessDatasetObsoleteItemsJobsWereCreated(string specificationId)
        {
            _createProcessDatasetObsoleteItemsJob.Received(1)
                .CreateJob(Arg.Is(specificationId),
                        Arg.Any<Reference>(),
                        Arg.Is(CorrelationId),
                        Arg.Any<Dictionary<string, string>>(),
                        Arg.Any<string>(),
                        Arg.Any<string>(),
                        Arg.Any<bool>());
        }

        private void AndSpecification(bool isSelectedForFunding = false)
        {
            _specificationSummary = NewSpecificationSummary(_ => _.WithId(SpecificationId)
            .WithFundingStreamIds(new[] { FundingStreamId })
            .WithFundingPeriodId(FundingPeriodId)
            .WithTemplateIds((FundingStreamId, "1.0"))
            .WithIsSelectedForFunding(isSelectedForFunding));

            _specificationsApiClient.GetSpecificationSummaryById(SpecificationId)
                .Returns(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, _specificationSummary));
        }

        private void AndTemplateMetadataContents()
        {
            _calculationTemplateIds = new[] { NewTemplateCalculation(), 
                NewTemplateCalculation(), 
                NewTemplateCalculation() };

            _fundingLines = new[] { NewTemplateFundingLine(fl => fl.WithCalculations(_calculationTemplateIds)) };

            _templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(_fundingLines));

            _policiesService
                .GetTemplateMetadataContents(FundingStreamId, _specificationSummary.FundingPeriod.Id,
                                             _specificationSummary.TemplateIds[FundingStreamId])
                .Returns(_templateMetadataContents);
        }

        private TemplateCalculation NewTemplateCalculation(Action<TemplateCalculationBuilder> setUp = null)
        {
            TemplateCalculationBuilder templateCalculationBuilder = new TemplateCalculationBuilder();

            setUp?.Invoke(templateCalculationBuilder);
            
            return templateCalculationBuilder.Build();
        }

        private void AndTemplateMapping()
        {
            TemplateMappingItem[] templateMappingItems = new[] {
                NewTemplateMappingItem(_ => _.WithTemplateId(_calculationTemplateIds[0].TemplateCalculationId)
                .WithCalculationId(_calculationResults[0].Id)),
                NewTemplateMappingItem(_ => _.WithTemplateId(_calculationTemplateIds[1].TemplateCalculationId)
                    .WithCalculationId(_calculationResults[1].Id)),
                NewTemplateMappingItem(_ => _.WithTemplateId(_calculationTemplateIds[2].TemplateCalculationId)
                    .WithCalculationId(_calculationResults[2].Id))
                };
            
            _templateMapping = NewTemplateMapping(_ => _.WithItems(templateMappingItems));

            _calculationsApiClient
                .GetTemplateMapping(_specificationSummary.Id, FundingStreamId)
                .Returns(new ApiResponse<TemplateMapping>(HttpStatusCode.OK, _templateMapping));
        }
        
        private void AndPublishedProviders(int majorVersion = 1, int minorVersion = 0, bool wasReleased = false)
        {
            Provider[] providers = new[] { NewProvider(), 
                NewProvider(), 
                NewProvider()
            };

            TemplateFundingLine templateFundingLine = _fundingLines[0];

            _publishedProviders = providers.Select(_ =>
                    NewPublishedProvider(pp =>
                    {
                        PublishedProviderVersion current = NewPublishedProviderVersion(ppv => ppv
                            .WithProvider(_)
                            .WithProviderId(_.ProviderId)
                            .WithTotalFunding(9)
                            .WithMajorVersion(majorVersion)
                            .WithMinorVersion(minorVersion)
                            .WithFundingLines(NewFundingLine(fl => fl.WithFundingLineCode(templateFundingLine.FundingLineCode)
                                .WithTemplateLineId(templateFundingLine.TemplateLineId)
                                .WithValue(9)))
                            .WithFundingCalculations(NewFundingCalculation(fc => fc.WithTemplateCalculationId(_calculationTemplateIds[0].TemplateCalculationId)
                                    .WithValue(_calculationTemplateIds[0].Value)),
                                NewFundingCalculation(fc => fc.WithTemplateCalculationId(_calculationTemplateIds[1].TemplateCalculationId)
                                    .WithValue(_calculationTemplateIds[1].Value)),
                                NewFundingCalculation(fc => fc.WithTemplateCalculationId(_calculationTemplateIds[2].TemplateCalculationId)
                                    .WithValue(_calculationTemplateIds[2].Value)))
                            .WithPublishedProviderStatus(PublishedProviderStatus.Approved)
                            .WithVariationReasons(new[] { VariationReason.AuthorityFieldUpdated })
                        );
                        
                        pp.WithCurrent(current);

                        if (wasReleased)
                        {
                            pp.WithReleased(current);
                        }
                        
                    })).ToList();

            _providerService
                .GetPublishedProviders(Arg.Is<Reference>(_ => _.Id == FundingStreamId),
                                                         _specificationSummary)
                .Returns((_publishedProviders.ToDictionary(_ => _.Current.ProviderId),
                                                           _publishedProviders.ToDictionary(_ => _.Current.ProviderId)));
        }

        private FundingLine NewFundingLine(Action<FundingLineBuilder> setUp = null)
        {
            FundingLineBuilder fundingLineBuilder = new FundingLineBuilder();

            setUp?.Invoke(fundingLineBuilder);
            
            return fundingLineBuilder.Build();
        }

        private FundingCalculation NewFundingCalculation(Action<FundingCalculationBuilder> setUp = null)
        {
            FundingCalculationBuilder fundingCalculationBuilder = new FundingCalculationBuilder();

            setUp?.Invoke(fundingCalculationBuilder);
            
            return fundingCalculationBuilder.Build();
        }

        private TemplateMappingItem NewTemplateMappingItem(Action<TemplateMappingItemBuilder> setUp = null)
        {
            TemplateMappingItemBuilder templateMappingItemBuilder = new TemplateMappingItemBuilder();

            setUp?.Invoke(templateMappingItemBuilder);
            
            return templateMappingItemBuilder.Build();
        }

        private PublishedFundingInput NewPublishedFundingInput(Action<PublishedFundingInputBuilder> setUp = null)
        {
            PublishedFundingInputBuilder publishedFundingInputBuilder = new PublishedFundingInputBuilder();

            setUp?.Invoke(publishedFundingInputBuilder);

            return publishedFundingInputBuilder.Build();
        }

        private PublishedFunding NewPublishedFunding(Action<PublishedFundingBuilder> setUp = null)
        {
            PublishedFundingBuilder publishedFundingBuilder = new PublishedFundingBuilder();

            setUp?.Invoke(publishedFundingBuilder);

            return publishedFundingBuilder.Build();
        }

        private PublishedFundingVersion NewPublishedFundingVersion(Action<PublishedFundingVersionBuilder> setUp = null)
        {
            PublishedFundingVersionBuilder publishedFundingVersionBuilder = new PublishedFundingVersionBuilder();

            setUp?.Invoke(publishedFundingVersionBuilder);

            return publishedFundingVersionBuilder.Build();
        }

        private void AndCalculationEngineRunningForPublishAllProviders()
        {
            string[] jobTypes = new string[] { 
                JobConstants.DefinitionNames.PublishedFundingUndoJob,
                JobConstants.DefinitionNames.RefreshFundingJob, 
                JobConstants.DefinitionNames.ApproveAllProviderFundingJob,
                JobConstants.DefinitionNames.ApproveBatchProviderFundingJob,
                JobConstants.DefinitionNames.ReIndexPublishedProvidersJob,
                JobConstants.DefinitionNames.PublishBatchProviderFundingJob
            };

            _jobsRunning
                .GetJobTypes(Arg.Is(SpecificationId), Arg.Is<IEnumerable<string>>(_ => _.All(jt => jobTypes.Contains(jt))))
                .Returns(new[] { JobConstants.DefinitionNames.RefreshFundingJob });
        }

        private void AndCalculationEngineRunningForPublishBatchProviders()
        {
            string[] jobTypes = new string[] {
                JobConstants.DefinitionNames.PublishedFundingUndoJob,
                JobConstants.DefinitionNames.RefreshFundingJob,
                JobConstants.DefinitionNames.ApproveAllProviderFundingJob,
                JobConstants.DefinitionNames.ApproveBatchProviderFundingJob,
                JobConstants.DefinitionNames.ReIndexPublishedProvidersJob,
                JobConstants.DefinitionNames.PublishAllProviderFundingJob
            };

            _jobsRunning
                .GetJobTypes(Arg.Is(SpecificationId), Arg.Is<IEnumerable<string>>(_ => _.All(jt => jobTypes.Contains(jt))))
                .Returns(new[] { JobConstants.DefinitionNames.RefreshFundingJob });
        }

        private void AndGeneratePublishedFunding()
        {
            IEnumerable<PublishedFundingVersion> publishedFundingVersions = _publishedProviders.Select(_ =>
                NewPublishedFundingVersion(pfv => pfv.WithSpecificationId(SpecificationId)))
                .ToList();

            IEnumerable<(PublishedFunding, PublishedFundingVersion)> publishedFunding = publishedFundingVersions.Select(_ =>
                (NewPublishedFunding(pf => pf.WithCurrent(_)),_)
            ).ToList();

            _publishedFundingGenerator
                .GeneratePublishedFunding(
                    Arg.Is<PublishedFundingInput>(_ => _.SpecificationId == SpecificationId),
                    Arg.Is<ICollection<PublishedProvider>>(_ => _.All(pp => _publishedProviders.Select(pp => pp.Current.ProviderId).Contains(pp.Current.ProviderId))),
                    _author,
                    _jobId,
                    _correlationId
                    )
                .Returns(publishedFunding);
        }

        private void AndCalculationResultsBySpecificationId()
        {
            decimal[] results = new[] { 2M, 3M, 4M };

            _calculationResults = results.Select(res => NewCalculationResult(cr => cr.WithValue(res))).ToArray();
        }

        private async Task WhenPublishAllProvidersMessageReceivedWithJobId()
        {
            Message message = NewMessage(_ => _.WithUserProperty("specification-id", 
                                                                 SpecificationId)
                .WithUserProperty("jobId", JobId)
                .WithUserProperty("sfa-correlationId", CorrelationId));

            await _publishService.Run(message);
        }

        private async Task WhenPublishBatchProvidersMessageReceivedWithJobIdAndCorrelationId(PublishedProviderIdsRequest publishProvidersRequest)
        {
            Message message = NewMessage(_ => _
                .WithUserProperty("specification-id",SpecificationId)
                .WithUserProperty("jobId",JobId)
                .WithUserProperty("sfa-correlationId", CorrelationId)
                .WithMessageBody(Encoding.UTF8.GetBytes(publishProvidersRequest.AsJson())));

            await _publishService.Run(message, async () =>
            {
                await _publishService.PublishProviderFundingResults(message, batched: true);
            });
        }

        private PublishedProviderIdsRequest BuildPublishProvidersRequest(Action<PublishedProviderIdsRequestBuilder> setUp = null)
        {
            PublishedProviderIdsRequestBuilder publishProvidersRequestBuilder = new PublishedProviderIdsRequestBuilder();

            setUp?.Invoke(publishProvidersRequestBuilder);

            return publishProvidersRequestBuilder.Build();
        }

        protected static TEnum NewRandomEnum<TEnum>(params TEnum[] except) where TEnum : struct => new RandomEnum<TEnum>(except);
    }
}
