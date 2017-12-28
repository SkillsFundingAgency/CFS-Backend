using System.Diagnostics;
using System.IO;
using System.Linq;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Services.Compiler.CSharp
{
    public class CSharpCompiler : RoslynCompiler
    {
        private readonly ILogger<CSharpCompiler> _logger;

        public CSharpCompiler(ILogger<CSharpCompiler> logger) : base(logger)
        {
        }

        protected override SyntaxTree GenerateProductSyntaxTree(Implementation implementation)
        {
            var productTypeGenerator = new ProductTypeGenerator();
            var calcSyntaxTree = productTypeGenerator.GenerateCalcs(implementation).SyntaxTree;
            return calcSyntaxTree;
        }

        protected override SyntaxTree GenerateDatasetSyntaxTree(Implementation implementation)
        {
            var datasetTypeGenerator = new DatasetTypeGenerator();
            return datasetTypeGenerator.GenerateDataset(implementation).SyntaxTree;
        }

        protected override EmitResult GenerateCode(MetadataReference[] references, MemoryStream ms, SyntaxTree datasetSyntaxTree,
            SyntaxTree calcSyntaxTree)
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var compilation = CSharpCompilation.Create("budget")
                .WithOptions(options)
                .AddSyntaxTrees(datasetSyntaxTree)
                .AddSyntaxTrees(calcSyntaxTree)
                .AddReferences(references);


            var result = compilation.Emit(ms);
            return result;
        }

        public override string GetIdentifier(string name)
        {
            return CSharpTypeGenerator.Identifier(name);
        }
    }
}