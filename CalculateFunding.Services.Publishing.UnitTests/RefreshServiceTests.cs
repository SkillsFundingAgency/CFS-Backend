using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using Serilog;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    [TestClass]
    public class RefreshServiceTests
    {
        //add some tests maybe??
        
        private ISpecificationService _specificationService;
        private IProviderService _providerService;
        private IRefreshService _refreshService;

        private ICalculationResultsService _calculationResultsService;
        private IPublishedProviderDataGenerator _fundingLineGenerator;
        private IPublishedProviderContentsGeneratorResolver _publishedProviderContentsGeneratorResolver;
        private IProfilingService _profilingService;
        private IInScopePublishedProviderService _inScopePublishedProviderService;
        private IPublishedProviderDataPopulator _publishedProviderDataPopulator;
        private ILogger _logger;
        private IPublishedProviderContentPersistanceService _publishedProviderContentPersistanceService;
        private ICalculationsApiClient _calculationsApiClient;
        private IRefreshPrerequisiteChecker _refreshPrerequisiteChecker;
        private IPoliciesApiClient _policiesApiClient;
        private IPublishProviderExclusionCheck _publishProviderExclusionCheck;
        private IFundingLineValueOverride _fundingLineValueOverride;
        private IJobManagement _jobManagement;
        private IDetectProviderVariations _detectProviderVariations;
        private IPublishingFeatureFlag _publishingFeatureFlag;
        private IPublishedProviderIndexerService _publishedProviderIndexerService;
        private IApplyProviderVariations _applyProviderVariations;

        [TestInitialize]
        public void SetUp()
        {
            _specificationService = Substitute.For<ISpecificationService>();
            _providerService = Substitute.For<IProviderService>();
            _calculationResultsService = Substitute.For<ICalculationResultsService>();
            _fundingLineGenerator = Substitute.For<IPublishedProviderDataGenerator>();
            _publishedProviderContentsGeneratorResolver = Substitute.For<IPublishedProviderContentsGeneratorResolver>();
            _inScopePublishedProviderService = Substitute.For<IInScopePublishedProviderService>();
            _publishedProviderDataPopulator = Substitute.For<IPublishedProviderDataPopulator>();
            _calculationsApiClient = Substitute.For<ICalculationsApiClient>();
            _publishProviderExclusionCheck = Substitute.For<IPublishProviderExclusionCheck>();
            _fundingLineValueOverride = Substitute.For<IFundingLineValueOverride>();

            _profilingService = Substitute.For<IProfilingService>();
            _logger = Substitute.For<ILogger>();
            _publishedProviderContentPersistanceService = Substitute.For<IPublishedProviderContentPersistanceService>();
            _refreshPrerequisiteChecker = Substitute.For<IRefreshPrerequisiteChecker>();
            _policiesApiClient = Substitute.For<IPoliciesApiClient>();
            _jobManagement = Substitute.For<IJobManagement>();
            _publishingFeatureFlag = Substitute.For<IPublishingFeatureFlag>();
            _publishedProviderIndexerService = Substitute.For<IPublishedProviderIndexerService>();
            _detectProviderVariations = Substitute.For<IDetectProviderVariations>();
            _applyProviderVariations = Substitute.For<IApplyProviderVariations>();

            _refreshService = new RefreshService(Substitute.For<IPublishedProviderStatusUpdateService>(),
                Substitute.For<IPublishedFundingDataService>(),
                Substitute.For<IPublishingResiliencePolicies>(),
                _specificationService,
                _providerService,
                _calculationResultsService,
                _fundingLineGenerator,
                _profilingService,
                _inScopePublishedProviderService,
                _publishedProviderDataPopulator,
                _logger,
                _calculationsApiClient,
                _policiesApiClient,
                _refreshPrerequisiteChecker,
                _publishProviderExclusionCheck,
                _fundingLineValueOverride,
                _jobManagement,
                _publishingFeatureFlag,
                _publishedProviderIndexerService,
                _detectProviderVariations,
                _applyProviderVariations);
        }
    }
}