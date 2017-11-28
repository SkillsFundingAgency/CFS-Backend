using System.IO;
using System.Linq;
using CalculateFunding.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Allocations.Services.Compiler.CSharp
{
    public class CSharpCompiler : BaseCompiler
    {
        protected override BudgetCompilerOutput Compile(Budget budget, MetadataReference[] references, MemoryStream ms)
        {
            var datasetTypeGenerator = new DatasetTypeGenerator();
            var productTypeGenerator = new ProductTypeGenerator();

            var datasetSyntaxTrees = budget.DatasetDefinitions.Select(x => datasetTypeGenerator.GenerateDataset(budget, x).SyntaxTree).ToArray();
            var calcSyntaxTree = productTypeGenerator.GenerateCalcs(budget).SyntaxTree;

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var compilation = CSharpCompilation.Create("budget")
                .WithOptions(options)
                .AddSyntaxTrees(datasetSyntaxTrees)
                .AddSyntaxTrees(calcSyntaxTree)
                .AddReferences(references);


            var compilerOutput = new BudgetCompilerOutput
            {
                Budget = budget,
      //          DatasetSourceCode = datasetSyntaxTrees.Select(x => x.ToString()).ToArray(),
                CalculationSourceCode = calcSyntaxTree.ToString()
            };

            var result = compilation.Emit(ms);
            compilerOutput.Success = result.Success;
            compilerOutput.CompilerMessages = result.Diagnostics.Select(x => new CompilerMessage { Message = x.GetMessage(), Severity = (Severity)x.Severity }).ToList();

            return compilerOutput;
        }

        public override string GetIdentifier(string name)
        {
            return CSharpTypeGenerator.Identifier(name);
        }
    }
}