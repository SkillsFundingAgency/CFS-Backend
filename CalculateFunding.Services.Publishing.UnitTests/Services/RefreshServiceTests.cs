using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Generators.OrganisationGroup.Interfaces;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Errors;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Services.Publishing.Variations;
using CalculateFunding.Services.Publishing.Variations.Strategies;
using FluentAssertions;
using FluentAssertions.Common;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog;
using CalculationResult = CalculateFunding.Models.Publishing.CalculationResult;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Publishing.UnitTests.Services
{
    [TestClass]
    public class RefreshServiceTests : ServiceTestsBase
    {
        private Mock<IPublishedProviderStatusUpdateService> _publishedProviderStatusUpdateService;
        private Mock<IPublishedFundingDataService> _publishedFundingDataService;
        private ISpecificationService _specificationService;
        private Mock<IProviderService> _providerService;
        private Mock<ICalculationResultsService> _calculationResultsService;
        private IPublishedProviderDataGenerator _publishedProviderDataGenerator;
        private IPublishedProviderDataPopulator _publishedProviderDataPopulator;
        private Mock<IProfilingService> _profilingService;
        private Mock<ILogger> _logger;
        private Mock<ICalculationsApiClient> _calculationsApiClient;
        private Mock<IPrerequisiteCheckerLocator> _prerequisiteCheckerLocator;
        private IPublishingResiliencePolicies _publishingResiliencePolicies;
        private Mock<IPublishProviderExclusionCheck> _providerExclusionCheck;
        private Mock<IFundingLineValueOverride> _fundingLineValueOverride;
        private Mock<IPublishedProviderIndexerService> _publishedProviderIndexerService;
        private Mock<IJobManagement> _jobManagement;
        private ITransactionFactory _transactionFactory;
        private RefreshService _refreshService;
        private SpecificationSummary _specificationSummary;
        private IEnumerable<Provider> _scopedProviders;
        private Mock<IPublishedProviderVersionService> _publishedProviderVersionService;
        private Mock<IPoliciesService> _policiesService;
        private IVariationService _variationService;
        private Mock<IGeneratePublishedFundingCsvJobsCreationLocator> _generateCsvJobsLocator;
        private Mock<IGeneratePublishedFundingCsvJobsCreation> _generateCsvJobsCreation;
        private TemplateMetadataContents _templateMetadataContents;
        private TemplateMapping _templateMapping;
        private IEnumerable<ProviderCalculationResult> _providerCalculationResults;
        private TemplateFundingLine[] _fundingLines;
        private CalculationResult[] _calculationResults;
        private TemplateCalculation[] _calculationTemplateIds;
        private IEnumerable<PublishedProvider> _publishedProviders;
        private Mock<ISpecificationsApiClient> _specificationsApiClient;
        private IVariationStrategyServiceLocator _variationStrategyServiceLocator;
        private IPublishedProviderErrorDetection _detection;
        private IDetectProviderVariations _detectProviderVariation;
        private IApplyProviderVariations _applyProviderVariation;
        private Mock<IRecordVariationErrors> _recordVariationErrors;
        private FundingConfiguration _fundingConfiguration;
        private Mock<ISpecificationFundingStatusService> _specificationFundingStatusService;
        private Mock<IJobsRunning> _jobsRunning;
        private Mock<ICalculationPrerequisiteCheckerService> _calculationApprovalCheckerService;
        private IMapper _mapper;
        private Mock<IOrganisationGroupGenerator> _organisationGroupGenerator;
        private string _providerIdVaried;
        private const string SpecificationId = "SpecificationId";
        private const string FundingPeriodId = "AY-2020";
        private const string JobId = "JobId";
        private const string CorrelationId = "CorrelationId";
        private const string FundingStreamId = "PSG";
        private const string Successor = "1234";
        private Mock<IReApplyCustomProfiles> _reApplyCustomProfiles;
        private Mock<IPoliciesApiClient> _policiesApiClient;
        private Mock<ICacheProvider> _cacheProvider;
        private string providerVersionId;
        private PublishedProvider _missingProvider;
        private ArraySegment<ProfileVariationPointer> _profileVariationPointers;
        private PublishingEngineOptions _publishingEngineOptions;

        [TestInitialize]
        public void Setup()
        {
            providerVersionId = NewRandomString();

            Mock<IConfiguration> configuration = new Mock<IConfiguration>();
            _publishedProviderStatusUpdateService = new Mock<IPublishedProviderStatusUpdateService>();
            _publishedFundingDataService = new Mock<IPublishedFundingDataService>();
            _publishingResiliencePolicies = new ResiliencePolicies
            {
                PublishedFundingRepository = Policy.NoOpAsync(),
                CalculationsApiClient = Policy.NoOpAsync(),
                SpecificationsApiClient = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
                CacheProvider = Policy.NoOpAsync(),
                PoliciesApiClient = Policy.NoOpAsync()
            };
            _specificationsApiClient = new Mock<ISpecificationsApiClient>();
            _specificationService = new SpecificationService(_specificationsApiClient.Object, _publishingResiliencePolicies);

            _profileVariationPointers = ArraySegment<ProfileVariationPointer>.Empty;

            _specificationsApiClient.Setup(_ =>
                _.GetProfileVariationPointers(It.IsAny<string>()))
                .ReturnsAsync(new ApiResponse<IEnumerable<ProfileVariationPointer>>(HttpStatusCode.OK, _profileVariationPointers));

            _providerService = new Mock<IProviderService>();
            _calculationResultsService = new Mock<ICalculationResultsService>();
            MapperConfiguration mappingConfig = new MapperConfiguration(c => c.AddProfile<PublishingServiceMappingProfile>());
            _mapper = mappingConfig.CreateMapper();
            _logger = new Mock<ILogger>();
            _publishedProviderDataGenerator = new PublishedProviderDataGenerator(_logger.Object, new FundingLineTotalAggregator(), _mapper);
            _publishedProviderDataPopulator = new PublishedProviderDataPopulator(_mapper, _logger.Object);
            _profilingService = new Mock<IProfilingService>();
            _calculationsApiClient = new Mock<ICalculationsApiClient>();
            _specificationFundingStatusService = new Mock<ISpecificationFundingStatusService>();
            _jobsRunning = new Mock<IJobsRunning>();
            _calculationApprovalCheckerService = new Mock<ICalculationPrerequisiteCheckerService>();
            _providerExclusionCheck = new Mock<IPublishProviderExclusionCheck>();
            _fundingLineValueOverride = new Mock<IFundingLineValueOverride>();
            _publishedProviderIndexerService = new Mock<IPublishedProviderIndexerService>();
            _jobManagement = new Mock<IJobManagement>();
            _prerequisiteCheckerLocator = new Mock<IPrerequisiteCheckerLocator>();
            _policiesService = new Mock<IPoliciesService>();
            _prerequisiteCheckerLocator.Setup(_ => _.GetPreReqChecker(PrerequisiteCheckerType.Refresh))
                .Returns(new RefreshPrerequisiteChecker(_specificationFundingStatusService.Object,
                _specificationService, _jobsRunning.Object, _calculationApprovalCheckerService.Object, _jobManagement.Object, _logger.Object));
            _organisationGroupGenerator = new Mock<IOrganisationGroupGenerator>();
            _transactionFactory = new TransactionFactory(_logger.Object, new TransactionResiliencePolicies { TransactionPolicy = Policy.NoOpAsync() });
            _publishedProviderVersionService = new Mock<IPublishedProviderVersionService>();
            _generateCsvJobsLocator = new Mock<IGeneratePublishedFundingCsvJobsCreationLocator>();
            _generateCsvJobsCreation = new Mock<IGeneratePublishedFundingCsvJobsCreation>();
            _reApplyCustomProfiles = new Mock<IReApplyCustomProfiles>();
            IDetectPublishedProviderErrors[] detectPublishedProviderErrors = typeof(IDetectPublishedProviderErrors).Assembly.GetTypes()
                .Where(_ => _.Implements(typeof(IDetectPublishedProviderErrors)) &&
                            !_.IsAbstract &&
                            _.GetConstructors().Any(ci => !ci.GetParameters().Any()))
                .Select(_ => (IDetectPublishedProviderErrors)Activator.CreateInstance(_))
                .ToArray();
            detectPublishedProviderErrors = detectPublishedProviderErrors.Concat(new[] { new TrustIdMismatchErrorDetector(_organisationGroupGenerator.Object, _mapper, _publishedFundingDataService.Object, _publishingResiliencePolicies) }).ToArray();
            _detection = new PublishedProviderErrorDetection(detectPublishedProviderErrors);
            _policiesApiClient = new Mock<IPoliciesApiClient>();
            _cacheProvider = new Mock<ICacheProvider>();

            //should we be using the concrete variations code here? they are all individually under test already I think
            IVariationStrategy[] variationStrategies = typeof(IVariationStrategy).Assembly.GetTypes()
                .Where(_ => _.Implements(typeof(IVariationStrategy)) &&
                            !_.IsAbstract &&
                            _.GetConstructors().Any(ci => !ci.GetParameters().Any()))
                .Select(_ => (IVariationStrategy)Activator.CreateInstance(_))
                .ToArray();

            variationStrategies = variationStrategies.Concat(new[] { new ClosureWithSuccessorVariationStrategy(_providerService.Object) }).ToArray();

            _variationStrategyServiceLocator = new VariationStrategyServiceLocator(variationStrategies);
            _detectProviderVariation = new ProviderVariationsDetection(_variationStrategyServiceLocator);
            _applyProviderVariation = new ProviderVariationsApplication(_publishingResiliencePolicies,
                _specificationsApiClient.Object,
                _policiesApiClient.Object,
                _cacheProvider.Object,
                new Mock<IProfilingApiClient>().Object,
                new Mock<IReProfilingRequestBuilder>().Object,
                new Mock<IReProfilingResponseMapper>().Object);
            _recordVariationErrors = new Mock<IRecordVariationErrors>();
            _variationService = new VariationService(_detectProviderVariation, _applyProviderVariation, _recordVariationErrors.Object, _logger.Object);
            _refreshService = new RefreshService(_publishedProviderStatusUpdateService.Object,
                _publishedFundingDataService.Object,
                _publishingResiliencePolicies,
                _specificationService,
                _providerService.Object,
                _calculationResultsService.Object,
                _publishedProviderDataGenerator,
                _profilingService.Object,
                _publishedProviderDataPopulator,
                _logger.Object,
                _calculationsApiClient.Object,
                _prerequisiteCheckerLocator.Object,
                _providerExclusionCheck.Object,
                _fundingLineValueOverride.Object,
                _jobManagement.Object,
                _publishedProviderIndexerService.Object,
                _variationService,
                _transactionFactory,
                _publishedProviderVersionService.Object,
                _policiesService.Object,
                _generateCsvJobsLocator.Object,
                _reApplyCustomProfiles.Object,
                new PublishingEngineOptions(configuration.Object),
                _detection);
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
            AndNewMissingPublishedProviders();
            AndUpdateStatusThrowsAnError(error);

            Func<Task> invocation = WhenMessageReceivedWithJobIdAndCorrelationId;

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be(error);

            _publishedProviderVersionService
                .Verify(_ => _.CreateReIndexJob(It.IsAny<Reference>(), It.IsAny<string>(), SpecificationId, JobId));
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
            AndNewMissingPublishedProviders();
            AndCsvJobService();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _publishedProviderStatusUpdateService
                .Verify(_ => _.UpdatePublishedProviderStatus(It.Is<IEnumerable<PublishedProvider>>(_ => _.Single().Current.ProviderId == _publishedProviders.Last().Current.ProviderId),
                    It.IsAny<Reference>(),
                    PublishedProviderStatus.Updated,
                    JobId,
                    CorrelationId,
                    It.IsAny<bool>()), Times.Once);

            AndTheCustomProfilesWereReApplied();
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
            AndNewMissingPublishedProviders();
            AndCsvJobService();

            GivenFundingConfiguration(new ProviderMetadataVariationStrategy());

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _publishedProviderStatusUpdateService
                .Verify(_ => _.UpdatePublishedProviderStatus(It.Is<IEnumerable<PublishedProvider>>(_ => _.Single().Current.ProviderId == _publishedProviders.Last().Current.ProviderId && _.Single().Current.VariationReasons.Single().Equals(VariationReason.NameFieldUpdated)),
                    It.IsAny<Reference>(),
                    PublishedProviderStatus.Updated,
                    JobId,
                    CorrelationId,
                    It.IsAny<bool>()), Times.Once);

            _missingProvider?.Current.VariationReasons
                .Should()
                .BeEquivalentTo(new[]
                    {
                        VariationReason.FundingUpdated, VariationReason.ProfilingUpdated
                    },
                    opt => opt.WithoutStrictOrdering());
        }

        [TestMethod]
        public void RefreshResults_WhenJobFailsPreRequisites_NonRetriableExceptionThrown()
        {
            GivenJobCannotBeProcessed();

            Func<Task> invocation = WhenMessageReceivedWithJobIdAndCorrelationId;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .And
                .Message
                .Should()
                .Be("Received job with id: 'JobId' is already in a completed state with status 'Superseded'");
        }

        [TestMethod]
        public void RefreshResults_WhenSpecificationNotFound_NonRetriableExceptionThrown()
        {
            GivenJobCanBeProcessed();

            Func<Task> invocation = WhenMessageReceivedWithJobIdAndCorrelationId;

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

            Func<Task> invocation = WhenMessageReceivedWithJobIdAndCorrelationId;

            Exception ex = invocation
                .Should()
                .Throw<Exception>()
                .Which;

            _logger
                .Verify(_ =>
                _.Error(ex, "Exception during calculation result lookup"));
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
            AndNewMissingPublishedProviders();

            Func<Task> invocation = WhenMessageReceivedWithJobIdAndCorrelationId;

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
            AndPublishedProviders(new PublishedProvider[0]);
            AndJobsRunning();

            Func<Task> invocation = WhenMessageReceivedWithJobIdAndCorrelationId;

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be($"Specification with id: '{SpecificationId} has prerequisites which aren't complete.");

            string[] prereqValidationErrors = new string[] { $"{JobConstants.DefinitionNames.CreateInstructAllocationJob} is still running", "Specification must have providers in scope." };

            _jobManagement
                .Verify(_ => _.UpdateJobStatus(JobId, It.IsAny<int>(), false, string.Join(", ", prereqValidationErrors)));

            _logger
                .Verify(_ => _.Information("No scoped providers found for refresh"));
        }

        [TestMethod]
        public void RefreshResults_WhenCallingGeneratorNoPublishedProviderResults_ExceptionThrown()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders();
            AndNewMissingPublishedProviders();
            AndNoScopedProviderCalculationResults();
            AndTemplateMapping();

            Func<Task> invocation = WhenMessageReceivedWithJobIdAndCorrelationId;

            Exception ex = invocation
                .Should()
                .Throw<Exception>()
                .Which;

            _logger
                .Verify(_ => _.Error(ex, "Exception during generating provider data"));
        }

        [TestMethod]
        public async Task RefreshResults_WhenNoTemplateContentsExistsForFundingStreamTemplateId_InformationLogged()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndScopedProviders((_) =>
            {
                _.Name = "New name";
            }, includeTemplateContents: false);
            AndScopedProviderCalculationResults();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _logger
                .Verify(_ => _.Information("Unable to locate template meta data contents for funding stream:'PSG' and template id:'1.0'"));
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
            AndPublishedProviders();
            AndNewMissingPublishedProviders();
            AndProfilingThrowsExeption();

            Func<Task> invocation = WhenMessageReceivedWithJobIdAndCorrelationId;

            Exception ex = invocation
                .Should()
                .Throw<Exception>()
                .Which;

            _logger
                .Verify(_ => _.Error(ex, "Exception during generating provider profiling"));
        }

        [TestMethod]
        public async Task RefreshResults_WhenProfilingExistingProvider_ShouldUseProfilePatternKeySavedInTheProvider()
        {
            string profilePatternKey = NewRandomString();
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders(profilePatternKeyPrefix: profilePatternKey);
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndPublishedProviders();
            AndNewMissingPublishedProviders();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _profilingService
                .Verify(_ =>
                _.ProfileFundingLines(
               It.IsAny<IEnumerable<FundingLine>>(),
               FundingStreamId,
               _specificationSummary.FundingPeriod.Id,
               It.Is<IEnumerable<ProfilePatternKey>>(x => x.All(pk => pk.Key.StartsWith(profilePatternKey))),
               null,
               null), Times.Exactly(3));
        }

        [TestMethod]
        public async Task RefreshResults_WhenMissingPublishedProvidersFound_ShouldUpdateNewProvidersOnlyOnce()
        {
            string profilePatternKey = NewRandomString();
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders();

            var newProvider = _publishedProviders.First();
            var existingProviders = _publishedProviders.Skip(1).ToList();

            AndPublishedProviders(existingProviders);
            AndNewMissingPublishedProviders(new[] { newProvider });
            AndScopedProviderCalculationResultsWithModifiedCalculationResults();
            AndTemplateMapping();
            AndCsvJobService();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _publishedProviderStatusUpdateService
                .Verify(_ =>
                _.UpdatePublishedProviderStatus(
                It.Is<IEnumerable<PublishedProvider>>(x => x.Count() == 2),
                It.IsAny<Reference>(),
                It.Is<PublishedProviderStatus>(x => x == PublishedProviderStatus.Updated),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()), Times.Once);

            _publishedProviderStatusUpdateService
                .Verify(_ =>
                _.UpdatePublishedProviderStatus(
                It.Is<IEnumerable<PublishedProvider>>(x => x.Count() == 1),
                It.IsAny<Reference>(),
                It.Is<PublishedProviderStatus>(x => x == PublishedProviderStatus.Draft),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<bool>()), Times.Once);
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
            AndNewMissingPublishedProviders();
            AndPublishedProviderExcluded();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _fundingLineValueOverride
                .Verify(_ =>
                _.TryOverridePreviousFundingLineValues(It.IsAny<PublishedProviderVersion>(), It.IsAny<GeneratedProviderResult>()), Times.Once);
        }

        [TestMethod]
        public void RefreshResults_GivenSpecificationDoesNotExist_ThrowsException()
        {
            GivenJobCanBeProcessed();

            Func<Task> invocation = WhenMessageReceivedWithJobIdAndCorrelationId;

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
            GivenJobNotFound();

            Func<Task> invocation = WhenMessageReceivedWithJobIdAndCorrelationId;

            invocation
                .Should()
                .Throw<NonRetriableException>()
                .And
                .Message
                .Should()
                .Be($"Could not find the job with id: '{JobId}'");
        }

        private void AndScopedProviderCalculationResults()
        {
            _providerCalculationResults = _scopedProviders.Select(_ =>
                NewProviderCalculationResult(pcr =>
                    pcr.WithProviderId(_.ProviderId)
                    .WithResults(_calculationResults)));

            _calculationResultsService.Setup(_ => _.GetCalculationResultsBySpecificationId(_specificationSummary.Id,
                It.Is<IEnumerable<string>>(_ => _scopedProviders.All(sp => _.Any(arg => arg == sp.ProviderId)))))
                .ReturnsAsync(_providerCalculationResults.ToDictionary(_ => _.ProviderId));
        }

        private void AndScopedProviderCalculationResultsWithModifiedCalculationResults()
        {
            var crs = _calculationResults.DeepCopy();
            foreach (var cr in crs)
            {
                cr.Value = (decimal.Parse(cr.Value?.ToString() ?? "0")) + 1m;
            }
            _providerCalculationResults = _scopedProviders.Select(_ =>
                NewProviderCalculationResult(pcr =>
                    pcr.WithProviderId(_.ProviderId)
                    .WithResults(crs)));

            _calculationResultsService.Setup(_ => _.GetCalculationResultsBySpecificationId(_specificationSummary.Id,
                It.Is<IEnumerable<string>>(_ => _scopedProviders.All(sp => _.Any(arg => arg == sp.ProviderId)))))
                .ReturnsAsync(_providerCalculationResults.ToDictionary(_ => _.ProviderId));
        }

        private void AndNoScopedProviderCalculationResults()
        {
            _calculationResultsService.Setup(_ => _.GetCalculationResultsBySpecificationId(_specificationSummary.Id,
                It.Is<IEnumerable<string>>(_ => _scopedProviders.All(sp => _.Any(arg => arg == sp.ProviderId)))))
                .ReturnsAsync(default(Dictionary<string, ProviderCalculationResult>));
        }

        private void AndTemplateMetadataContents()
        {
            _calculationTemplateIds = new[] { new TemplateCalculationBuilder().Build(), new TemplateCalculationBuilder().Build(), new TemplateCalculationBuilder().Build() };
            _fundingLines = new[] { NewTemplateFundingLine(fl => fl.WithCalculations(_calculationTemplateIds)) };
            _templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(_fundingLines));

            _policiesService
                .Setup(_ => _.GetTemplateMetadataContents(FundingStreamId, _specificationSummary.FundingPeriod.Id, _specificationSummary.TemplateIds[FundingStreamId]))
                .ReturnsAsync(_templateMetadataContents);
        }

        private void GivenFundingConfiguration(params IVariationStrategy[] variations)
        {
            _fundingConfiguration = new FundingConfiguration { Variations = variations.Select(_ => new FundingVariation { Name = _.Name }) };
            _policiesService
                .Setup(_ => _.GetFundingConfiguration(FundingStreamId, _specificationSummary.FundingPeriod.Id))
                .ReturnsAsync(_fundingConfiguration);
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
                .Setup(_ => _.GetTemplateMapping(_specificationSummary.Id, FundingStreamId))
                .ReturnsAsync(new ApiResponse<TemplateMapping>(HttpStatusCode.OK, _templateMapping));
        }

        private void AndPublishedProviderExcluded()
        {
            string providerToExcludeId = _publishedProviders.First().Current.ProviderId;
            _providerExclusionCheck
                .Setup(_ => _.ShouldBeExcluded(It.Is<GeneratedProviderResult>(_ => _.Provider.ProviderId == providerToExcludeId), It.IsAny<TemplateFundingLine[]>()))
                .Returns(new PublishedProviderExclusionCheckResult(providerToExcludeId, true));
        }

        private void AndProfilingThrowsExeption()
        {
            _profilingService.Setup(_ => _.ProfileFundingLines(
                It.IsAny<IEnumerable<FundingLine>>(),
                FundingStreamId,
                _specificationSummary.FundingPeriod.Id,
                It.IsAny<IEnumerable<ProfilePatternKey>>(),
                It.IsAny<string>(),
                It.IsAny<string>()))
                .Throws(new Exception());
        }

        private void AndCalculationResultsBySpecificationId()
        {
            decimal[] results = new[] { 2M, 3M, 4M };

            _calculationResults = results.Select(res => NewCalculationResult(cr => cr.WithValue(res))).ToArray();
        }

        private void GivenCalculationResultsBySpecificationIdThrowsException()
        {
            _calculationResultsService.Setup(_ => _.GetCalculationResultsBySpecificationId(_specificationSummary.Id,
                It.Is<IEnumerable<string>>(_ => _scopedProviders.All(sp => _.Any(arg => arg == sp.ProviderId)))))
                .Throws(new Exception());
        }

        private void AndScopedProviders(Action<Provider> variationAction = null, string profilePatternKeyPrefix = null, bool includeTemplateContents = true)
        {
            _scopedProviders = new[] { NewProvider(), NewProvider(), NewProvider() };

            _publishedProviders = _scopedProviders.DeepCopy().Select(_ =>
                    NewPublishedProvider(pp => pp.WithCurrent(
                        NewPublishedProviderVersion(ppv => ppv.WithProvider(_.DeepCopy())
                            .WithFundingStreamId(FundingStreamId)
                            .WithProviderId(_.ProviderId)
                            .WithTotalFunding(9)
                            .WithProfilePatternKeys(includeTemplateContents ? new ProfilePatternKey() { FundingLineCode = _fundingLines[0].FundingLineCode, Key = $"{profilePatternKeyPrefix}-{NewRandomString()}" } : null)
                            .WithFundingLines(includeTemplateContents ? new[] { new FundingLine { FundingLineCode = _fundingLines[0].FundingLineCode, TemplateLineId = _fundingLines[0].TemplateLineId, Value = 9 } } : null)
                            .WithFundingCalculations(includeTemplateContents ? new[] {new FundingCalculation { Value = _calculationResults[0].Value, TemplateCalculationId = _calculationTemplateIds[0].TemplateCalculationId },
                                                            new FundingCalculation { Value = _calculationResults[1].Value, TemplateCalculationId = _calculationTemplateIds[1].TemplateCalculationId },
                                                            new FundingCalculation { Value = _calculationResults[2].Value, TemplateCalculationId = _calculationTemplateIds[2].TemplateCalculationId } } : null)))
                        .WithReleased(NewPublishedProviderVersion(ppv => ppv.WithProvider(_.DeepCopy())
                                .WithFundingStreamId(FundingStreamId)
                                .WithProviderId(_.ProviderId)
                                .WithTotalFunding(9)
                                .WithFundingLines(includeTemplateContents ? new[] { new FundingLine { FundingLineCode = _fundingLines[0].FundingLineCode, TemplateLineId = _fundingLines[0].TemplateLineId, Value = 9 } } : null)
                                .WithFundingCalculations(includeTemplateContents ? new[] {new FundingCalculation { Value = _calculationResults[0].Value, TemplateCalculationId = _calculationTemplateIds[0].TemplateCalculationId },
                                    new FundingCalculation { Value = _calculationResults[1].Value, TemplateCalculationId = _calculationTemplateIds[1].TemplateCalculationId },
                                    new FundingCalculation { Value = _calculationResults[2].Value, TemplateCalculationId = _calculationTemplateIds[2].TemplateCalculationId } } : null))))).ToList();

            Provider providerToVary = _scopedProviders.Last();
            _providerIdVaried = providerToVary.ProviderId;

            variationAction?.Invoke(providerToVary);

            _providerService.Setup(_ => _.GetScopedProvidersForSpecification(_specificationSummary.Id, _specificationSummary.ProviderVersionId))
                .ReturnsAsync(_scopedProviders.ToDictionary(_ => _.ProviderId));
        }

        private void AndPublishedProviders(IEnumerable<PublishedProvider> publishedProviders = null)
        {
            _publishedFundingDataService
                .Setup(_ => _.GetCurrentPublishedProviders(FundingStreamId, _specificationSummary.FundingPeriod.Id, It.IsAny<string[]>()))
                .ReturnsAsync(publishedProviders ?? _publishedProviders);
        }

        private void AndNewMissingPublishedProviders(IEnumerable<PublishedProvider> publishedProviders = null)
        {
            _providerService
                .Setup(_ => _.GenerateMissingPublishedProviders(It.IsAny<IEnumerable<Provider>>(), It.IsAny<SpecificationSummary>(), It.IsAny<Reference>(), It.IsAny<IDictionary<string, PublishedProvider>>()))
                .Returns((publishedProviders ?? new List<PublishedProvider>()).ToDictionary(x => x.Current.ProviderId));
        }

        private void AndJobsRunning()
        {
            string[] jobTypes = new string[]
            {
                JobConstants.DefinitionNames.CreateInstructAllocationJob,
                JobConstants.DefinitionNames.ApproveAllProviderFundingJob,
                JobConstants.DefinitionNames.ApproveBatchProviderFundingJob,
                JobConstants.DefinitionNames.PublishAllProviderFundingJob,
                JobConstants.DefinitionNames.PublishBatchProviderFundingJob,
                JobConstants.DefinitionNames.ReIndexPublishedProvidersJob
            };

            _jobsRunning
                .Setup(_ => _.GetJobTypes(SpecificationId, It.Is<IEnumerable<string>>(_ => _.All(jt => jobTypes.Contains(jt)))))
                .ReturnsAsync(new[] { JobConstants.DefinitionNames.CreateInstructAllocationJob });
        }

        private void AndUpdateStatusThrowsAnError(string error)
        {
            _publishedProviderStatusUpdateService.Setup(_ => _.UpdatePublishedProviderStatus(It.IsAny<IEnumerable<PublishedProvider>>(),
                    It.IsAny<Reference>(),
                    It.IsAny<PublishedProviderStatus>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<bool>()))
                .Throws(new Exception(error));
        }

        private void GivenJobCanBeProcessed()
        {
            JobViewModel jobViewModel = NewJobViewModel(_ => _.WithJobId(JobId));

            _jobManagement.Setup(_ => _.RetrieveJobAndCheckCanBeProcessed(JobId))
                .ReturnsAsync(jobViewModel);
        }

        private void AndCsvJobService()
        {
            _generateCsvJobsLocator
                .Setup(_ => _.GetService(It.IsAny<GeneratePublishingCsvJobsCreationAction>()))
                .Returns(_generateCsvJobsCreation.Object);
        }

        private void GivenJobNotFound()
        {
            _jobManagement
                .Setup(_ => _.RetrieveJobAndCheckCanBeProcessed(JobId))
                .Throws(new JobNotFoundException($"Could not find the job with id: '{JobId}'", JobId));
        }

        private void GivenJobCannotBeProcessed()
        {
            JobViewModel jobViewModel = NewJobViewModel(_ => _.WithCompletionStatus(CompletionStatus.Superseded));

            _jobManagement.Setup(_ => _.RetrieveJobAndCheckCanBeProcessed(JobId))
                .Throws(
                new JobAlreadyCompletedException(
                    $"Received job with id: 'JobId' is already in a completed state with status {jobViewModel.CompletionStatus}", jobViewModel));
        }

        private void AndSpecification()
        {
            _specificationSummary = NewSpecificationSummary(_ => _
                .WithId(SpecificationId)
                .WithPublishStatus(PublishStatus.Approved)
                .WithFundingStreamIds(new[] { FundingStreamId })
                .WithFundingPeriodId(FundingPeriodId)
                .WithTemplateIds((FundingStreamId, "1.0"))
                .WithProviderVersionId(providerVersionId));

            _specificationsApiClient.Setup(_ => _.GetSpecificationSummaryById(SpecificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, _specificationSummary));
        }

        private async Task WhenMessageReceivedWithJobIdAndCorrelationId()
        {
            Message message = NewMessage(_ => _.WithUserProperty("specification-id", SpecificationId)
                .WithUserProperty("jobId", JobId)
                .WithUserProperty("sfa-correlationId", CorrelationId));

            await _refreshService.Run(message);
        }

        private void AndTheCustomProfilesWereReApplied()
        {
            foreach (PublishedProvider publishedProvider in _publishedProviders)
            {
                _reApplyCustomProfiles.Verify(_ => _.ProcessPublishedProvider(publishedProvider.Current),
                    Times.Once);
            }
        }
    }
}
