using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Serilog;

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
            MetadataReference[] references = {
                AssemblyMetadata.CreateFromFile(typeof(object).Assembly.Location).GetReference(),
                AssemblyMetadata.CreateFromFile(typeof(Microsoft.VisualBasic.Constants).Assembly.Location).GetReference()
            };

            using (var ms = new MemoryStream())
            {
                var build = GenerateCode(sourcefiles, references, ms);

                if (build.Success)
                {
                    ms.Seek(0L, SeekOrigin.Begin);

                    byte[] data = new byte[ms.Length];
                    ms.Read(data, 0, data.Length);
                    build.AssemblyBase64 = Convert.ToBase64String(data);
                }

                return build;
            }
        }

        protected Build GenerateCode(List<SourceFile> sourceFiles, MetadataReference[] references, MemoryStream ms)
        {
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            var result = Compile(references, ms, sourceFiles);

            var compilerOutput = new Build
            {
                SourceFiles = sourceFiles,
                Success = result.Success
            };

            stopwatch.Stop();
            Logger.Information($"Compilation complete success = {compilerOutput.Success} ({stopwatch.ElapsedMilliseconds}ms)");

            compilerOutput.CompilerMessages = result.Diagnostics.Where(x  => x.Severity != DiagnosticSeverity.Hidden)
                .Select(x => new CompilerMessage {
	                    Message = x.GetMessage(),
				        Severity = (Severity)x.Severity,
				        Location = GetLocation(x)
                    })
                .ToList();

            foreach (var compilerMessage in compilerOutput.CompilerMessages)
            {
                switch (compilerMessage.Severity)
                {
                    case Severity.Info:
                        Logger.Information(compilerMessage.Message);
                        break;
                    case Severity.Warning:
                        Logger.Warning(compilerMessage.Message);
                        break;
                    case Severity.Error:
                        Logger.Error(compilerMessage.Message);
                        break;
                }
            }

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

        protected abstract IDictionary<string, string> GetCalulationFunctions(IEnumerable<SourceFile> sourceFiles);

	    protected abstract EmitResult Compile(MetadataReference[] references, MemoryStream ms, List<SourceFile> sourceFiles);

        IDictionary<string, string> ICompiler.GetCalulationFunctions(IEnumerable<SourceFile> sourceFiles)
        {
            return GetCalulationFunctions(sourceFiles);
        }
    }
}