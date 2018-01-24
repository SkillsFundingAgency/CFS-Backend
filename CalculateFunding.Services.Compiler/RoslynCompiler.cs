﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CalculateFunding.Models.Calcs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.Extensions.Logging;

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
                AssemblyMetadata.CreateFromFile(typeof(object).Assembly.Location).GetReference()
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
                SourceFiles = sourceFiles
            };


            stopwatch.Stop();
            Logger.LogInformation($"Compilation complete success = {compilerOutput.Success} ({stopwatch.ElapsedMilliseconds}ms)");

            compilerOutput.Success = result.Success;


			
            compilerOutput.CompilerMessages = result.Diagnostics.Select(x => new CompilerMessage
            {
	            Message = x.GetMessage(),
				Severity = (Severity)x.Severity,
				Location = GetLocation(x)
            }).ToList();

            foreach (var compilerMessage in compilerOutput.CompilerMessages)
            {
                switch (compilerMessage.Severity)
                {
                    case Severity.Info:
                        Logger.LogInformation(compilerMessage.Message);
                        break;
                    case Severity.Warning:
                        Logger.LogWarning(compilerMessage.Message);
                        break;
                    case Severity.Error:
                        Logger.LogError(compilerMessage.Message);
                        break;
                }
            }
            return compilerOutput;
        }

	    private SourceLocation GetLocation(Diagnostic diagnostic)
	    {
		    var span = diagnostic.Location.GetMappedLineSpan();

			    return new SourceLocation
			    {
				    MappedId = span.Path,
				    StartLine = span.StartLinePosition.Line,
				    StartChar = span.StartLinePosition.Character,
				    EndLine = span.EndLinePosition.Line,
				    EndChar = span.EndLinePosition.Character
			    };


	    }

	    protected abstract EmitResult Compile(MetadataReference[] references, MemoryStream ms, List<SourceFile> sourceFiles);
    }
}