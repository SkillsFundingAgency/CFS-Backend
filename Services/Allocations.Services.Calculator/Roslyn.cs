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
using SyntaxFactory = Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace Allocations.Services.Calculator
{
    public  class RoslynDatasetAssemblyFactory
    {

       public Assembly Test(Budget budget)
        {
            StringBuilder sb = new StringBuilder();


            var datacon = new DatasetTypeGenerator();

            var datasetSyntaxTrees = budget.DatasetDefinitions.Select(x => datacon.Test(budget, x).SyntaxTree);

            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var references = new[]
            {
                AssemblyMetadata.CreateFromFile(typeof(object).Assembly.Location).GetReference(),
                AssemblyMetadata.CreateFromFile(typeof(ProviderSourceDataset).Assembly.Location).GetReference(),
                AssemblyMetadata.CreateFromFile(typeof(CalculationResult).Assembly.Location).GetReference(),
                AssemblyMetadata.CreateFromFile(typeof(RequiredAttribute).Assembly.Location).GetReference(),
                AssemblyMetadata.CreateFromFile(typeof(JsonPropertyAttribute).Assembly.Location).GetReference(),

            };


            var compilation = CSharpCompilation.Create("datsets.dll")
                                    .WithOptions(options)
                                    .AddSyntaxTrees(datasetSyntaxTrees)
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



            

//            Results.Text = sb.ToString();

//            if (diagnostics.Length == 0)
//            {
//                try
//                {
//                    AsmHelper _asmHelper = null;
//#if DEBUG
//                    var _assembly = CSScript.LoadCode(userinput, null, true, null);
//#else  
//                    var  _assembly = CSScript.LoadCode(userinput, null);
//#endif
//                    _asmHelper = new AsmHelper(_assembly);

//                    var methodName = "GetProductResults";

//                    var CsharpResults = _asmHelper.Invoke(string.Format("*.{0}", methodName));

//                    sb.Append("Results: " + CsharpResults);

//                    Results.Text = sb.ToString();
//                }
//                catch (Exception ex)
//                {
//                    Results.Text = ex.ToString();
//                }
//            }
        }
    }
}