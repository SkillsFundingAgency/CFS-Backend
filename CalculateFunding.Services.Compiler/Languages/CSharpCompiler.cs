using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalculateFunding.Models.Calcs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using Serilog;

namespace CalculateFunding.Services.Compiler.Languages
{
    public class CSharpCompiler : RoslynCompiler
    {
        private readonly ILogger _logger;

        public CSharpCompiler(ILogger logger) : base(logger)
        {
        }


        protected override EmitResult Compile(MetadataReference[] references, MemoryStream ms, List<SourceFile> sourceFiles)
        {
            var options = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var syntaxTrees = sourceFiles.Where(x => x.FileName.EndsWith(".cs"))
                .Select(x => SyntaxFactory.ParseSyntaxTree(x.SourceCode));

            var compilation = CSharpCompilation.Create("budget")
                .WithOptions(options)
                .AddSyntaxTrees(syntaxTrees)
                .AddReferences(references);


            var result = compilation.Emit(ms);
            return result;
        }

    }
}