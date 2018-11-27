using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Services.CalcEngine.Interfaces;
using CalculateFunding.Services.Calculator.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Options;
using FluentValidation;
using FluentValidation.Results;
using NSubstitute;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Calculator
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
            MockCalculatorResiliencePolicies.JobsRepository.Returns(MockJobsRepositoryPolicy);
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
                    FeatureToggle,
                    MockJobsRepository);

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
        public IJobsRepository MockJobsRepository { get; set; } = Substitute.For<IJobsRepository>();
        public IFeatureToggle FeatureToggle { get; set; } = Substitute.For<IFeatureToggle>();
        public Policy MockCacheProviderPolicy { get; set; } = Policy.NoOpAsync();
        public Policy MockMessengerPolicy { get; set; } = Policy.NoOpAsync();
        public Policy MockProviderSourceDatasetsRepositoryPolicy { get; set; } = Policy.NoOpAsync();
        public Policy MockProviderResultsRepositoryPolicy { get; set; } = Policy.NoOpAsync();
        public Policy MockCalculationRepositoryPolicy { get; set; } = Policy.NoOpAsync();
        public Policy MockJobsRepositoryPolicy { get; set; } = Policy.NoOpAsync();
    }
}