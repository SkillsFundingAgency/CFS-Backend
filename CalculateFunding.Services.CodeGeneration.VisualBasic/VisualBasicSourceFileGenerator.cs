using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using Serilog;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class VisualBasicSourceFileGenerator : RoslynSourceFileGenerator
    {
        public VisualBasicSourceFileGenerator(ILogger logger) : base(logger)
        {
        }

        protected override IEnumerable<SourceFile> GenerateCalculationSourceFiles(BuildProject buildProject, IEnumerable<Calculation> calculations, CompilerOptions compilerOptions)
        {
            CalculationTypeGenerator calculationTypeGenerator = new CalculationTypeGenerator(compilerOptions);
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