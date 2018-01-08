using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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