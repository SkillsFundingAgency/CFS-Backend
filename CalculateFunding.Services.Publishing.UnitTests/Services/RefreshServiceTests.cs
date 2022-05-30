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
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Polly;
using Serilog;
using CalculationResult = CalculateFunding.Models.Publishing.CalculationResult;
using FundingLine = CalculateFunding.Models.Publishing.FundingLine;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;

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
        private IPublishProviderExclusionCheck _providerExclusionCheck;
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
        private Mock<IPublishedFundingCsvJobsService> _publishFundingCsvJobsService;
        private TemplateMetadataContents _templateMetadataContents;
        private TemplateMapping _templateMapping;
        private IEnumerable<ProviderCalculationResult> _providerCalculationResults;
        private TemplateFundingLine[] _fundingLines;
        private CalculationResult[] _calculationResults;
        private TemplateCalculation[] _calculationTemplateIds;
        private IEnumerable<PublishedProvider> _publishedProviders;
        private Mock<ISpecificationsApiClient> _specificationsApiClient;
        private VariationStrategyServiceLocator _variationStrategyServiceLocator;
        private IPublishedProviderErrorDetection _detection;
        private IDetectProviderVariations _detectProviderVariation;
        private IApplyProviderVariations _applyProviderVariation;
        private Mock<IRecordVariationErrors> _recordVariationErrors;
        private FundingConfiguration _fundingConfiguration;
        private Mock<ISpecificationFundingStatusService> _specificationFundingStatusService;
        private Mock<IJobsRunning> _jobsRunning;
        private Mock<ICalculationPrerequisiteCheckerService> _calculationApprovalCheckerService;
        private IMapper _mapper;
        private Mock<IOrganisationGroupService> _organisationGroupService;
        private Mock<IChannelOrganisationGroupGeneratorService> _channelOrganisationGroupGeneratorService;
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
        private IEnumerable<ProfileVariationPointer> _profileVariationPointers;
        private Mock<IBatchProfilingService> _batchProfilingService;
        private Mock<ICalculationsService> _calculationsService;
        private IRefreshStateService _refreshStateService;
        private Mock<IFundingStreamPaymentDatesRepository> _fundingStreamPaymentDatesRepository;

        [TestInitialize]
        public void Setup()
        {
            providerVersionId = NewRandomString();

            _publishedProviderStatusUpdateService = new Mock<IPublishedProviderStatusUpdateService>();
            _publishedFundingDataService = new Mock<IPublishedFundingDataService>();
            _publishingResiliencePolicies = new ResiliencePolicies
            {
                PublishedFundingRepository = Policy.NoOpAsync(),
                CalculationsApiClient = Policy.NoOpAsync(),
                SpecificationsApiClient = Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Policy.NoOpAsync(),
                CacheProvider = Policy.NoOpAsync(),
                PoliciesApiClient = Policy.NoOpAsync(),
                FundingStreamPaymentDatesRepository = Policy.NoOpAsync()
            };
            _specificationsApiClient = new Mock<ISpecificationsApiClient>();
            _specificationService = new SpecificationService(_specificationsApiClient.Object, _publishingResiliencePolicies);

            _profileVariationPointers = ArraySegment<ProfileVariationPointer>.Empty;

            _providerService = new Mock<IProviderService>();
            _calculationResultsService = new Mock<ICalculationResultsService>();
            MapperConfiguration mappingConfig = new MapperConfiguration(c => c.AddProfile<PublishingServiceMappingProfile>());
            _mapper = mappingConfig.CreateMapper();
            _logger = new Mock<ILogger>();
            _publishedProviderDataGenerator = new PublishedProviderDataGenerator(_logger.Object, new FundingLineTotalAggregator(new Mock<IFundingLineRoundingSettings>().Object), _mapper);
            _publishedProviderDataPopulator = new PublishedProviderDataPopulator(_mapper, _logger.Object);
            _profilingService = new Mock<IProfilingService>();
            _calculationsApiClient = new Mock<ICalculationsApiClient>();
            _specificationFundingStatusService = new Mock<ISpecificationFundingStatusService>();
            _jobsRunning = new Mock<IJobsRunning>();
            _calculationApprovalCheckerService = new Mock<ICalculationPrerequisiteCheckerService>();
            _providerExclusionCheck = new PublishedProviderExclusionCheck();
            _fundingLineValueOverride = new Mock<IFundingLineValueOverride>();
            _publishedProviderIndexerService = new Mock<IPublishedProviderIndexerService>();
            _jobManagement = new Mock<IJobManagement>();
            _prerequisiteCheckerLocator = new Mock<IPrerequisiteCheckerLocator>();
            _policiesService = new Mock<IPoliciesService>();
            _calculationsService = new Mock<ICalculationsService>();
            _fundingStreamPaymentDatesRepository = new Mock<IFundingStreamPaymentDatesRepository>();

            _prerequisiteCheckerLocator.Setup(_ => _.GetPreReqChecker(PrerequisiteCheckerType.Refresh))
                .Returns(new RefreshPrerequisiteChecker(
                    _specificationFundingStatusService.Object,
                _specificationService,
                _jobsRunning.Object,
                _calculationApprovalCheckerService.Object,
                _jobManagement.Object,
                _logger.Object,
                _policiesService.Object,
                _profilingService.Object,
                _calculationsService.Object,
                _publishingResiliencePolicies,
                _fundingStreamPaymentDatesRepository.Object));
            _organisationGroupService = new Mock<IOrganisationGroupService>();
            _channelOrganisationGroupGeneratorService = new Mock<IChannelOrganisationGroupGeneratorService>();
            _transactionFactory = new TransactionFactory(_logger.Object, new TransactionResiliencePolicies { TransactionPolicy = Policy.NoOpAsync() });
            _publishedProviderVersionService = new Mock<IPublishedProviderVersionService>();
            _publishFundingCsvJobsService = new Mock<IPublishedFundingCsvJobsService>();
            _reApplyCustomProfiles = new Mock<IReApplyCustomProfiles>();
            IDetectPublishedProviderErrors[] detectPublishedProviderErrors = typeof(IDetectPublishedProviderErrors).Assembly.GetTypes()
                .Where(_ => _.Implements(typeof(IDetectPublishedProviderErrors)) &&
                            !_.IsAbstract &&
                            _.GetConstructors().Any(ci => !ci.GetParameters().Any()))
                .Select(_ => (IDetectPublishedProviderErrors)Activator.CreateInstance(_))
                .ToArray();
            IErrorDetectionStrategyLocator errorDetectionStrategyLocator = new ErrorDetectionStrategyLocator(detectPublishedProviderErrors);
            _detection = new PublishedProviderErrorDetection(errorDetectionStrategyLocator);
            _policiesApiClient = new Mock<IPoliciesApiClient>();
            _cacheProvider = new Mock<ICacheProvider>();

            //should we be using the concrete variations code here? they are all individually under test already I think
            IVariationStrategy[] variationStrategies = typeof(IVariationStrategy).Assembly.GetTypes()
                .Where(_ => _.Implements(typeof(IVariationStrategy)) &&
                            !_.IsAbstract &&
                            _.GetConstructors().Any(ci => !ci.GetParameters().Any()))
                .Select(_ => (IVariationStrategy)Activator.CreateInstance(_))
                .ToArray();

            variationStrategies = variationStrategies.Concat(new[] { (IVariationStrategy)new ClosureWithSuccessorVariationStrategy(_providerService.Object) }).ToArray();

            _batchProfilingService = new Mock<IBatchProfilingService>();

            _variationStrategyServiceLocator = new VariationStrategyServiceLocator(variationStrategies);
            _detectProviderVariation = new ProviderVariationsDetection(_variationStrategyServiceLocator, _policiesService.Object, _profilingService.Object, _logger.Object);
            _applyProviderVariation = new ProviderVariationsApplication(_publishingResiliencePolicies,
                _specificationsApiClient.Object,
                _policiesApiClient.Object,
                _cacheProvider.Object,
                new Mock<IProfilingApiClient>().Object,
                new Mock<IReProfilingRequestBuilder>().Object,
                new Mock<IReProfilingResponseMapper>().Object);
            _recordVariationErrors = new Mock<IRecordVariationErrors>();
            _variationService = new VariationService(_detectProviderVariation, _applyProviderVariation, _recordVariationErrors.Object, _logger.Object);
            _refreshStateService = new RefreshStateService(_logger.Object, _publishedProviderStatusUpdateService.Object, _publishedProviderIndexerService.Object, _publishedFundingDataService.Object);

            var mapper = new MapperConfiguration(_ =>
            {
                _.AddProfile<PublishingServiceMappingProfile>();
            }).CreateMapper();

            _refreshService = new RefreshService(_publishedFundingDataService.Object,
                _publishingResiliencePolicies,
                _specificationService,
                _providerService.Object,
                _calculationResultsService.Object,
                _publishedProviderDataGenerator,
                _publishedProviderDataPopulator,
                _logger.Object,
                _calculationsApiClient.Object,
                _prerequisiteCheckerLocator.Object,
                _providerExclusionCheck,
                _fundingLineValueOverride.Object,
                new InformationLinesAggregationService(new InformationLineAggregator(), _logger.Object),
                _jobManagement.Object,
                _variationService,
                _transactionFactory,
                _publishedProviderVersionService.Object,
                _policiesService.Object,
                _reApplyCustomProfiles.Object,
                _detection,
                _batchProfilingService.Object,
                _publishFundingCsvJobsService.Object,
                _refreshStateService,
                _organisationGroupService.Object,
                _channelOrganisationGroupGeneratorService.Object);
        }

        [TestMethod]
        public void RefreshResults_WhenAnUpdatePublishStatusThrowsException_TransactionCompensates()
        {
            string error = "Unable to update status.";

            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders((_) =>
            {
                _.Last().Name = "New name";
            });
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndPublishedProviders();
            AndNewMissingPublishedProviders();
            AndUpdateStatusThrowsAnError(error);
            AndProfilePatternsForFundingStreamAndFundingPeriod();

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
        public async Task RefreshResults_WhenForceUpdateOnRefreshAnUpdatePublishStatusCompletesWithoutError_NoPublishedProvidersUpdated()
        {
            GivenJobCanBeProcessed();
            AndSpecification(true);
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders();
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndPublishedProviders();
            AndNewMissingPublishedProviders();
            AndProfilePatternsForFundingStreamAndFundingPeriod();
            AndClearForceUpdateOnRefresh();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _publishedProviderStatusUpdateService
                .Verify(_ => _.UpdatePublishedProviderStatus(It.Is<IEnumerable<PublishedProvider>>(_ => _.Count() == _scopedProviders.Count()),
                    It.IsAny<Reference>(),
                    PublishedProviderStatus.Updated,
                    JobId,
                    CorrelationId,
                    It.IsAny<bool>()), Times.Never);

            AndClearForceUpdateOnRefreshCalled();
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
                _.Last().Name = "New name";
            });
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndPublishedProviders();
            AndNewMissingPublishedProviders();
            AndProfilePatternsForFundingStreamAndFundingPeriod();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _publishedProviderStatusUpdateService
                .Verify(_ => _.UpdatePublishedProviderStatus(It.Is<IEnumerable<PublishedProvider>>(_ => _.Count() == 1 && _.Single().Current.ProviderId == _publishedProviders.Last().Current.ProviderId),
                    It.IsAny<Reference>(),
                    PublishedProviderStatus.Updated,
                    JobId,
                    CorrelationId,
                    It.IsAny<bool>()), Times.Once);

            AndTheCustomProfilesWereReApplied();
            AndTheCsvGenerationJobsWereCreated(SpecificationId, FundingPeriodId);
        }

        [TestMethod]
        public async Task RefreshResults_WhenAnUpdatePublishStatusWithAnExistingDraftPublishedProviderCompletesWithoutError_PublishedProviderStatusSetToDraft()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders((_) =>
            {
                _.Last().Name = "New name";
            },
            publishedProviderStatus: PublishedProviderStatus.Draft);
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndPublishedProviders();
            AndNewMissingPublishedProviders();
            AndProfilePatternsForFundingStreamAndFundingPeriod();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _publishedProviderStatusUpdateService
                .Verify(_ => _.UpdatePublishedProviderStatus(It.Is<IEnumerable<PublishedProvider>>(_ => _.Count() == 1 && _.Single().Current.ProviderId == _publishedProviders.Last().Current.ProviderId),
                    It.IsAny<Reference>(),
                    PublishedProviderStatus.Draft,
                    JobId,
                    CorrelationId,
                    It.IsAny<bool>()), Times.Once);

            AndTheCustomProfilesWereReApplied();
        }

        [TestMethod]
        public async Task RefreshResults_WhenPublishedProviderVariesDueToCustomProfiles_NoErrorLogged()
        {
            string providerId = string.Empty;
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders((_) =>
            {
                _.Last().Status = "Proposed to open";
                providerId = _.Last().ProviderId;
            });
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            _publishedProviders.Last().Current.CustomProfiles = new[] { new FundingLineProfileOverrides() };
            AndPublishedProviders(_publishedProviders);
            AndNewMissingPublishedProviders();
            AndProfilePatternsForFundingStreamAndFundingPeriod();
            GivenFundingConfiguration(new ClosureWithSuccessorVariationStrategy(_providerService.Object));
            AndFundingConfiguration();
            AndFundingConfigurationIndicativeStatuses("Proposed to open", "Pending approval");
            AndTheFundingPeriod();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _publishedProviderStatusUpdateService
                .Verify(_ => _.UpdatePublishedProviderStatus(It.Is<IEnumerable<PublishedProvider>>(_ => _.Count() == 1
                        && _.Single().Current.ProviderId == providerId
                        && _.Single().Current.Errors.Where(_ => !_.Type.Equals(PublishedProviderErrorType.NoApplicableVariation) &&
                        !_.Type.Equals(PublishedProviderErrorType.NoApplicableProfilingUpdateVariation) &&
                        !_.Type.Equals(PublishedProviderErrorType.ProfilingConsistencyCheckFailure)).AnyWithNullCheck()),
                    It.IsAny<Reference>(),
                    PublishedProviderStatus.Updated,
                    JobId,
                    CorrelationId,
                    It.IsAny<bool>()), Times.Never);
        }

        [TestMethod]
        public async Task RefreshResults_WhenPublishedProviderVariesDueToUntrackedVariationProperty_NoErrorLogged()
        {
            string providerId = string.Empty;
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders((_) =>
            {
                _.Last().TrustName = $"{_.Last().TrustName}_updated";
                providerId = _.Last().ProviderId;
            });
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndPublishedProviders(_publishedProviders);
            AndNewMissingPublishedProviders();
            AndProfilePatternsForFundingStreamAndFundingPeriod();
            GivenFundingConfiguration(new ClosureWithSuccessorVariationStrategy(_providerService.Object));
            AndFundingConfiguration();
            AndFundingConfigurationIndicativeStatuses("Proposed to open", "Pending approval");
            AndTheFundingPeriod();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _publishedProviderStatusUpdateService
                .Verify(_ => _.UpdatePublishedProviderStatus(It.Is<IEnumerable<PublishedProvider>>(_ => _.Count() == 1
                        && _.Single().Current.ProviderId == providerId
                        && _.Single().Current.Errors.AnyWithNullCheck(_ => _.Type.Equals(PublishedProviderErrorType.ProfilingConsistencyCheckFailure))),
                    It.IsAny<Reference>(),
                    PublishedProviderStatus.Updated,
                    JobId,
                    CorrelationId,
                    It.IsAny<bool>()), Times.Once);
        }

        [TestMethod]
        public async Task RefreshResults_WhenPublishedProviderVariesButNoApplicableProfilingUpdatedVariationDetected_ErrorLogged()
        {
            string providerId = string.Empty;
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId(new decimal?[] { 1M, 2M, 4M } );
            AndTemplateMetadataContents();
            AndScopedProviders((_) =>
            {
                providerId = _.Last().ProviderId;
            });
            _publishedProviders.ForEach(_ => {
                if (_.Current.ProviderId != providerId)
                {
                    _.Current.TotalFunding = 7M;
                    _.Current.FundingLines.First().Value = 7M;
                }
            });
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndTheVariationPointers();
            AndPublishedProviders();
            AndNewMissingPublishedProviders();
            AndProfilePatternsForFundingStreamAndFundingPeriod();
            GivenFundingConfiguration(new IndicativeToLiveVariationStrategy());
            AndFundingConfiguration("NoApplicableProfilingUpdateVariationErrorDetector");
            AndFundingConfigurationIndicativeStatuses("Proposed to open", "Pending approval");
            AndTheFundingPeriod();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _publishedProviderStatusUpdateService
                .Verify(_ => _.UpdatePublishedProviderStatus(It.Is<IEnumerable<PublishedProvider>>(_ => 
                        _.AnyWithNullCheck() && 
                        _.First(p => 
                            p.Current.ProviderId == providerId).Current.Errors.Any(_ => 
                                _.Type == PublishedProviderErrorType.NoApplicableProfilingUpdateVariation && 
                                _.SummaryErrorMessage == $"Post Profiling and Variations - No applicable variation strategy executed for profiling update from £9.0 to £7 against funding line {_.FundingLineCode}."
                            )
                    ),
                    It.IsAny<Reference>(),
                    PublishedProviderStatus.Updated,
                    JobId,
                    CorrelationId,
                    It.IsAny<bool>()), Times.Once);

            _publishedProviderIndexerService
                .Verify(_ => _.IndexPublishedProviders(
                    It.Is<IEnumerable<PublishedProviderVersion>>(_ => _.First(pv => pv.ProviderId == providerId).Errors.Any(_ => _.Type == PublishedProviderErrorType.NoApplicableProfilingUpdateVariation))), Times.Once);
        }


        [TestMethod]
        public async Task RefreshResults_WhenPublishedProviderVariesButNoApplicableVariationDetected_ErrorLogged()
        {
            string providerId = string.Empty;
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders((_) =>
            {
                _.Last().Status = "Proposed to open";
                providerId = _.Last().ProviderId;
            });
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndPublishedProviders();
            AndNewMissingPublishedProviders();
            AndProfilePatternsForFundingStreamAndFundingPeriod();
            GivenFundingConfiguration(new ClosureWithSuccessorVariationStrategy(_providerService.Object));
            AndFundingConfiguration("NoApplicableVariationErrorDetector");
            AndFundingConfigurationIndicativeStatuses("Proposed to open", "Pending approval");
            AndTheFundingPeriod();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _publishedProviderStatusUpdateService
                .Verify(_ => _.UpdatePublishedProviderStatus(It.Is<IEnumerable<PublishedProvider>>(_ => _.Count() == 1
                        && _.Single().Current.ProviderId == providerId
                        && _.Single().Current.Errors.Any(_ => _.Type.Equals(PublishedProviderErrorType.NoApplicableVariation))),
                    It.IsAny<Reference>(),
                    PublishedProviderStatus.Updated,
                    JobId,
                    CorrelationId,
                    It.IsAny<bool>()), Times.Once);

            _publishedProviderIndexerService
                .Verify(_ => _.IndexPublishedProviders(
                    It.Is<IEnumerable<PublishedProviderVersion>>(_ => _.Single().ProviderId == providerId)), Times.Once);
        }

        [TestMethod]
        public async Task RefreshResults_WhenAnUnReleasedProviderOutOfScope_PublishedProviderDeleted()
        {
            string outOfScopeProviderId = NewRandomString();

            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders();
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndProfilePatternsForFundingStreamAndFundingPeriod();

            List<PublishedProvider> publishedProviders = _publishedProviders.ToList();
            publishedProviders.Add(NewPublishedProvider(pp => pp.WithCurrent(
                        NewPublishedProviderVersion(ppv => ppv
                            .WithProviderId(outOfScopeProviderId)
                            .WithPublishedProviderStatus(PublishedProviderStatus.Draft)
                            .WithFundingStreamId(FundingStreamId)))));
            AndPublishedProviders(publishedProviders);
            AndNewMissingPublishedProviders();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _publishedFundingDataService
                .Verify(_ => _.DeletePublishedProviders(
                    It.Is<IEnumerable<PublishedProvider>>(_ => _.Single().Current.ProviderId == outOfScopeProviderId)), Times.Once);

            _publishedProviderIndexerService
                .Verify(_ => _.Remove(
                    It.Is<IEnumerable<PublishedProviderVersion>>(_ => _.Single().ProviderId == outOfScopeProviderId)), Times.Once);
        }

        [TestMethod]
        public async Task RefreshResults_WhenAReleasedProviderOutOfScope_PublishedProviderDeleted()
        {
            string outOfScopeProviderId = NewRandomString();

            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders();
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndFundingConfiguration(nameof(PostPaymentOutOfScopeProviderErrorDetector));

            List<PublishedProvider> publishedProviders = _publishedProviders.ToList();
            publishedProviders.Add(NewPublishedProvider(pp => pp
                    .WithCurrent(
                        NewPublishedProviderVersion(ppv => ppv
                            .WithPublishedProviderStatus(PublishedProviderStatus.Updated)
                            .WithProviderId(outOfScopeProviderId)
                            .WithProvider(NewProvider(_ => _.WithName("Out of scope")))
                            .WithFundingStreamId(FundingStreamId)))
                    .WithReleased(
                        NewPublishedProviderVersion(ppv => ppv
                            .WithPublishedProviderStatus(PublishedProviderStatus.Updated)
                            .WithProviderId(outOfScopeProviderId)
                            .WithProvider(NewProvider(_ => _.WithName("Out of scope")))
                            .WithFundingStreamId(FundingStreamId)))));
            AndPublishedProviders(publishedProviders);
            AndNewMissingPublishedProviders();
            AndProfilePatternsForFundingStreamAndFundingPeriod();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _publishedProviderStatusUpdateService
                .Verify(_ => _.UpdatePublishedProviderStatus(It.Is<IEnumerable<PublishedProvider>>(_ => _.Count() == 1
                        && _.Single().Current.ProviderId == outOfScopeProviderId
                        && _.Single().Current.Errors.Single().Type.Equals(PublishedProviderErrorType.PostPaymentOutOfScopeProvider)),
                    It.IsAny<Reference>(),
                    PublishedProviderStatus.Updated,
                    JobId,
                    CorrelationId,
                    It.IsAny<bool>()), Times.Once);

            _publishedProviderIndexerService
                .Verify(_ => _.IndexPublishedProviders(
                    It.Is<IEnumerable<PublishedProviderVersion>>(_ => _.Single().ProviderId == outOfScopeProviderId)), Times.Once);
        }

        [TestMethod]
        [DataRow(true, true)]
        [DataRow(false, true)]
        [DataRow(false, false)]
        [DataRow(false, false)]
        public async Task RefreshResults_WhenAnUpdatePublishStatusCompletesWithoutErrorAndVariationsEnabled_PublishedProviderUpdatedAndVariationReasonSet(bool successorAlreadyExists, bool withSuccessorVariation)
        {
            PublishedProvider successor = NewPublishedProvider(_ => _.WithCurrent(NewPublishedProviderVersion(ppv => ppv.WithProvider(NewProvider()))));
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders((_) =>
            {
                _.Last().Name = "New name";
                _.Last().Status = "Closed";
                _.Last().Successor = successor.Current.ProviderId;
                if (withSuccessorVariation)
                {
                    _.First().Name = "Successor name";
                }
            }, scopedProviders: new[] { successor.Current.Provider });

            if (successorAlreadyExists)
            {
                AndScopedProviderCalculationResults();
            }
            else
            {
                // if successor doesn't exist in calc results then it has been added via refresh
                AndScopedProviderCalculationResults(successor.Current.Provider);
            }

            AndTemplateMapping();
            AndPublishedProviders();
            AndNewMissingPublishedProviders();
            AndProfilePatternsForFundingStreamAndFundingPeriod();
            AndTheFundingPeriod();

            GivenFundingConfiguration(new ProviderMetadataVariationStrategy(), new ClosureWithSuccessorVariationStrategy(_providerService.Object));

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _publishedProviderStatusUpdateService
                .Verify(_ => _.UpdatePublishedProviderStatus(It.Is<IEnumerable<PublishedProvider>>(_ => _.Where(pp => pp.Current.ProviderId == _publishedProviders.Last().Current.ProviderId && pp.Current.VariationReasons.Contains(VariationReason.NameFieldUpdated)).Count() == 1),
                    It.IsAny<Reference>(),
                    PublishedProviderStatus.Updated,
                    JobId,
                    CorrelationId,
                    It.IsAny<bool>()), Times.Once);

            _publishedProviderStatusUpdateService
                .Verify(_ => _.UpdatePublishedProviderStatus(It.Is<IEnumerable<PublishedProvider>>(_ => _.Where(pp => pp.Current.ProviderId == successor.Current.ProviderId).Count() == 1),
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

            AndTheCsvGenerationJobsWereCreated(SpecificationId, FundingPeriodId);
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
            AndProfilePatternsForFundingStreamAndFundingPeriod();

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
            AndProfilePatternsForFundingStreamAndFundingPeriod();

            Func<Task> invocation = WhenMessageReceivedWithJobIdAndCorrelationId;

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be($"calculationMappingResult returned null for funding stream {FundingStreamId}");
        }

        [DataTestMethod]
        [DynamicData(nameof(GetRefreshFundingBlockingJobTypesData), DynamicDataSourceType.Method)]
        public void CheckPrerequisitesForSpecificationToBeRefreshed_WhenPreReqsValidationErrors_ThrowsException(string runningJobType)
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndPublishedProviders(Array.Empty<PublishedProvider>());
            AndJobsRunning(runningJobType);

            Func<Task> invocation = WhenMessageReceivedWithJobIdAndCorrelationId;

            invocation
                .Should()
                .Throw<Exception>()
                .And
                .Message
                .Should()
                .Be($"Refresh with specification id: '{SpecificationId}' has prerequisites which aren't complete.");

            string[] prereqValidationErrors = new string[] { $"{runningJobType} is still running", "Specification must have providers in scope." };

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
            AndProfilePatternsForFundingStreamAndFundingPeriod();

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
                _.Last().Name = "New name";
            }, includeTemplateContents: false);
            AndScopedProviderCalculationResults();
            AndProfilePatternsForFundingStreamAndFundingPeriod();

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
            AndProfilePatternsForFundingStreamAndFundingPeriod();

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
            AndProfilePatternsForFundingStreamAndFundingPeriod();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _batchProfilingService.Verify(_ => _.ProfileBatches(
                    It.Is<BatchProfilingContext>(context =>
                        context.ProfilingRequests != null &&
                        context.ProfilingRequests.Count == 3 &&
                        context.ProfilingRequests.All(request => request.ProfilePatternKeys != null &&
                                                                 request.ProfilePatternKeys.Values.All(key =>
                                                                     key.StartsWith(profilePatternKey))))),
                Times.Once);
        }

        [TestMethod]
        public async Task RefreshResults_WhenMissingPublishedProvidersFound_ShouldUpdateNewProvidersOnlyOnce()
        {
            string profilePatternKey = NewRandomString();
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId();
            AndTemplateMetadataContents();
            AndScopedProviders(profilePatternKeyPrefix: profilePatternKey);

            var newProvider = _publishedProviders.First();
            _publishedProviders.Skip(1).First().Current.FundingLines.ForEach(_ => _.Value = null);
            _publishedProviders.Skip(1).First().Released = null;
            var existingProviders = _publishedProviders.Skip(1).ToList();

            AndPublishedProviders(existingProviders);
            AndNewMissingPublishedProviders(new[] { newProvider });
            AndScopedProviderCalculationResultsWithModifiedCalculationResults();
            AndTemplateMapping();
            AndProfilePatternsForFundingStreamAndFundingPeriod();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _batchProfilingService.Verify(_ => _.ProfileBatches(
                    It.Is<BatchProfilingContext>(context =>
                        context.ProfilingRequests != null &&
                        context.ProfilingRequests.Count == 3 &&
                        context.ProfilingRequests.First().NewInScopeFundingLines.Count() == 1 &&
                        context.ProfilingRequests.SkipLast(1).All(request => request.ProfilePatternKeys != null &&
                                                                 request.ProfilePatternKeys.Values.All(key =>
                                                                     key.StartsWith(profilePatternKey))))),
                Times.Once);

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
        public async Task RefreshResults_GivenPublishedProviderExcluded_HasPreviousFundingCalled()
        {
            GivenJobCanBeProcessed();
            AndSpecification();
            AndCalculationResultsBySpecificationId(new decimal?[] { null, null, null });
            AndTemplateMetadataContents();
            AndScopedProviders();
            AndScopedProviderCalculationResults();
            AndTemplateMapping();
            AndPublishedProviders();
            AndNewMissingPublishedProviders();
            AndProfilePatternsForFundingStreamAndFundingPeriod();

            await WhenMessageReceivedWithJobIdAndCorrelationId();

            _fundingLineValueOverride
                .Verify(_ =>
                _.HasPreviousFunding(It.IsAny<GeneratedProviderResult>(), It.IsAny<PublishedProviderVersion>()), Times.Exactly(3));
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

        private void AndScopedProviderCalculationResults(params Provider[] skipProviders)
        {
            _providerCalculationResults = _scopedProviders.Where(_ => !skipProviders.Any(sp => sp.ProviderId == _.ProviderId)).Select(_ =>
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
            _templateMetadataContents = NewTemplateMetadataContents(_ => _.WithFundingLines(_fundingLines).WithSchemaVersion("1.2"));

            _policiesService
                .Setup(_ => _.GetTemplateMetadataContents(FundingStreamId, _specificationSummary.FundingPeriod.Id, _specificationSummary.TemplateIds[FundingStreamId]))
                .ReturnsAsync(_templateMetadataContents);
        }

        private void AndTheCsvGenerationJobsWereCreated(string specificationId, string fundingPeriodId)
        {
            _publishFundingCsvJobsService.Verify(_ =>
                _.GenerateCsvJobs(GeneratePublishingCsvJobsCreationAction.Refresh,
                        specificationId,
                        fundingPeriodId,
                        It.IsAny<IEnumerable<string>>(),
                        It.IsAny<string>(),
                        It.IsAny<Reference>(),
                        true,
                        null),
                        Times.Once);
        }

        private void GivenFundingConfiguration(params IVariationStrategy[] variations)
        {
            _fundingConfiguration ??= new FundingConfiguration();
            _fundingConfiguration.Variations = variations.Select(_ => new FundingVariation { Name = _.Name });
            _policiesService
                .Setup(_ => _.GetFundingConfiguration(FundingStreamId, _specificationSummary.FundingPeriod.Id))
                .ReturnsAsync(_fundingConfiguration);
        }

        private void AndFundingConfiguration(params string[] errorDetectors)
        {
            _fundingConfiguration ??= new FundingConfiguration();
            _fundingConfiguration.ErrorDetectors = errorDetectors;
            _policiesService
                .Setup(_ => _.GetFundingConfiguration(FundingStreamId, _specificationSummary.FundingPeriod.Id))
                .ReturnsAsync(_fundingConfiguration);
        }

        private void AndFundingConfigurationIndicativeStatuses(params string[] indicativeStatuses)
        {
            _fundingConfiguration ??= new FundingConfiguration();
            _fundingConfiguration.IndicativeOpenerProviderStatus = indicativeStatuses;
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

        private void AndProfilingThrowsExeption()
        {
            _batchProfilingService.Setup(_ => _.ProfileBatches(It.IsAny<BatchProfilingContext>()))
                .Throws(new Exception());
        }

        private void AndCalculationResultsBySpecificationId(decimal?[] resultsIn = null)
        {
            decimal?[] results = resultsIn ?? new decimal?[] { 2M, 3M, 4M };

            _calculationResults = results.Select(res => NewCalculationResult(cr => cr.WithValue(res))).ToArray();
        }

        private void GivenCalculationResultsBySpecificationIdThrowsException()
        {
            _calculationResultsService.Setup(_ => _.GetCalculationResultsBySpecificationId(_specificationSummary.Id,
                It.Is<IEnumerable<string>>(_ => _scopedProviders.All(sp => _.Any(arg => arg == sp.ProviderId)))))
                .Throws(new Exception());
        }

        private void AndScopedProviders(
            Action<Provider[]> variationAction = null,
            string profilePatternKeyPrefix = null,
            bool includeTemplateContents = true,
            PublishedProviderStatus publishedProviderStatus = PublishedProviderStatus.Updated,
            Provider[] scopedProviders = null)
        {
            _scopedProviders = new[] { NewProvider(), NewProvider(), NewProvider() };

            if (scopedProviders != null)
            {
                _scopedProviders = scopedProviders
                    .Concat(_scopedProviders);
            }

            _publishedProviders = _scopedProviders.DeepCopy().Select(_ =>
                    NewPublishedProvider(pp => pp.WithCurrent(
                        NewPublishedProviderVersion(ppv => ppv.WithProvider(_.DeepCopy())
                            .WithFundingStreamId(FundingStreamId)
                            .WithFundingPeriodId(_specificationSummary.FundingPeriod.Id)
                            .WithProviderId(_.ProviderId)
                            .WithTotalFunding(9)
                            .WithPublishedProviderStatus(publishedProviderStatus)
                            .WithProfilePatternKeys(includeTemplateContents ? new ProfilePatternKey() { FundingLineCode = _fundingLines[0].FundingLineCode, Key = $"{profilePatternKeyPrefix}-{NewRandomString()}" } : null)
                            .WithFundingLines(includeTemplateContents ? new[] { new FundingLine { FundingLineCode = _fundingLines[0].FundingLineCode, TemplateLineId = _fundingLines[0].TemplateLineId, Value = 9 } } : null)
                            .WithFundingCalculations(includeTemplateContents ? new[] {new FundingCalculation { Value = _calculationResults[0].Value, TemplateCalculationId = _calculationTemplateIds[0].TemplateCalculationId },
                                                            new FundingCalculation { Value = _calculationResults[1].Value, TemplateCalculationId = _calculationTemplateIds[1].TemplateCalculationId },
                                                            new FundingCalculation { Value = _calculationResults[2].Value, TemplateCalculationId = _calculationTemplateIds[2].TemplateCalculationId } } : null)))
                        .WithReleased(NewPublishedProviderVersion(ppv => ppv.WithProvider(_.DeepCopy())
                                .WithFundingStreamId(FundingStreamId)
                                .WithProviderId(_.ProviderId)
                                .WithTotalFunding(9)
                                .WithFundingPeriodId(_specificationSummary.FundingPeriod.Id)
                                .WithFundingLines(includeTemplateContents ? new[] { new FundingLine { FundingLineCode = _fundingLines[0].FundingLineCode, TemplateLineId = _fundingLines[0].TemplateLineId, Value = 9 } } : null)
                                .WithFundingCalculations(includeTemplateContents ? new[] {new FundingCalculation { Value = _calculationResults[0].Value, TemplateCalculationId = _calculationTemplateIds[0].TemplateCalculationId },
                                    new FundingCalculation { Value = _calculationResults[1].Value, TemplateCalculationId = _calculationTemplateIds[1].TemplateCalculationId },
                                    new FundingCalculation { Value = _calculationResults[2].Value, TemplateCalculationId = _calculationTemplateIds[2].TemplateCalculationId } } : null))))).ToList();

            variationAction?.Invoke(_scopedProviders.ToArray());

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
                .ReturnsAsync((publishedProviders ?? new List<PublishedProvider>()).ToDictionary(x => x.Current.ProviderId));
        }

        public static IEnumerable<object[]> GetRefreshFundingBlockingJobTypesData()
        {
            foreach (string jobType in GetRefreshFundingBlockingJobTypes())
            {
                yield return new object[] { jobType };
            }
        }

        private static string[] GetRefreshFundingBlockingJobTypes()
        {
            return new string[]
            {
                JobConstants.DefinitionNames.ProviderSnapshotDataLoadJob,
                JobConstants.DefinitionNames.PublishedFundingUndoJob,
                JobConstants.DefinitionNames.CreateInstructAllocationJob,
                JobConstants.DefinitionNames.ApproveAllProviderFundingJob,
                JobConstants.DefinitionNames.ApproveBatchProviderFundingJob,
                JobConstants.DefinitionNames.PublishAllProviderFundingJob,
                JobConstants.DefinitionNames.PublishBatchProviderFundingJob,
                JobConstants.DefinitionNames.ReleaseProvidersToChannelsJob,
                JobConstants.DefinitionNames.ReIndexPublishedProvidersJob,
                JobConstants.DefinitionNames.GenerateGraphAndInstructAllocationJob,
                JobConstants.DefinitionNames.GenerateGraphAndInstructGenerateAggregationAllocationJob
            };
        }

        private void AndJobsRunning(string runningJobType)
        {
            string[] jobTypes = GetRefreshFundingBlockingJobTypes();

            _jobsRunning
                .Setup(_ => _.GetJobTypes(SpecificationId, It.Is<IEnumerable<string>>(_ => _.All(jt => jobTypes.Contains(jt)))))
                .ReturnsAsync(new[] { runningJobType });
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

        private void AndTheFundingPeriod()
        {
            _policiesService
                .Setup(_ => _.GetFundingPeriodByConfigurationId(_specificationSummary.FundingPeriod.Id))
                .ReturnsAsync(new FundingPeriod());
        }

        private void AndClearForceUpdateOnRefresh()
        {
            _specificationsApiClient
                .Setup(_ => _.ClearForceOnNextRefresh(_specificationSummary.Id))
                .ReturnsAsync(HttpStatusCode.OK);
        }

        private void AndClearForceUpdateOnRefreshCalled()
        {
            _specificationsApiClient
                .Verify(_ => _.ClearForceOnNextRefresh(_specificationSummary.Id), Times.Once);
        }

        private void AndProfilePatternsForFundingStreamAndFundingPeriod()
        {
            IEnumerable<Common.ApiClient.Profiling.Models.FundingStreamPeriodProfilePattern> fundingStreamPeriodProfilePatterns
                = new List<Common.ApiClient.Profiling.Models.FundingStreamPeriodProfilePattern>
                {
                    new Common.ApiClient.Profiling.Models.FundingStreamPeriodProfilePattern
                    {
                        FundingLineId = NewRandomString()
                    }
                };

            foreach (Reference fundingStream in _specificationSummary.FundingStreams)
            {
                _profilingService
                    .Setup(_ => _.GetProfilePatternsForFundingStreamAndFundingPeriod(fundingStream.Id, _specificationSummary.FundingPeriod.Id))
                    .ReturnsAsync(fundingStreamPeriodProfilePatterns);
            }
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

        private void AndTheVariationPointers()
        {
            _profileVariationPointers = _fundingLines.Select(_ => new ProfileVariationPointer { FundingLineId = _.FundingLineCode }); _specificationsApiClient.Setup(_ =>

            _.GetProfileVariationPointers(It.IsAny<string>()))
            .ReturnsAsync(new ApiResponse<IEnumerable<ProfileVariationPointer>>(HttpStatusCode.OK, _profileVariationPointers));
        }

        private void AndSpecification(bool? forceUpdateOnNextRefresh = null)
        {
            _specificationSummary = NewSpecificationSummary(_ => _
                .WithId(SpecificationId)
                .WithPublishStatus(PublishStatus.Approved)
                .WithFundingStreamIds(new[] { FundingStreamId })
                .WithFundingPeriodId(FundingPeriodId)
                .WithTemplateIds((FundingStreamId, "1.0"))
                .WithForceUpdateOnNextRefresh(forceUpdateOnNextRefresh.GetValueOrDefault())
                .WithProviderVersionId(providerVersionId));

            _specificationsApiClient.Setup(_ => _.GetSpecificationSummaryById(SpecificationId))
                .ReturnsAsync(new ApiResponse<SpecificationSummary>(HttpStatusCode.OK, _specificationSummary));

            _specificationsApiClient.Setup(_ =>
                _.GetProfileVariationPointers(It.IsAny<string>()))
                .ReturnsAsync(new ApiResponse<IEnumerable<ProfileVariationPointer>>(HttpStatusCode.OK, _profileVariationPointers));
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
                _reApplyCustomProfiles.Verify(_ => _.ProcessPublishedProvider(publishedProvider.Current,
                    It.Is<GeneratedProviderResult>(_ => _.FundingLines.All(fl => publishedProvider.Current.FundingLines.Any(cfl => cfl.FundingLineCode == fl.FundingLineCode)))),
                    Times.Once);
            }
        }
    }
}
