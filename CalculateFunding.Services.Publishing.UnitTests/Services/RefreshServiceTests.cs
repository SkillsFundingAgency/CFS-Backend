using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Services.UnitTests;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using FluentAssertions.Common;
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
    public class RefreshServiceTests : ServiceTestsBase
    {
        private IPublishedProviderStatusUpdateService _publishedProviderStatusUpdateService;
        private IPublishedFundingDataService _publishedFundingDataService;
        private ISpecificationService _specificationService;
        private IProviderService _providerService;
        private ICalculationResultsService _calculationResultsService;
        private IPublishedProviderDataGenerator _publishedProviderDataGenerator;
        private IPublishedProviderDataPopulator _publishedProviderDataPopulator;
        private IProfilingService _profilingService;
        private ILogger _logger;
        private ICalculationsApiClient _calculationsApiClient;
        private IPrerequisiteCheckerLocator _prerequisiteCheckerLocator;
        private IPublishingResiliencePolicies _publishingResiliencePolicies;
        private IPublishProviderExclusionCheck _providerExclusionCheck;
        private IFundingLineValueOverride _fundingLineValueOverride;
        private IPublishedProviderIndexerService _publishedProviderIndexerService;
        private IJobManagement _jobManagement;
        private ITransactionFactory _transactionFactory;
        private RefreshService _refreshService;
        private SpecificationSummary _specificationSummary;
        private IEnumerable<Provider> _scopedProviders;
        private IPublishedProviderVersionService _publishedProviderVersionService;
        private IPoliciesService _policiesService;
        private IVariationService _variationService;
        private IGeneratePublishedFundingCsvJobsCreationLocator _generateCsvJobsLocator;
        private TemplateMetadataContents _templateMetadataContents;
        private TemplateMapping _templateMapping;
        private IEnumerable<ProviderCalculationResult> _providerCalculationResults;
        private TemplateFundingLine[] _fundingLines;
        private CalculationResult[] _calculationResults;
        private TemplateCalculation[] _calculationTemplateIds;
        private IEnumerable<PublishedProvider> _publishedProviders;
        private IJobsApiClient _jobsApiClient;
        private ISpecificationsApiClient _specificationsApiClient;
        private IVariationStrategyServiceLocator _variationStrategyServiceLocator;
        private IDetectProviderVariations _detectProviderVariation;
        private IApplyProviderVariations _applyProviderVariation;
        private IRecordVariationErrors _recordVariationErrors;
        private IPublishingFeatureFlag _publishingFeatureFlag;
        private FundingConfiguration _fundingConfiguration;
        private ISpecificationFundingStatusService _specificationFundingStatusService;
        private ICalculationEngineRunningChecker _calculationEngineRunningChecker;
        private ICalculationPrerequisiteCheckerService _calculationApprovalCheckerService;
        private IMapper _mapper;
        private string _providerIdVaried;
        private const string SpecificationId = "SpecificationId";
        private const string JobId = "JobId";
        private const string FundingStreamId = "PSG";
        private const string Successor = "1234";

        [TestInitialize]
        public void Setup()
        {
            _publishedProviderStatusUpdateService = Substitute.For<IPublishedProviderStatusUpdateService>();
            _publishedFundingDataService = Substitute.For<IPublishedFundingDataService>();
            _publishingResiliencePolicies = new ResiliencePolicies { PublishedFundingRepository = Policy.NoOpAsync(), 
                CalculationsApiClient = Policy.NoOpAsync(),
                SpecificationsApiClient = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync()};
            _specificationsApiClient = Substitute.For<ISpecificationsApiClient>();
            _specificationService = new SpecificationService(_specificationsApiClient, _publishingResiliencePolicies);
            _providerService = Substitute.For<IProviderService>();
            _calculationResultsService = Substitute.For<ICalculationResultsService>();
            MapperConfiguration mappingConfig = new MapperConfiguration(c => c.AddProfile<PublishingServiceMappingProfile>());
            _mapper = mappingConfig.CreateMapper();
            _logger = Substitute.For<ILogger>();
            _publishedProviderDataGenerator = new PublishedProviderDataGenerator(new FundingLineTotalAggregator(), _mapper);
            _publishedProviderDataPopulator = new PublishedProviderDataPopulator(_mapper, _logger);
            _profilingService = Substitute.For<IProfilingService>();
            _calculationsApiClient = Substitute.For<ICalculationsApiClient>();
            _specificationFundingStatusService = Substitute.For<ISpecificationFundingStatusService>();
            _calculationEngineRunningChecker = Substitute.For<ICalculationEngineRunningChecker>();
            _calculationApprovalCheckerService = Substitute.For<ICalculationPrerequisiteCheckerService>();
            _providerExclusionCheck = Substitute.For<IPublishProviderExclusionCheck>();
            _fundingLineValueOverride = Substitute.For<IFundingLineValueOverride>();
            _publishedProviderIndexerService = Substitute.For<IPublishedProviderIndexerService>();
            _jobsApiClient = Substitute.For<IJobsApiClient>();
            _jobManagement = new JobManagement(_jobsApiClient, _logger, new JobManagementResiliencePolicies { JobsApiClient = Policy.NoOpAsync() });
            _prerequisiteCheckerLocator = Substitute.For<IPrerequisiteCheckerLocator>();
            _prerequisiteCheckerLocator.GetPreReqChecker(PrerequisiteCheckerType.Refresh)
                .Returns(new RefreshPrerequisiteChecker(_specificationFundingStatusService, _specificationService, _calculationEngineRunningChecker, _calculationApprovalCheckerService, _jobManagement, _logger));
            _transactionFactory = new TransactionFactory(_logger, new TransactionResiliencePolicies { TransactionPolicy = Policy.NoOpAsync() });
            _publishedProviderVersionService = Substitute.For<IPublishedProviderVersionService>();
            _generateCsvJobsLocator = Substitute.For<IGeneratePublishedFundingCsvJobsCreationLocator>();
            _policiesService = Substitute.For<IPoliciesService>();

            IVariationStrategy[] variationStrategies = typeof(IVariationStrategy).Assembly.GetTypes()
                .Where(_ => _.Implements(typeof(IVariationStrategy)) &&
                            _.GetConstructors().Any(ci => !ci.GetParameters().Any()))
                .Select(_ => (IVariationStrategy)Activator.CreateInstance(_))
                .ToArray();

            variationStrategies = variationStrategies.Concat(new[] { new ClosureWithSuccessorVariationStrategy(_providerService) }).ToArray();

            _variationStrategyServiceLocator = new VariationStrategyServiceLocator(variationStrategies);
            _detectProviderVariation = new ProviderVariationsDetection(_variationStrategyServiceLocator);
            _applyProviderVariation = new ProviderVariationsApplication(_publishingResiliencePolicies, _specificationsApiClient);
            _recordVariationErrors = Substitute.For<IRecordVariationErrors>();
            _publishingFeatureFlag = Substitute.For<IPublishingFeatureFlag>();
            _variationService = new VariationService(_detectProviderVariation, _applyProviderVariation, _recordVariationErrors, _logger, _publishingFeatureFlag);
            _refreshService = new RefreshService(_publishedProviderStatusUpdateService,
                _publishedFundingDataService,
                _publishingResiliencePolicies,
                _specificationService,
                _providerService,
                _calculationResultsService,
                _publishedProviderDataGenerator,
                _profilingService,
                _publishedProviderDataPopulator,
                _logger,
                _calculationsApiClient,
                _prerequisiteCheckerLocator,
                _providerExclusionCheck,
                _fundingLineValueOverride,
                _jobManagement,
                _publishedProviderIndexerService,
                _variationService,
                _transactionFactory,
                _publishedProviderVersionService,
                _policiesService,
                _generateCsvJobsLocator);
        }

        [TestMethod]
        public async Task RefreshResults_WhenAnUpdatePublishStatusThrowsException_TransactionCompensates()
        {
            string error = "Unable to update status.";

            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders((_) =>
            {
                _.Name = "New name";
            });
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndPublishedProviders();
            AndUpdateStatusThrowsAnError(error);

            Func<Task> invocation = WhenMessageReceivedWithJobId;
 
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
        }

        [TestMethod]
        public async Task RefreshResults_WhenAnUpdatePublishStatusCompletesWithoutError_PublishedProviderUpdated()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders((_) =>
            {
                _.Name = "New name";
            });
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndPublishedProviders();

            await WhenMessageReceivedWithJobId();

            await _publishedProviderStatusUpdateService
                .Received(1)
                .UpdatePublishedProviderStatus(Arg.Is<IEnumerable<PublishedProvider>>(_ => _.Single().Current.ProviderId == _publishedProviders.Last().Current.ProviderId), 
                    Arg.Any<Reference>(),
                    Arg.Is(PublishedProviderStatus.Updated),
                    Arg.Is(JobId));
        }

        [TestMethod]
        public async Task RefreshResults_WhenAnUpdatePublishStatusCompletesWithoutErrorAndVariationsEnabled_PublishedProviderUpdatedAndVariationReasonSet()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders((_) =>
            {
                _.Name = "New name";
            });
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndPublishedProviders();
            GivenVariationsEnabled();
            GivenFundingConfiguration(new ProviderMetadataVariationStrategy());

            await WhenMessageReceivedWithJobId();

            await _publishedProviderStatusUpdateService
                .Received(1)
                .UpdatePublishedProviderStatus(Arg.Is<IEnumerable<PublishedProvider>>(_ => _.Single().Current.ProviderId == _publishedProviders.Last().Current.ProviderId && _.Single().Current.VariationReasons.Single().Equals(VariationReason.NameFieldUpdated)),
                    Arg.Any<Reference>(),
                    Arg.Is(PublishedProviderStatus.Updated),
                    Arg.Is(JobId));
        }

        [TestMethod]
        public async Task RefreshResults_WhenProviderClosedWithSuccessorButNoPointerSet_RecordsErrors()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders((_) =>
            {
                _.Successor = Successor;
                _.Status = "Closed";
            });
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            GivenPublishedProviderClosedWithSuccessor();
            AndPublishedProviders();
            GivenVariationsEnabled();
            GivenFundingConfiguration(new ClosureWithSuccessorVariationStrategy(_providerService));

            Func<Task> invocation = WhenMessageReceivedWithJobId;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .And
                .Message
                .Should()
                .Be($"Unable to refresh funding. Variations generated {_variationService.ErrorCount} errors. Check log for details");

            await _recordVariationErrors.Received(1)
                .RecordVariationErrors(Arg.Is<IEnumerable<string>>(_ => _.First().Equals($"Unable to transfer remaining profiles for provider id {_providerIdVaried}") && _.Skip(1).First().Equals($"Unable to zero remaining profiles for provider id {_providerIdVaried}")), Arg.Is(SpecificationId));

        }

        [TestMethod]
        public void RefreshResults_WhenJobFailsPreRequisites_NonRetriableExceptionThrown()
        {
            GivenJobCannotBeProcessed();

            Func<Task> invocation = WhenMessageReceivedWithJobId;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .And
                .Message
                .Should()
                .Be("Job cannot be run. Received job with id: 'JobId' is already in a completed state with status Superseded");
        }

        [TestMethod]
        public void RefreshResults_WhenSpecificationNotFound_NonRetriableExceptionThrown()
        {
            GivenJobCanBeProcessed();

            Func<Task> invocation = WhenMessageReceivedWithJobId;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .And
                .Message
                .Should()
                .Be("Could not find specification with id 'SpecificationId'");
        }

        [TestMethod]
        public void RefreshResults_WhenExceptionThrownWhileRetrievingCalcs_ErrorLogged()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders();
            GivenCalculationResultsBySpecificationIdThrowsException();

            Func<Task> invocation = WhenMessageReceivedWithJobId;

            Exception ex = invocation
                .Should()
                .Throw<Exception>()
                .Which;

            _logger
                .Received(1)
                .Error(Arg.Is(ex), "Exception during calculation result lookup");
        }

        [TestMethod]
        public void RefreshResults_WhenNoTemplateMappingExists_ExceptionThrown()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders();
            AndScopedProviderCalculationResults();

            Func<Task> invocation = WhenMessageReceivedWithJobId;

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be($"calculationMappingResult returned null for funding stream {FundingStreamId}");
        }

        [TestMethod]
        public void CheckPrerequisitesForSpecificationToBeRefreshed_WhenPreReqsValidationErrors_ThrowsException()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders();
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndPublishedProviders();
            AndCalculationEngineRunning();

            Func<Task> invocation = WhenMessageReceivedWithJobId;

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be($"Specification with id: '{SpecificationId} has prerequisites which aren't complete.");

            string[] prereqValidationErrors = new string[] { "Calculation engine is still running" };

            _jobsApiClient.Received(1)
                .AddJobLog(Arg.Is(JobId), Arg.Is<JobLogUpdateModel>(_ => _.CompletedSuccessfully == false && _.Outcome == string.Join(", ", prereqValidationErrors)));
        }

        [TestMethod]
        public void RefreshResults_WhenCallingGeneratorNoPublishedProviderResults_ExceptionThrown()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders();
            AndNoScopedProviderCalculationResults();
            AndTemplateMapping();

            Func<Task> invocation = WhenMessageReceivedWithJobId;

            Exception ex = invocation
                .Should()
                .Throw<Exception>()
                .Which;

            _logger
                .Received(1)
                .Error(Arg.Is(ex), "Exception during generating provider data");
        }

        [TestMethod]
        public async Task RefreshResults_WhenNoTemplateContentsExistsForFundingStreamTemplateId_InformationLogged()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();

            await WhenMessageReceivedWithJobId();

            _logger
                .Received(1)
                .Information(Arg.Is("Unable to locate template meta data contents for funding stream:'PSG' and template id:'1.0'"));
        }

        [TestMethod]
        public void RefreshResults_WhenExceptionThrownWhenProfiling_InformationLogged()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders();
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndProfilingThrowsExeption();

            Func<Task> invocation = WhenMessageReceivedWithJobId;

            Exception ex = invocation
                .Should()
                .Throw<Exception>()
                .Which;

            _logger
                .Received(1)
                .Error(Arg.Is(ex), "Exception during generating provider profiling");
        }

        [TestMethod]
        public async Task RefreshResults_GivenPublishedProviderExcluded_FundingLineOverrideCalled()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders();
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndPublishedProviders();
            AndPublishedProviderExcluded();

            await WhenMessageReceivedWithJobId();

            _fundingLineValueOverride
                .Received(1)
                .TryOverridePreviousFundingLineValues(Arg.Any<PublishedProviderVersion>(), Arg.Any<GeneratedProviderResult>());

        }

        [TestMethod]
        public void RefreshResults_GivenSpecificationDoesNotExist_ThrowsException()
        {
            GivenJobCanBeProcessed();

            Func<Task> invocation = WhenMessageReceivedWithJobId;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .And
                .Message
                .Should()
                .Be($"Could not find specification with id '{SpecificationId}'");
        }

        [TestMethod]
        public void PublishResults_GivenJobCannotBeProcessed_ThrowsException()
        {
            Func<Task> invocation = WhenMessageReceivedWithJobId;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .And
                .Message
                .Should()
                .Be($"Job cannot be run. Could not find the job with id: '{JobId}'");
        }

        private void AndScopedProviderCalculationResults()
        {
            _providerCalculationResults = _scopedProviders.Select(_ =>
                NewProviderCalculationResult(pcr =>
                    pcr.WithProviderId(_.ProviderId)
                    .WithResults(_calculationResults)));

            _calculationResultsService.GetCalculationResultsBySpecificationId(_specificationSummary.Id,
                Arg.Is<IEnumerable<string>>(_ => _scopedProviders.All(sp => _.Any(arg => arg == sp.ProviderId))))
                .Returns(_providerCalculationResults.ToDictionary(_ => _.ProviderId));
        }

        private void AndNoScopedProviderCalculationResults()
        {
            _calculationResultsService.GetCalculationResultsBySpecificationId(_specificationSummary.Id,
                Arg.Is<IEnumerable<string>>(_ => _scopedProviders.All(sp => _.Any(arg => arg == sp.ProviderId))))
                .Returns(default(Dictionary<string, ProviderCalculationResult>));
        }

        private void AndTemplateMetadataContents()
        {
            _calculationTemplateIds = new[] { new TemplateCalcationBuilder().Build(), new TemplateCalcationBuilder().Build(), new TemplateCalcationBuilder().Build() };
            _fundingLines = new[] { NewTemplateFundingLine(fl => fl.WithCalculations(_calculationTemplateIds))};
            _templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(_fundingLines));

            _policiesService
                .GetTemplateMetadataContents(FundingStreamId, _specificationSummary.TemplateIds[FundingStreamId])
                .Returns(_templateMetadataContents);
        }

        private void GivenFundingConfiguration(params IVariationStrategy[] variations)
        {
            _fundingConfiguration = new FundingConfiguration { Variations = variations.Select(_ => new FundingVariation { Name = _.Name })};
            _policiesService
                .GetFundingConfiguration(FundingStreamId, _specificationSummary.FundingPeriod.Id)
                .Returns(_fundingConfiguration);
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

        private void AndPublishedProviderExcluded()
        {
            string providerToExcludeId = _publishedProviders.First().Current.ProviderId;
            _providerExclusionCheck.ShouldBeExcluded(Arg.Is<ProviderCalculationResult>(_ => _.ProviderId == providerToExcludeId), Arg.Any<TemplateMapping>(), Arg.Any<TemplateCalculation[]>())
                .Returns(new PublishedProviderExclusionCheckResult(providerToExcludeId, true));
        }

        private void AndProfilingThrowsExeption()
        {
            _profilingService.ProfileFundingLines(Arg.Any<IEnumerable<FundingLine>>(), FundingStreamId, _specificationSummary.FundingPeriod.Id)
                .Throws(new Exception());
        }

        private void AndCalculationResultsBySpecificationId()
        {
            decimal[] results = new[] { 2M, 3M, 4M };

            _calculationResults = results.Select(res => NewCalculationResult(cr => cr.WithValue(res))).ToArray();
        }

        private void GivenCalculationResultsBySpecificationIdThrowsException()
        {
            _calculationResultsService.GetCalculationResultsBySpecificationId(_specificationSummary.Id,
                Arg.Is<IEnumerable<string>>(_ => _scopedProviders.All(sp => _.Any(arg => arg == sp.ProviderId))))
                .Throws(new Exception());
        }

        private void AndScopedProviders(Action<Provider> variationAction = null)
        {
            _scopedProviders = new[] { NewProvider(), NewProvider(), NewProvider() };

            _publishedProviders = _scopedProviders.DeepCopy().Select(_ =>
                    NewPublishedProvider(pp => pp.WithCurrent(
                        NewPublishedProviderVersion(ppv => ppv.WithProvider(_.DeepCopy())
                            .WithProviderId(_.ProviderId)
                            .WithTotalFunding(9)
                            .WithFundingLines(new[] { new FundingLine { FundingLineCode = _fundingLines[0].FundingLineCode, TemplateLineId  = _fundingLines[0].TemplateLineId, Value = 9} })
                            .WithFundingCalculations(new[] {new FundingCalculation { Value = _calculationResults[0].Value, TemplateCalculationId = _calculationTemplateIds[0].TemplateCalculationId },
                                                            new FundingCalculation { Value = _calculationResults[1].Value, TemplateCalculationId = _calculationTemplateIds[1].TemplateCalculationId },
                                                            new FundingCalculation { Value = _calculationResults[2].Value, TemplateCalculationId = _calculationTemplateIds[2].TemplateCalculationId } }))))).ToList();

            Provider providerToVary = _scopedProviders.Last();
            _providerIdVaried = providerToVary.ProviderId;

            variationAction?.Invoke(providerToVary);

            _providerService.GetScopedProvidersForSpecification(_specificationSummary.Id, _specificationSummary.ProviderVersionId)
                .Returns(_scopedProviders.ToDictionary(_ => _.ProviderId));
        }

        private void GivenPublishedProviderClosedWithSuccessor()
        {
            PublishedProvider predecessor = _publishedProviders.Single(_ => _.Current.ProviderId == _providerIdVaried);
            PublishedProvider missingProvider = predecessor.DeepCopy();
            missingProvider.Current.ProviderId = Successor;
            missingProvider.Current.Provider.ProviderId = Successor;
            missingProvider.Current.Provider.Status = "Open";

            _providerService
                .CreateMissingPublishedProviderForPredecessor(Arg.Is<PublishedProvider>(_ => _.Current.ProviderId == predecessor.Current.ProviderId), Arg.Is(Successor))
                .Returns(missingProvider);
        }

        private void AndPublishedProviders()
        {
            _publishedFundingDataService
                .GetCurrentPublishedProviders(FundingStreamId, _specificationSummary.FundingPeriod.Id)
                .Returns(_publishedProviders);
        }

        private void AndCalculationEngineRunning()
        {
            string[] jobTypes = new string[] { JobConstants.DefinitionNames.CreateInstructAllocationJob, JobConstants.DefinitionNames.ApproveFunding,
                JobConstants.DefinitionNames.PublishProviderFundingJob,
                JobConstants.DefinitionNames.ReIndexPublishedProvidersJob };

            _calculationEngineRunningChecker
                .IsCalculationEngineRunning(Arg.Is(SpecificationId), Arg.Is<IEnumerable<string>>(_ => _.All(jt => jobTypes.Contains(jt))))
                .Returns(true);
        }

        private void GivenVariationsEnabled()
        {
            _publishingFeatureFlag.IsVariationsEnabled()
                .Returns(true);
        }

        private void AndUpdateStatusThrowsAnError(string error)
        {
            _publishedProviderStatusUpdateService.UpdatePublishedProviderStatus(Arg.Any<IEnumerable<PublishedProvider>>(),
                    Arg.Any<Reference>(),
                    Arg.Any<PublishedProviderStatus>(),
                    Arg.Any<string>())
                .Throws(new Exception(error));
        }

        private void ThenJobStatusUpdated()
        {
            _jobManagement
                .Received(1)
                .UpdateJobStatus(Arg.Is(JobId), 0, 0, null, null);
        }

        private void GivenJobCanBeProcessed()
        {
            JobViewModel jobViewModel = NewJobViewModel();

            _jobsApiClient.GetJobById(JobId)
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, jobViewModel));
        }

        private void GivenJobCannotBeProcessed()
        {
            JobViewModel jobViewModel = NewJobViewModel(_ => _.WithCompletionStatus(CompletionStatus.Superseded));

            _jobsApiClient.GetJobById(JobId)
                .Returns(new ApiResponse<JobViewModel>(HttpStatusCode.OK, jobViewModel));
        }

        private void AndSpecification()
        {
            _specificationSummary = NewSpecificationSummary(_ => _.WithId(SpecificationId).WithFundingStreamIds(new[] { FundingStreamId }).WithTemplateIds((FundingStreamId, "1.0")));

            _specificationsApiClient.GetSpecificationSummaryById(SpecificationId)
                .Returns(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, _specificationSummary));
        }

        private async Task WhenMessageReceivedWithJobId()
        {
            Message message = NewMessage(_ => _.WithUserProperty("specification-id", SpecificationId).WithUserProperty("jobId", JobId));

            await _refreshService.RefreshResults(message);
        }
    }
}
