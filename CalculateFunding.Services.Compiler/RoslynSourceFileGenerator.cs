using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using CalculateFunding.Models.Calcs;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Services.Compiler
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

        protected IEnumerable<SourceFile> GenerateStaticSourceFiles(params string[] sourceFileSuffixes)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var codeFiles = assembly.GetManifestResourceNames().Where(x => sourceFileSuffixes.Any(x.EndsWith));
            foreach (var codeFile in codeFiles)
            {
                using (var stream = assembly.GetManifestResourceStream(codeFile))
                {
                    using (var reader = new StreamReader(stream))
                    {
                        var split = codeFile.Split('.');
                        yield return new SourceFile { FileName = $"{split.Reverse().Skip(1).First()}.{split.Last()}", SourceCode = reader.ReadToEnd() };
                    }

                }
            }
        }

        protected abstract IEnumerable<SourceFile> GenerateProductSourceFiles(Implementation budget);
        protected abstract IEnumerable<SourceFile> GenerateDatasetSourceFiles(Implementation budget);
        protected abstract IEnumerable<SourceFile> GenerateStaticSourceFiles(Implementation budget);
        public abstract string GetIdentifier(string name);


    }
}