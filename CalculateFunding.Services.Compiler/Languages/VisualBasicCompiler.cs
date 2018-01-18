using System.Collections.Generic;
using System.IO;
using System.Linq;
using CalculateFunding.Models.Calcs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.VisualBasic;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Services.Compiler.Languages
{
    public class VisualBasicCompiler : RoslynCompiler
    {
        
        public VisualBasicCompiler(ILogger<VisualBasicCompiler> logger) : base(logger)
        {         
        }

        protected override EmitResult Compile(MetadataReference[] references, MemoryStream ms, List<SourceFile> sourceFiles)
        {
            var options = new VisualBasicCompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            var syntaxTrees = sourceFiles.Where(x => x.FileName.EndsWith(".vb"))
                .Select(x => SyntaxFactory.ParseSyntaxTree(x.SourceCode));

            var compilation = VisualBasicCompilation.Create("implementation.dll")
                .WithOptions(options)
                .AddSyntaxTrees(syntaxTrees)
                .AddReferences(references);

            return compilation.Emit(ms);
        }

    }
}