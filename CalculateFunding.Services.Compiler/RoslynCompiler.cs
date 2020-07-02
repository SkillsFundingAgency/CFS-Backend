using CalculateFunding.Common.Models;
using CalculateFunding.Models.Calcs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Serilog;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CalculateFunding.Common.Extensions;

namespace CalculateFunding.Services.Compiler
{
    public abstract class RoslynCompiler : ICompiler
    {
        protected ILogger Logger;

        protected RoslynCompiler(ILogger logger)
        {
            Logger = logger;
        }

        
        public Build GenerateCode(List<SourceFile> sourcefiles,
            IEnumerable<Calculation> calculations)
        {
            // ReSharper disable once CoVariantArrayConversion
            MetadataReference[] references = Assembly.GetExecutingAssembly().GetReferencedAssemblies().Select(_ => AssemblyMetadata.CreateFromFile(Assembly.Load(_).Location).GetReference()).ToArray();

            references = references.Concat(new[] {
                AssemblyMetadata.CreateFromFile(typeof(object).Assembly.Location).GetReference(),
                AssemblyMetadata.CreateFromFile(typeof(Microsoft.VisualBasic.Constants).Assembly.Location).GetReference()
            }).ToArray();

            Dictionary<string, Calculation> calculationsLookup = calculations.ToDictionary(_ => _.Id);
            
            using MemoryStream stream = new MemoryStream();
            
            Build build = GenerateCode(sourcefiles, references, stream, calculationsLookup);

            if (build.Success)
            {
                stream.Seek(0L, SeekOrigin.Begin);

                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);

                build.Assembly = data;
            }

            return build;
        }

        protected Build GenerateCode(List<SourceFile> sourceFiles, 
            MetadataReference[] references, 
            MemoryStream stream,
            IDictionary<string, Calculation> calculations)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            EmitResult result = Compile(references, stream, sourceFiles);

            Build compilerOutput = new Build
            {
                SourceFiles = sourceFiles,
                Success = result.Success
            };

            stopwatch.Stop();
            Logger.Information($"Compilation complete success = {compilerOutput.Success} ({stopwatch.ElapsedMilliseconds}ms)");

            compilerOutput.CompilerMessages = result.Diagnostics.Where(x => x.Severity != DiagnosticSeverity.Hidden)
                .Select(diagnostic => new CompilerMessage
                {
                    Message = diagnostic.GetMessage(),
                    Severity = diagnostic.Severity.AsMatchingEnum<Severity>(),
                    Location = GetLocation(diagnostic, calculations)
                })
                .ToList();

            return compilerOutput;
        }

        private SourceLocation GetLocation(Diagnostic diagnostic, 
            IDictionary<string, Calculation> calculations)
        {
            FileLinePositionSpan span = diagnostic.Location.GetMappedLineSpan();

            Reference owner = null;

            string[] split = span.Path.Split('|');
            
            if (split.Length == 2)
            {
                owner = new Reference(split.First(), split.Last());

                if (calculations.TryGetValue(owner.Id, out Calculation externalSource))
                {
                    DeNormaliseWhiteSpaceLinePosition originalSourceCodeLinePosition 
                        = new DeNormaliseWhiteSpaceLinePosition(span, externalSource.Current.SourceCode);
                    
                    return new SourceLocation
                    {
                        Owner = owner,
                        StartLine = originalSourceCodeLinePosition.StartLine,
                        StartChar = originalSourceCodeLinePosition.StartCharacter,
                        EndLine = originalSourceCodeLinePosition.EndLine,
                        EndChar = originalSourceCodeLinePosition.EndCharacter
                    };
                }
            }

            //increment all by 1 as roslyn uses base 0 indices for line numbers
            return new SourceLocation
            {
                Owner = owner,
                StartLine = span.StartLinePosition.Line + 1,
                StartChar = span.StartLinePosition.Character,
                EndLine = span.EndLinePosition.Line + 1,
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