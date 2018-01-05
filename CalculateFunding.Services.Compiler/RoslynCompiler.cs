using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Specs;
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
        public Build GenerateCode(Implementation implementation)
        {
            MetadataReference[] references = {
                AssemblyMetadata.CreateFromFile(typeof(object).Assembly.Location).GetReference(),
                AssemblyMetadata.CreateFromFile(typeof(DisplayNameAttribute).Assembly.Location).GetReference(),
            };


            using (var ms = new MemoryStream())
            {
                var build = GenerateCode(implementation, references, ms);
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

        protected Build GenerateCode(Implementation implementation, MetadataReference[] references, MemoryStream ms)
        {
            var stopwatch = new Stopwatch();

            stopwatch.Start();
            var datasetSyntaxTree = GenerateDatasetSyntaxTree(implementation);
            var productSyntaxTree = GenerateProductSyntaxTree(implementation);
            stopwatch.Stop();
            Logger.LogInformation($"${implementation.Id} created syntax tree ({stopwatch.ElapsedMilliseconds}ms)");
            stopwatch.Restart();
            var result = GenerateCode(references, ms, datasetSyntaxTree, productSyntaxTree);

            var compilerOutput = new Build
            {
                //DatasetSourceCode = datasetSyntaxTrees.Select(x => x.ToString()).ToArray(),
                CalculationSourceCode = productSyntaxTree.ToString()
            };

            stopwatch.Stop();
            Logger.LogInformation($"${implementation.Id} compilation complete success = {compilerOutput.Success} ({stopwatch.ElapsedMilliseconds}ms)");

            compilerOutput.Success = result.Success;
            compilerOutput.CompilerMessages = result.Diagnostics.Select(x => new CompilerMessage { Message = x.GetMessage(), Severity = (Severity)x.Severity }).ToList();

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

        protected abstract EmitResult GenerateCode(MetadataReference[] references, MemoryStream ms, SyntaxTree datasetSyntaxTree, SyntaxTree calcSyntaxTree);
        protected abstract SyntaxTree GenerateProductSyntaxTree(Implementation budget);
        protected abstract SyntaxTree GenerateDatasetSyntaxTree(Implementation budget);
        public abstract string GetIdentifier(string name);


    }
}