using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Serilog;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CalculateFunding.Services.Compiler
{
    public abstract class RoslynCompiler : ICompiler
    {
        protected ILogger Logger;

        protected RoslynCompiler(ILogger logger)
        {
            Logger = logger;
        }

        public Build GenerateCode(List<SourceFile> sourcefiles)
        {
            MetadataReference[] references = Assembly.GetExecutingAssembly().GetReferencedAssemblies().Select(_ => AssemblyMetadata.CreateFromFile(Assembly.Load(_).Location).GetReference()).ToArray();

            references = references.Concat(new[] {
                AssemblyMetadata.CreateFromFile(typeof(object).Assembly.Location).GetReference(),
                AssemblyMetadata.CreateFromFile(typeof(Microsoft.VisualBasic.Constants).Assembly.Location).GetReference()
            }).ToArray();

            using (var ms = new MemoryStream())
            {
                var build = GenerateCode(sourcefiles, references, ms);

                if (build.Success)
                {
                    ms.Seek(0L, SeekOrigin.Begin);

                    byte[] data = new byte[ms.Length];
                    ms.Read(data, 0, data.Length);

                    build.Assembly = data;
                }

                return build;
            }
        }

        protected Build GenerateCode(List<SourceFile> sourceFiles, MetadataReference[] references, MemoryStream ms)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            EmitResult result = Compile(references, ms, sourceFiles);

            Build compilerOutput = new Build
            {
                SourceFiles = sourceFiles,
                Success = result.Success
            };

            stopwatch.Stop();
            Logger.Information($"Compilation complete success = {compilerOutput.Success} ({stopwatch.ElapsedMilliseconds}ms)");

            compilerOutput.CompilerMessages = result.Diagnostics.Where(x => x.Severity != DiagnosticSeverity.Hidden)
                .Select(x => new CompilerMessage
                {
                    Message = x.GetMessage(),
                    Severity = (Severity)x.Severity,
                    Location = GetLocation(x)
                })
                .ToList();

            return compilerOutput;
        }

        private SourceLocation GetLocation(Diagnostic diagnostic)
        {
            var span = diagnostic.Location.GetMappedLineSpan();

            Reference owner = null;

            var split = span.Path?.Split('|');
            if (split != null && split.Length == 2)
            {
                owner = new Reference(split.First(), split.Last());
            }

            return new SourceLocation
            {
                Owner = owner,
                StartLine = span.StartLinePosition.Line,
                StartChar = span.StartLinePosition.Character,
                EndLine = span.EndLinePosition.Line,
                EndChar = span.EndLinePosition.Character
            };
        }

        protected IEnumerable<TSyntaxType> GetDescendants<TSyntaxType>(SyntaxNode syntaxNode)
            where TSyntaxType : SyntaxNode
        {
            return syntaxNode?.DescendantNodes().OfType<TSyntaxType>();
        }

        protected IEnumerable<TSyntaxType> GetDescendants<TSyntaxType>(SyntaxTree syntaxTree)
            where TSyntaxType : SyntaxNode
        {
            return GetDescendants<TSyntaxType>(syntaxTree.GetRoot());
        }

        protected abstract IDictionary<string, string> GetCalculationFunctions(IEnumerable<SourceFile> sourceFiles);

        protected abstract EmitResult Compile(MetadataReference[] references, MemoryStream ms, List<SourceFile> sourceFiles);

        IDictionary<string, string> ICompiler.GetCalculationFunctions(IEnumerable<SourceFile> sourceFiles)
        {
            return GetCalculationFunctions(sourceFiles);
        }
    }
}