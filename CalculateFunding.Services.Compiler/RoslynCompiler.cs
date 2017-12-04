using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
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
        public BudgetCompilerOutput GenerateCode(Budget budget)
        {
            MetadataReference[] references = {
                AssemblyMetadata.CreateFromFile(typeof(object).Assembly.Location).GetReference()
            };


            using (var ms = new MemoryStream())
            {
                var compilerOutput = GenerateCode(budget, references, ms);
                if (compilerOutput.Success)
                {
                    ms.Seek(0L, SeekOrigin.Begin);

                    byte[] data = new byte[ms.Length];
                    ms.Read(data, 0, data.Length);


                    compilerOutput.Assembly = Assembly.Load(data);

                }


                return compilerOutput;
            }
        }

        protected BudgetCompilerOutput GenerateCode(Budget budget, MetadataReference[] references, MemoryStream ms)
        {
            var stopwatch = new Stopwatch();

            stopwatch.Start();
            var datasetSyntaxTree = GenerateDatasetSyntaxTree(budget);
            var productSyntaxTree = GenerateProductSyntaxTree(budget);
            stopwatch.Stop();
            Logger.LogInformation($"${budget.Id} created syntax tree ({stopwatch.ElapsedMilliseconds}ms)");
            stopwatch.Restart();
            var result = GenerateCode(references, ms, datasetSyntaxTree, productSyntaxTree);

            var compilerOutput = new BudgetCompilerOutput
            {
                Budget = budget,
                //DatasetSourceCode = datasetSyntaxTrees.Select(x => x.ToString()).ToArray(),
                CalculationSourceCode = productSyntaxTree.ToString()
            };

            stopwatch.Stop();
            Logger.LogInformation($"${budget.Id} compilation complete success = {compilerOutput.Success} ({stopwatch.ElapsedMilliseconds}ms)");

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
        protected abstract SyntaxTree GenerateProductSyntaxTree(Budget budget);
        protected abstract SyntaxTree GenerateDatasetSyntaxTree(Budget budget);
        public abstract string GetIdentifier(string name);


    }
}