using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Allocations.Models.Datasets;
using Allocations.Models.Framework;
using Allocations.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json;

namespace Allocations.Services.Calculator
{
    public class BudgetAssemblyGenerator
    {

        public Assembly GenerateAssembly(Budget budget)
        {
            StringBuilder sb = new StringBuilder();


            var datacon = new DatasetTypeGenerator();

            var datasetSyntaxTrees = budget.DatasetDefinitions.Select(x => datacon.Test(budget, x).SyntaxTree);

            var calc = new ProductFolderTypeGenerator();

            var calcSyntaxTree = calc.GenerateCalcs(budget).SyntaxTree;

            File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{budget.Name}.datasets.cs"), string.Join(Environment.NewLine, datasetSyntaxTrees.Select(x => x.ToString())));
            File.WriteAllText(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), $"{budget.Name}.cs"), calcSyntaxTree.ToString());

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var references = new[]
            {
                AssemblyMetadata.CreateFromFile(typeof(object).Assembly.Location).GetReference(),
                AssemblyMetadata.CreateFromFile(typeof(ProviderSourceDataset).Assembly.Location).GetReference(),
                AssemblyMetadata.CreateFromFile(typeof(CalculationResult).Assembly.Location).GetReference(),
                AssemblyMetadata.CreateFromFile(typeof(RequiredAttribute).Assembly.Location).GetReference(),
                AssemblyMetadata.CreateFromFile(typeof(JsonPropertyAttribute).Assembly.Location).GetReference(),
                //AssemblyMetadata.CreateFromStream(datasetAssembly).GetReference(),
            };


            var compilation = CSharpCompilation.Create($"calcs.dll")
                .WithOptions(options)
                .AddSyntaxTrees(datasetSyntaxTrees)
                .AddSyntaxTrees(calcSyntaxTree)
                .AddReferences(references);

            var diagnostics = compilation.GetDiagnostics();
            sb.AppendLine("Output:");

            foreach (var diagnostic in diagnostics)
            {
                sb.AppendLine(diagnostic.ToString());
            }

            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);

                ms.Seek(0L, SeekOrigin.Begin);

                byte[] data = new byte[ms.Length];
                ms.Read(data, 0, data.Length);
                return Assembly.Load(data);
            }
        }
    }
}