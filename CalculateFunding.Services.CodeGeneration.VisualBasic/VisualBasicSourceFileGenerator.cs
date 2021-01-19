using System.Collections.Generic;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Publishing;
using Serilog;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class VisualBasicSourceFileGenerator : RoslynSourceFileGenerator
    {
        private readonly IFundingLineRoundingSettings _fundingLineRoundingSettings;
        
        public VisualBasicSourceFileGenerator(ILogger logger,
            IFundingLineRoundingSettings fundingLineRoundingSettings) : base(logger)
        {
            Guard.ArgumentNotNull(fundingLineRoundingSettings, nameof(fundingLineRoundingSettings));
            
            _fundingLineRoundingSettings = fundingLineRoundingSettings;
        }

        protected override IEnumerable<SourceFile> GenerateCalculationSourceFiles(BuildProject buildProject, IEnumerable<Calculation> calculations, CompilerOptions compilerOptions)
        {
            CalculationTypeGenerator calculationTypeGenerator = new CalculationTypeGenerator(compilerOptions, _fundingLineRoundingSettings);
            return calculationTypeGenerator.GenerateCalcs(calculations, buildProject.FundingLines);
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