using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Models.Calcs;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Services.CodeGeneration.CSharp
{

    public class CSharpSourceFileGenerator : RoslynSourceFileGenerator
    {

        public CSharpSourceFileGenerator(ILogger<CSharpSourceFileGenerator> logger) : base(logger)
        {
        }

        protected override IEnumerable<SourceFile> GenerateProductSourceFiles(Implementation implementation)
        {
            var productTypeGenerator = new ProductTypeGenerator();
            var calcSyntaxTree = productTypeGenerator.GenerateCalcs(implementation);
            return calcSyntaxTree;
        }

        protected override IEnumerable<SourceFile> GenerateDatasetSourceFiles(Implementation implementation)
        {
            var datasetTypeGenerator = new DatasetTypeGenerator();
            return datasetTypeGenerator.GenerateDataset(implementation);
        }


        public override string GetIdentifier(string name)
        {
            return CSharpTypeGenerator.Identifier(name);
        }
    }
}