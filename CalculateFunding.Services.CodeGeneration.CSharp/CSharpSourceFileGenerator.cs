using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Models.Calcs;
using Serilog;

namespace CalculateFunding.Services.CodeGeneration.CSharp
{

    public class CSharpSourceFileGenerator : RoslynSourceFileGenerator
    {

        public CSharpSourceFileGenerator(ILogger logger) : base(logger)
        {
        }

        protected override IEnumerable<SourceFile> GenerateProductSourceFiles(BuildProject buildProject)
        {
            var productTypeGenerator = new ProductTypeGenerator();
            var calcSyntaxTree = productTypeGenerator.GenerateCalcs(buildProject);
            return calcSyntaxTree;
        }

        protected override IEnumerable<SourceFile> GenerateDatasetSourceFiles(BuildProject buildProject)
        {
            var datasetTypeGenerator = new DatasetTypeGenerator();
            return datasetTypeGenerator.GenerateDataset(buildProject);
        }


        public override string GetIdentifier(string name)
        {
            return CSharpTypeGenerator.Identifier(name);
        }
    }
}