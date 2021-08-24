using System;
using System.Collections.Generic;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using Serilog;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class VisualBasicSourceFileGenerator : RoslynSourceFileGenerator
    {
        private readonly IFundingLineRoundingSettings _fundingLineRoundingSettings;
        private readonly ITypeIdentifierGenerator _typeIdentifierGenerator;

        public VisualBasicSourceFileGenerator(ILogger logger,
            IFundingLineRoundingSettings fundingLineRoundingSettings) : base(logger)
        {
            Guard.ArgumentNotNull(fundingLineRoundingSettings, nameof(fundingLineRoundingSettings));
            
            _fundingLineRoundingSettings = fundingLineRoundingSettings;

            _typeIdentifierGenerator = new VisualBasicTypeIdentifierGenerator();
        }

        protected override IEnumerable<SourceFile> GenerateCalculationSourceFiles(BuildProject buildProject,
            IEnumerable<Calculation> calculations,
            CompilerOptions compilerOptions,
            IEnumerable<ObsoleteItem> obsoleteItems)
        {
            CalculationTypeGenerator calculationTypeGenerator = new CalculationTypeGenerator(compilerOptions, _fundingLineRoundingSettings);
            return calculationTypeGenerator.GenerateCalcs(calculations, buildProject.FundingLines, obsoleteItems ?? ArraySegment<ObsoleteItem>.Empty);
        }

        protected override IEnumerable<SourceFile> GenerateDatasetSourceFiles(BuildProject buildProject,
            IEnumerable<ObsoleteItem> obsoleteItems)
        {
            DatasetTypeGenerator datasetTypeGenerator = new DatasetTypeGenerator();
            return datasetTypeGenerator.GenerateDatasetSourceFiles(buildProject, obsoleteItems);
        }

        public override string GetIdentifier(string name)
        {
            return _typeIdentifierGenerator.GenerateIdentifier(name);
        }
    }
}