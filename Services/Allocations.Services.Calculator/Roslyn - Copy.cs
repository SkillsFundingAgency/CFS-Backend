using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Allocations.Models.Framework;
using Allocations.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.CodeAnalysis.VisualBasic.Syntax;

namespace Allocations.Services.Calculator
{
    public  class RoslynVisualBasicCalculator 
    {
  
 
       public Assembly Test(Budget budget)
        {
            StringBuilder sb = new StringBuilder();


            string vb =
            @"
                Imports Allocations.Models.Framework

                Public Class Class2
                    Protected Function P004_PriRate()

                        return New CalculationResult(""product1"", 1.10)
                    End Function
                End Class

            ";
            var tree = SyntaxFactory.ParseSyntaxTree(vb);

            var datacon = new DatasetTypeGenerator();

            var datasetSyntaxTrees = budget.DatasetDefinitions.Select(x => datacon.Test(budget, x).SyntaxTree);

            var options = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var sytemReference = AssemblyMetadata.CreateFromFile(typeof(object).Assembly.Location);
            var frameworkReference = AssemblyMetadata.CreateFromFile(typeof(CalculationResult).Assembly.Location);

            var compilation = VisualBasicCompilation.Create("Test.vb")
                                    .WithOptions(options)
                                    .AddSyntaxTrees(datasetSyntaxTrees)
                                    .AddReferences(sytemReference.GetReference(), frameworkReference.GetReference());

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