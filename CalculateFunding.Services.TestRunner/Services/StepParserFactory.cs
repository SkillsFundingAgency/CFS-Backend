using CalculateFunding.Services.TestRunner.Interfaces;
using CalculateFunding.Services.TestRunner.StepParsers;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;

namespace CalculateFunding.Services.TestRunner.Services
{

    public class StepParserFactory : IStepParserFactory
    {
        private readonly ICodeMetadataGeneratorService _codeMetadataGeneratorService;
        private readonly IProviderResultsRepository _providerRepository;

        public StepParserFactory(
            ICodeMetadataGeneratorService codeMetadataGeneratorService,
            IProviderResultsRepository providerRepository)
        {
            _codeMetadataGeneratorService = codeMetadataGeneratorService;
            _providerRepository = providerRepository;
        }

        public IStepParser GetStepParser(StepType stepType)
        {
            switch (stepType)
            {
                case StepType.Datasets:
                    return new DatsetsStepParser(_codeMetadataGeneratorService);
                case StepType.Provider:
                    return new ProviderStepParser(_providerRepository);
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
