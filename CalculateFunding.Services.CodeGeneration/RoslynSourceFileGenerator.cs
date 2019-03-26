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

        public List<SourceFile> GenerateCode(BuildProject buildProject, IEnumerable<Calculation> calculations, CompilerOptions compilerOptions)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            List<SourceFile> sourceFiles = new List<SourceFile>();
            sourceFiles.AddRange(GenerateStaticSourceFiles());

            sourceFiles.AddRange(GenerateDatasetSourceFiles(buildProject));

            sourceFiles.AddRange(GenerateCalculationSourceFiles(buildProject, calculations, compilerOptions));
            stopwatch.Stop();
            _logger.Information($"${buildProject.Id} created syntax tree ({stopwatch.ElapsedMilliseconds}ms)");
            return sourceFiles;
        }

        protected abstract IEnumerable<SourceFile> GenerateCalculationSourceFiles(BuildProject buildProject, IEnumerable<Calculation> calculations, CompilerOptions compilerOptions);

        protected abstract IEnumerable<SourceFile> GenerateDatasetSourceFiles(BuildProject buildProject);

        protected IEnumerable<SourceFile> GenerateStaticSourceFiles()
        {
            System.Reflection.Assembly assembly = GetType().Assembly;
            string[] codeFiles = assembly.GetManifestResourceNames();

            foreach (string codeFile in codeFiles)
            {
                int prefixIndex = codeFile.LastIndexOf("CodeResources");
                if (prefixIndex > 0)
                {
                    string relative = codeFile.Substring(prefixIndex + "CodeResources.".Length);

                    string[] split = relative.Split('.');
                    if (split.Length >= 2)
                    {
                        string fileName = $"{split[split.Length - 2]}.{split[split.Length - 1]}";
                        string folderPath = "";
                        if (split.Length > 2)
                        {
                            folderPath = string.Join("\\", split.Take(split.Length - 2));
                        }

                        using (Stream stream = assembly.GetManifestResourceStream(codeFile))
                        {
                            using (StreamReader reader = new StreamReader(stream))
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