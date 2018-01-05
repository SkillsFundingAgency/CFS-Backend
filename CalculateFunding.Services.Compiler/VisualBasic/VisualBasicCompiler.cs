using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CalculateFunding.Models.Calcs;
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
                .AddSyntaxTrees(GetCodeResourcesSyntaxTree().ToArray())
                .AddSyntaxTrees(datasetSyntaxTree)
                .AddSyntaxTrees(calcSyntaxTree)
                .AddReferences(references);


            return compilation.Emit(ms);
        }

        private IEnumerable<SyntaxTree> GetCodeResourcesSyntaxTree()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var codeFiles = assembly.GetManifestResourceNames().Where(x => x.EndsWith(".vb"));
            foreach (var codeFile in codeFiles)
            {
                using (var stream = assembly.GetManifestResourceStream(codeFile))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        yield return VisualBasicSyntaxTree.ParseText(reader.ReadToEnd());
                    }

                }
            }
        }


        protected override SyntaxTree GenerateProductSyntaxTree(Implementation budget)
        {
            var productTypeGenerator = new ProductTypeGenerator();
            return productTypeGenerator.GenerateCalcs(budget).SyntaxTree;
        }

        protected override SyntaxTree GenerateDatasetSyntaxTree(Implementation budget)
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