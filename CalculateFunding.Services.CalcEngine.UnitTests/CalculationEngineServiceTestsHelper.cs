using AutoMapper;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.ServiceBus.Interfaces;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Options;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.CalcEngine.UnitTests
{
    public class CalculationEngineServiceTestsHelper
    {
        public CalculationEngineServiceTestsHelper()
        {
            MockCalculatorResiliencePolicies.CacheProvider.Returns(MockCacheProviderPolicy);
            MockCalculatorResiliencePolicies.Messenger.Returns(MockMessengerPolicy);
            MockCalculatorResiliencePolicies.ProviderSourceDatasetsRepository.Returns(MockProviderSourceDatasetsRepositoryPolicy);
            MockCalculatorResiliencePolicies.CalculationResultsRepository.Returns(MockProviderResultsRepositoryPolicy);
            MockCalculatorResiliencePolicies.CalculationsApiClient.Returns(MockCalculationRepositoryPolicy);
            MockCalculatorResiliencePolicies.PoliciesApiClient.Returns(MockPoliciesApiClientPolicy);
            MockCalculatorResiliencePolicies.ResultsApiClient.Returns(MockResultsApiClientPolicy);
            MockCalculatorResiliencePoliciesValidator
                .Validate(Arg.Any<ICalculatorResiliencePolicies>())
                .Returns(new ValidationResult());
            MockCalculatorResiliencePolicies.JobsApiClient.Returns(MockJobsApiClientPolicy);
            MockCalculatorResiliencePolicies.SpecificationsApiClient.Returns(MockSpecificationsApiClientPolicy);
        }

        public CalculationEngineService CreateCalculationEngineService()
        {
            CalculationEngineService service =
                new CalculationEngineService(
                    MockLogger,
                    MockCalculationEngine,
                    MockCacheProvider,
                    MockMessengerService,
                    MockDatasetRepo,
                    MockTelemetry,
                    MockProviderResultRepo,
                    MockCalculationRepository,
                    MockEngineSettings,
                    MockCalculatorResiliencePolicies,
                    MockJobManagement,
                    MockSpecificationsApiClient,
                    MockResultsApiClient,
                    MockCalculatorResiliencePoliciesValidator,
                    MockCalculationEngineServiceValidator,
                    MockCalculationAggregationService,
                    MockAssemblyService
                    );

            return service;
        }

        public ILogger MockLogger { get; set; } = Substitute.For<ILogger>();
        public ICacheProvider MockCacheProvider { get; set; } = Substitute.For<ICacheProvider>();
        public IMessengerService MockMessengerService { get; set; } = Substitute.For<IMessengerService>();
        public IProviderSourceDatasetsRepository MockDatasetRepo { get; set; } = Substitute.For<IProviderSourceDatasetsRepository>();
        public ITelemetry MockTelemetry { get; set; } = Substitute.For<ITelemetry>();
        public IProviderResultsRepository MockProviderResultRepo { get; set; } = Substitute.For<IProviderResultsRepository>();
        public ICalculationsRepository MockCalculationRepository { get; set; } = Substitute.For<ICalculationsRepository>();
        public EngineSettings MockEngineSettings { get; set; } = Substitute.For<EngineSettings>();
        public ICalculatorResiliencePolicies MockCalculatorResiliencePolicies { get; set; } = Substitute.For<ICalculatorResiliencePolicies>();
        public ICalculationEngine MockCalculationEngine { get; set; } = Substitute.For<ICalculationEngine>();
        public IValidator<ICalculatorResiliencePolicies> MockCalculatorResiliencePoliciesValidator { get; set; } = Substitute.For<IValidator<ICalculatorResiliencePolicies>>();
        public ICalculationEngineServiceValidator MockCalculationEngineServiceValidator { get; set; } = Substitute.For<ICalculationEngineServiceValidator>();
        public IFeatureToggle FeatureToggle { get; set; } = Substitute.For<IFeatureToggle>();
        public IJobManagement MockJobManagement { get; set; } = Substitute.For<IJobManagement>();
        public ISpecificationsApiClient MockSpecificationsApiClient { get; set; } = Substitute.For<ISpecificationsApiClient>();
        public IPoliciesApiClient MockPoliciesApiClient { get; set; } = Substitute.For<IPoliciesApiClient>();
        public IResultsApiClient MockResultsApiClient { get; set; } = Substitute.For<IResultsApiClient>();
        public ICalculationAggregationService MockCalculationAggregationService { get; set; } = Substitute.For<ICalculationAggregationService>();
        public IAssemblyService MockAssemblyService { get; set; } = Substitute.For<IAssemblyService>();
        public AsyncPolicy MockCacheProviderPolicy { get; set; } = Policy.NoOpAsync();
        public AsyncPolicy MockMessengerPolicy { get; set; } = Policy.NoOpAsync();
        public AsyncPolicy MockProviderSourceDatasetsRepositoryPolicy { get; set; } = Policy.NoOpAsync();
        public AsyncPolicy MockProviderResultsRepositoryPolicy { get; set; } = Policy.NoOpAsync();
        public AsyncPolicy MockCalculationRepositoryPolicy { get; set; } = Policy.NoOpAsync();
        public AsyncPolicy MockPoliciesApiClientPolicy { get; set; } = Policy.NoOpAsync();
        public AsyncPolicy MockResultsApiClientPolicy { get; set; } = Policy.NoOpAsync();
        public AsyncPolicy MockJobsApiClientPolicy { get; set; } = Policy.NoOpAsync();
        public AsyncPolicy MockSpecificationsApiClientPolicy { get; set; } = Policy.NoOpAsync();
    }
}