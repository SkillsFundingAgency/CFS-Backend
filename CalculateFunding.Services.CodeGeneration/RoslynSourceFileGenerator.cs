using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CalculateFunding.Models.Calcs;
using Serilog;

namespace CalculateFunding.Services.CodeGeneration
{
    public abstract class RoslynSourceFileGenerator : ISourceFileGenerator
    {
        protected ILogger _logger;

        protected RoslynSourceFileGenerator(ILogger logger)
        {
            _logger = logger;
        }

        public List<SourceFile> GenerateCode(BuildProject buildProject)
        {
            var stopwatch = new Stopwatch();

            stopwatch.Start();

            List<SourceFile> sourceFiles = new List<SourceFile>();
            sourceFiles.AddRange(GenerateStaticSourceFiles());

            sourceFiles.AddRange(GenerateDatasetSourceFiles(buildProject));

            sourceFiles.AddRange(GenerateCalculationSourceFiles(buildProject));
            stopwatch.Stop();
            _logger.Information($"${buildProject.Id} created syntax tree ({stopwatch.ElapsedMilliseconds}ms)");
            return sourceFiles;
        }

        protected abstract IEnumerable<SourceFile> GenerateCalculationSourceFiles(BuildProject buildProject);

        protected abstract IEnumerable<SourceFile> GenerateDatasetSourceFiles(BuildProject buildProject);

        protected IEnumerable<SourceFile> GenerateStaticSourceFiles()
        {
            var assembly = GetType().Assembly;
            var codeFiles = assembly.GetManifestResourceNames();

            foreach (var codeFile in codeFiles)
            { 
                var prefixIndex = codeFile.LastIndexOf("CodeResources");
                if (prefixIndex > 0)
                {
                    var relative = codeFile.Substring(prefixIndex + "CodeResources.".Length);

                    var split = relative.Split('.');
                    if (split.Length >= 2)
                    {
                        var fileName = $"{split[split.Length - 2]}.{split[split.Length - 1]}";
                        var folderPath = "";
                        if (split.Length > 2)
                        {
                            folderPath = string.Join("\\", split.Take(split.Length - 2));
                        }

                        using (var stream = assembly.GetManifestResourceStream(codeFile))
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                yield return new SourceFile { FileName = Path.Combine(folderPath, fileName), SourceCode = reader.ReadToEnd() };
                            }

                        }
                    }
                }
            }
        }
        public abstract string GetIdentifier(string name);
    }
}