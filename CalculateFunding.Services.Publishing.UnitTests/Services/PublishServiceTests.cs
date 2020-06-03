﻿using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.ServiceBus.Interfaces;
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
using System.Threading.Tasks;
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
        private IPublishedFundingContentsPersistanceService _publishedFundingContentsPersistanceService;
        private IPublishedProviderContentPersistanceService _publishedProviderContentsPersistanceService;
        private IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private ISearchRepository<PublishedFundingIndex> _publishedFundingSearchRepository;
        private ICalculationsApiClient _calculationsApiClient;
        private IProviderService _providerService;
        private IJobManagement _jobManagement;
        private IGeneratePublishedFundingCsvJobsCreationLocator _generateCsvJobsLocator;
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
        private IJobsRunning _jobsRunning;
        
        private const string SpecificationId = "SpecificationId";
        private const string FundingPeriodId = "AY-2020";
        private const string JobId = "JobId";
        private const string FundingStreamId = "PSG";
        private const string CorrelationId = "CorrelationId";

        private string _providerId;
        private string[] _providerIds;

        [TestInitialize]
        public void Setup()
        {
            _providerId = NewRandomString();
            _providerIds = new[] { _providerId };

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
            _publishedFundingContentsPersistanceService = Substitute.For<IPublishedFundingContentsPersistanceService>();
            _publishedProviderContentsPersistanceService = Substitute.For<IPublishedProviderContentPersistanceService>();
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
            _generateCsvJobsLocator = Substitute.For<IGeneratePublishedFundingCsvJobsCreationLocator>();
            _mapper = Substitute.For<IMapper>();
            _transactionFactory = new TransactionFactory(_logger, new TransactionResiliencePolicies { TransactionPolicy = Policy.NoOpAsync() });
            _publishedProviderVersionService = Substitute.For<IPublishedProviderVersionService>();
            _publishedFundingDataService = Substitute.For<IPublishedFundingDataService>();
            _policiesService = Substitute.For<IPoliciesService>();
            _organisationGroupGenerator = Substitute.For<IOrganisationGroupGenerator>();
            _publishedFundingChangeDetectorService = Substitute.For<IPublishedFundingChangeDetectorService>();
            _publishedFundingDateService = Substitute.For<IPublishedFundingDateService>();
            _publishedFundingDataService = Substitute.For<IPublishedFundingDataService>();

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
                _publishedFundingContentsPersistanceService,
                _publishedProviderContentsPersistanceService,
                _publishedProviderStatusUpdateService,
                _providerService,
                _publishedFundingSearchRepository,
                _calculationsApiClient,
                _logger,
                _jobManagement,
                _generateCsvJobsLocator,
                _transactionFactory,
                _publishedProviderVersionService,
                _publishedFundingService,
                _publishedFundingDataService,
                _policiesService
            );
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
                .CreateReIndexJob(Arg.Any<Reference>(), Arg.Any<string>());

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

            Func<Task> invocation = () => WhenPublishBatchProvidersMessageReceivedWithJobIdAndCorrelationId(BuildPublishProvidersRequest(_ => _.WithProviders(_providerIds)));

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be(error);

            await _publishedProviderVersionService
                .Received(1)
                .CreateReIndexJob(Arg.Any<Reference>(), Arg.Any<string>());

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
                .CreateReIndexJob(Arg.Any<Reference>(), Arg.Any<string>());

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

            Func<Task> invocation = () => WhenPublishBatchProvidersMessageReceivedWithJobIdAndCorrelationId(BuildPublishProvidersRequest(_ => _.WithProviders(_providerIds)));

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be(error);

            await _publishedProviderVersionService
                .Received(1)
                .CreateReIndexJob(Arg.Any<Reference>(), Arg.Any<string>());

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

            Func<Task> invocation = () => WhenPublishBatchProvidersMessageReceivedWithJobIdAndCorrelationId(BuildPublishProvidersRequest(_ => _.WithProviders(_providerIds)));

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

            Func<Task> invocation = () => WhenPublishBatchProvidersMessageReceivedWithJobIdAndCorrelationId(BuildPublishProvidersRequest(_ => _.WithProviders(_providerIds)));

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
                .Be("Job can not be run");
        }

        [TestMethod]
        public void PublishBatchProviderFundingResults_GivenJobCannotBeProcessed_ThrowsException()
        {
            _jobManagement
                .RetrieveJobAndCheckCanBeProcessed(JobId)
                .Throws(new JobNotFoundException(string.Empty, JobId));

            Func<Task> invocation = () => WhenPublishBatchProvidersMessageReceivedWithJobIdAndCorrelationId(BuildPublishProvidersRequest(_ => _.WithProviders(_providerIds)));

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .And
                .Message
                .Should()
                .Be("Job can not be run");
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
                .Be($"Specification with id: '{SpecificationId} has prerequisites which aren't complete.");

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

            Func<Task> invocation = () => WhenPublishBatchProvidersMessageReceivedWithJobIdAndCorrelationId(BuildPublishProvidersRequest(_ => _.WithProviders(_providerIds)));

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be($"Specification with id: '{SpecificationId} has prerequisites which aren't complete.");

            string[] prereqValidationErrors = new string[] { $"{JobConstants.DefinitionNames.RefreshFundingJob} is still running" };

            _jobManagement
                .Received(1)
                .UpdateJobStatus(JobId, 0, false, string.Join(", ", prereqValidationErrors));
        }

        private void GivenJobCanBeProcessed()
        {
            JobViewModel jobViewModel = NewJobViewModel();

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
                    Arg.Any<Reference>(),
                    Arg.Is(PublishedFundingStatus.Released),
                    Arg.Is(JobId),
                    Arg.Is(CorrelationId))
                .Throws(new Exception(error));
        }

        private void AndSpecification()
        {
            _specificationSummary = NewSpecificationSummary(_ => _.WithId(SpecificationId)
            .WithFundingStreamIds(new[] { FundingStreamId })
            .WithFundingPeriodId(FundingPeriodId)
            .WithTemplateIds((FundingStreamId, "1.0")));

            _specificationsApiClient.GetSpecificationSummaryById(SpecificationId)
                .Returns(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, _specificationSummary));
        }

        private void AndTemplateMetadataContents()
        {
            _calculationTemplateIds = new[] { new TemplateCalculationBuilder().Build(), 
                                              new TemplateCalculationBuilder().Build(), 
                                              new TemplateCalculationBuilder().Build() };

            _fundingLines = new[] { NewTemplateFundingLine(fl => fl.WithCalculations(_calculationTemplateIds)) };

            _templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(_fundingLines));

            _policiesService
                .GetTemplateMetadataContents(FundingStreamId, _specificationSummary.FundingPeriod.Id,
                                             _specificationSummary.TemplateIds[FundingStreamId])
                .Returns(_templateMetadataContents);
        }

        private void AndTemplateMapping()
        {
            TemplateMappingItem[] templateMappingItems = new[] { new TemplateMappingItem { TemplateId = _calculationTemplateIds[0].TemplateCalculationId,
                                                                        CalculationId = _calculationResults[0].Id },
                                                                 new TemplateMappingItem { TemplateId = _calculationTemplateIds[1].TemplateCalculationId,
                                                                        CalculationId = _calculationResults[1].Id },
                                                                 new TemplateMappingItem { TemplateId = _calculationTemplateIds[2].TemplateCalculationId,
                                                                        CalculationId = _calculationResults[2].Id } };
            _templateMapping = NewTemplateMapping(_ => _.WithItems(templateMappingItems));

            _calculationsApiClient
                .GetTemplateMapping(_specificationSummary.Id, FundingStreamId)
                .Returns(new ApiResponse<TemplateMapping>(HttpStatusCode.OK, _templateMapping));
        }
        private void AndPublishedProviders()
        {
            Provider[] providers = new[] { NewProvider(), 
                NewProvider(), 
                NewProvider()
            };

            _publishedProviders = providers.Select(_ =>
                    NewPublishedProvider(pp => pp.WithCurrent(
                        NewPublishedProviderVersion(ppv => ppv.WithProvider(_)
                            .WithProviderId(_.ProviderId)
                            .WithTotalFunding(9)
                            .WithFundingLines(new[] { new FundingLine { FundingLineCode = _fundingLines[0].FundingLineCode, 
                                                                        TemplateLineId = _fundingLines[0].TemplateLineId, 
                                                                        Value = 9 } })
                            .WithFundingCalculations(new[] {new FundingCalculation { Value = _calculationResults[0].Value, 
                                                                                        TemplateCalculationId = _calculationTemplateIds[0].TemplateCalculationId },
                                                            new FundingCalculation { Value = _calculationResults[1].Value, 
                                                                                        TemplateCalculationId = _calculationTemplateIds[1].TemplateCalculationId },
                                                            new FundingCalculation { Value = _calculationResults[2].Value, 
                                                                                        TemplateCalculationId = _calculationTemplateIds[2].TemplateCalculationId } })
                            .WithPublishedProviderStatus(PublishedProviderStatus.Approved))))).ToList();

            _providerService
                .GetPublishedProviders(Arg.Is<Reference>(_ => _.Id == FundingStreamId),
                                                         _specificationSummary)
                .Returns((_publishedProviders.ToDictionary(_ => _.Current.ProviderId),
                                                           _publishedProviders.ToDictionary(_ => _.Current.ProviderId)));
        }

        private void AndCalculationEngineRunningForPublishAllProviders()
        {
            string[] jobTypes = new string[] { 
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

            await _publishService.PublishProviderFundingResults(message);
        }

        private async Task WhenPublishBatchProvidersMessageReceivedWithJobIdAndCorrelationId(PublishProvidersRequest publishProvidersRequest)
        {
            Message message = NewMessage(_ => _
                .WithUserProperty("specification-id",SpecificationId)
                .WithUserProperty("jobId",JobId)
                .WithUserProperty("sfa-correlationId", CorrelationId)
                .WithUserProperty(
                    JobConstants.MessagePropertyNames.PublishProvidersRequest, JsonExtensions.AsJson(publishProvidersRequest)));

            await _publishService.PublishProviderFundingResults(message, batched: true);
        }

        private PublishProvidersRequest BuildPublishProvidersRequest(Action<PublishProvidersRequestBuilder> setUp = null)
        {
            PublishProvidersRequestBuilder publishProvidersRequestBuilder = new PublishProvidersRequestBuilder();

            setUp?.Invoke(publishProvidersRequestBuilder);

            return publishProvidersRequestBuilder.Build();
        }

        private static RandomString NewRandomString()
        {
            return new RandomString();
        }
    }
}
