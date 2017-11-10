using System;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Allocations.Models;
using Allocations.Models.Datasets;
using Allocations.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;

namespace Allocations.Services.Calculator
{
    public static class BudgetCompiler
    {

        public static BudgetCompilerOutput GenerateAssembly(Budget budget)
        {
            var datasetTypeGenerator = new DatasetTypeGenerator();
            var productTypeGenerator = new ProductTypeGenerator();

            var datasetSyntaxTrees = budget.DatasetDefinitions.Select(x => datasetTypeGenerator.Test(budget, x).SyntaxTree);
            var calcSyntaxTree = productTypeGenerator.GenerateCalcs(budget).SyntaxTree;

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            MetadataReference[] references = {
                AssemblyMetadata.CreateFromFile(typeof(object).Assembly.Location).GetReference(),
                AssemblyMetadata.CreateFromFile(typeof(RequiredAttribute).Assembly.Location).GetReference(),
                AssemblyMetadata.CreateFromFile(typeof(JsonPropertyAttribute).Assembly.Location).GetReference(),
            };


            var compilation = CSharpCompilation.Create("budget")
                .WithOptions(options)
                .AddSyntaxTrees(datasetSyntaxTrees)
                .AddSyntaxTrees(calcSyntaxTree)
                .AddReferences(references);


            var compilerOutput = new BudgetCompilerOutput
            {
                Budget = budget,
                DatasetSourceCode = datasetSyntaxTrees.Select(x => x.ToString()).ToArray(),
                CalculationSourceCode = calcSyntaxTree.ToString()
            };


            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);
                compilerOutput.Success = result.Success;
                compilerOutput.CompilerMessages = result.Diagnostics.Select(x => new CompilerMessage{Message = x.GetMessage(), Severity = (Severity) x.Severity}).ToList();
                if (compilerOutput.Success)
                {
                    ms.Seek(0L, SeekOrigin.Begin);

                    byte[] data = new byte[ms.Length];
                    ms.Read(data, 0, data.Length);
                    compilerOutput.Assembly = Assembly.Load(data);
                }
                


            }

            return compilerOutput;
        }
    }
}