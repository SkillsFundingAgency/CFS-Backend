using System.IO;
using System.Linq;
using CalculateFunding.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;

namespace CalculateFunding.Services.Compiler.VisualBasic
{
    public class VisualBasicCompiler : BaseCompiler
    {
        protected override BudgetCompilerOutput Compile(Budget budget, MetadataReference[] references, MemoryStream ms)
        {
            var datasetTypeGenerator = new DatasetTypeGenerator();
            var productTypeGenerator = new ProductTypeGenerator();

            var datasetSyntaxTrees = datasetTypeGenerator.GenerateDatasets(budget).SyntaxTree;
            var calcSyntaxTree = productTypeGenerator.GenerateCalcs(budget).SyntaxTree;

             var compilerOutput = new BudgetCompilerOutput
            {
                Budget = budget,
                DatasetSourceCode = datasetSyntaxTrees.ToString(),
                CalculationSourceCode = calcSyntaxTree.ToString()
            };

            var options = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);


            var compilation = VisualBasicCompilation.Create("budget")
                .WithOptions(options)
                .AddSyntaxTrees(datasetSyntaxTrees)
                .AddSyntaxTrees(calcSyntaxTree)
                .AddReferences(references);


            var result = compilation.Emit(ms);
            compilerOutput.Success = result.Success;
            compilerOutput.CompilerMessages = result.Diagnostics.Select(x => new CompilerMessage { Message = x.GetMessage(), Severity = (Severity)x.Severity }).ToList();

            return compilerOutput;
        }

        public override string GetIdentifier(string name)
        {
            return VisualBasicTypeGenerator.Identifier(name);
        }
    }
}