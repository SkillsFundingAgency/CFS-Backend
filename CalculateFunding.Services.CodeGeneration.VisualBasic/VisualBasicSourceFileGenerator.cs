using System.Collections.Generic;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using Serilog;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class VisualBasicSourceFileGenerator : RoslynSourceFileGenerator
    {
        private readonly IFeatureToggle _featureToggle;

        public VisualBasicSourceFileGenerator(ILogger logger, IFeatureToggle featureToggle) : base(logger)
        {
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

            _featureToggle = featureToggle;
        }

        protected override IEnumerable<SourceFile> GenerateCalculationSourceFiles(BuildProject buildProject, IEnumerable<Calculation> calculations, CompilerOptions compilerOptions)
        {
            CalculationTypeGenerator calculationTypeGenerator = new CalculationTypeGenerator(compilerOptions, _featureToggle.IsDuplicateCalculationNameCheckEnabled());
            return calculationTypeGenerator.GenerateCalcs(calculations);
        }

        protected override IEnumerable<SourceFile> GenerateDatasetSourceFiles(BuildProject buildProject)
        {
            DatasetTypeGenerator datasetTypeGenerator = new DatasetTypeGenerator();
            return datasetTypeGenerator.GenerateDatasets(buildProject);
        }

        public override string GetIdentifier(string name)
        {
            return VisualBasicTypeGenerator.GenerateIdentifier(name);
        }
    }
}