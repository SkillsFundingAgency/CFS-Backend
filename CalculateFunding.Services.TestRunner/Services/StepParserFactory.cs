using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.StepParsers;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using Polly;

namespace CalculateFunding.Services.TestRunner.Services
{

    public class StepParserFactory : IStepParserFactory
    {
        private readonly ICodeMetadataGeneratorService _codeMetadataGeneratorService;
        private readonly IProviderResultsRepository _providerResultsRepository;
        private readonly ITestRunnerResiliencePolicies _resiliencePolicies;

        public StepParserFactory(
            ICodeMetadataGeneratorService codeMetadataGeneratorService,
            IProviderResultsRepository providerResultsRepository,
            ITestRunnerResiliencePolicies resiliencePolicies)
        {
            _codeMetadataGeneratorService = codeMetadataGeneratorService;
            _providerResultsRepository = providerResultsRepository;
            _resiliencePolicies = resiliencePolicies;
        }

        public IStepParser GetStepParser(StepType stepType)
        {
            switch (stepType)
            {
                case StepType.Datasets:
                    return new DatsetsStepParser(_codeMetadataGeneratorService);
                case StepType.Provider:
                    return new ProviderStepParser(_providerResultsRepository, _resiliencePolicies);
                case StepType.AssertCalcDataset:
                    return new AssertDatasetCalcStepParser(_codeMetadataGeneratorService);
                case StepType.AssertCalc:
                    return new AssertCalcStepParser(_codeMetadataGeneratorService);
                default:

                    return null;
            }
        }
    }
}
