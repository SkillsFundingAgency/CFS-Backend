using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
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
            MockCalculatorResiliencePolicies.ProviderResultsRepository.Returns(MockProviderResultsRepositoryPolicy);
            MockCalculatorResiliencePolicies.CalculationsRepository.Returns(MockCalculationRepositoryPolicy);
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
                    MockCalculatorResiliencePoliciesValidator,
                    DatasetAggregationsRepository,
                    MockJobsApiClient,
                    MockSpecificationsApiClient
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
        public IDatasetAggregationsRepository DatasetAggregationsRepository { get; set; } = Substitute.For<IDatasetAggregationsRepository>();
        public IFeatureToggle FeatureToggle { get; set; } = Substitute.For<IFeatureToggle>();
        public IJobsApiClient MockJobsApiClient { get; set; } = Substitute.For<IJobsApiClient>();
        public ISpecificationsApiClient MockSpecificationsApiClient { get; set; } = Substitute.For<ISpecificationsApiClient>();
        public Policy MockCacheProviderPolicy { get; set; } = Policy.NoOpAsync();
        public Policy MockMessengerPolicy { get; set; } = Policy.NoOpAsync();
        public Policy MockProviderSourceDatasetsRepositoryPolicy { get; set; } = Policy.NoOpAsync();
        public Policy MockProviderResultsRepositoryPolicy { get; set; } = Policy.NoOpAsync();
        public Policy MockCalculationRepositoryPolicy { get; set; } = Policy.NoOpAsync();
        public Policy MockJobsApiClientPolicy { get; set; } = Policy.NoOpAsync();
        public Policy MockSpecificationsApiClientPolicy { get; set; } = Policy.NoOpAsync();
    }
}