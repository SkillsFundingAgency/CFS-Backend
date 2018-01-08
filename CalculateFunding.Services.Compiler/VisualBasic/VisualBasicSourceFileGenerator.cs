using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Services.Compiler.VisualBasic
{
    public class VisualBasicSourceFileGenerator : RoslynSourceFileGenerator
    {

        public VisualBasicSourceFileGenerator(ILogger<VisualBasicSourceFileGenerator> logger) : base(logger)
        {
        }

        protected override IEnumerable<SourceFile> GenerateProductSourceFiles(Implementation budget)
        {
            var productTypeGenerator = new ProductTypeGenerator();
            return productTypeGenerator.GenerateCalcs(budget);
        }

        protected override IEnumerable<SourceFile> GenerateDatasetSourceFiles(Implementation budget)
        {
            var datasetTypeGenerator = new DatasetTypeGenerator();
            return datasetTypeGenerator.GenerateDatasets(budget);
        }

        protected override IEnumerable<SourceFile> GenerateStaticSourceFiles(Implementation budget)
        {
            return GenerateStaticSourceFiles(".vb", ".vbproj");
        }

        public override string GetIdentifier(string name)
        {
            return VisualBasicTypeGenerator.Identifier(name);
        }
    }
}