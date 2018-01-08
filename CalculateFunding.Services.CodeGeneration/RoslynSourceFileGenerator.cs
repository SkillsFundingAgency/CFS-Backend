using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CalculateFunding.Models.Calcs;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Services.CodeGeneration
{
    public abstract class RoslynSourceFileGenerator : ISourceFileGenerator
    {
        protected ILogger Logger;


        protected RoslynSourceFileGenerator(ILogger logger)
        {
            Logger = logger;
        }

        public List<SourceFile> GenerateCode(Implementation implementation)
        {
            var stopwatch = new Stopwatch();

            stopwatch.Start();


            List<SourceFile> sourceFiles = new List<SourceFile>();
            sourceFiles.AddRange(GenerateStaticSourceFiles(implementation));
            sourceFiles.AddRange(GenerateDatasetSourceFiles(implementation));
            sourceFiles.AddRange(GenerateProductSourceFiles(implementation));
            stopwatch.Stop();
            Logger.LogInformation($"${implementation.Id} created syntax tree ({stopwatch.ElapsedMilliseconds}ms)");
            return sourceFiles;

        }


        protected abstract IEnumerable<SourceFile> GenerateProductSourceFiles(Implementation budget);
        protected abstract IEnumerable<SourceFile> GenerateDatasetSourceFiles(Implementation budget);
        protected abstract IEnumerable<SourceFile> GenerateStaticSourceFiles(Implementation budget);
        public abstract string GetIdentifier(string name);


    }
}