using System.IO;
using System.Linq;
using CalculateFunding.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Services.Compiler.VisualBasic
{
    public class VisualBasicCompiler : RoslynCompiler
    {
        public VisualBasicCompiler(ILogger<VisualBasicCompiler> logger) : base(logger)
        {         
        }

        protected override EmitResult GenerateCode(MetadataReference[] references, MemoryStream ms, SyntaxTree datasetSyntaxTree,
            SyntaxTree calcSyntaxTree)
        {
            var options = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);


            var compilation = VisualBasicCompilation.Create("budget")
                .WithOptions(options)
                .AddSyntaxTrees(datasetSyntaxTree)
                .AddSyntaxTrees(calcSyntaxTree)
                .AddReferences(references);


            return compilation.Emit(ms);
        }

        protected override SyntaxTree GenerateProductSyntaxTree(Budget budget)
        {
            var productTypeGenerator = new ProductTypeGenerator();
            return productTypeGenerator.GenerateCalcs(budget).SyntaxTree;
        }

        protected override SyntaxTree GenerateDatasetSyntaxTree(Budget budget)
        {
            var datasetTypeGenerator = new DatasetTypeGenerator();
            return datasetTypeGenerator.GenerateDatasets(budget).SyntaxTree;
        }

        public override string GetIdentifier(string name)
        {
            return VisualBasicTypeGenerator.Identifier(name);
        }
    }
}