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

        public List<SourceFile> GenerateCode(BuildProject buildProject)
        {
            var stopwatch = new Stopwatch();

            stopwatch.Start();


            List<SourceFile> sourceFiles = new List<SourceFile>();
            sourceFiles.AddRange(GenerateStaticSourceFiles());

            if(buildProject.DatasetDefinitions != null)
                sourceFiles.AddRange(GenerateDatasetSourceFiles(buildProject));

            sourceFiles.AddRange(GenerateProductSourceFiles(buildProject));
            stopwatch.Stop();
            Logger.LogInformation($"${buildProject.Id} created syntax tree ({stopwatch.ElapsedMilliseconds}ms)");
            return sourceFiles;

        }


        protected abstract IEnumerable<SourceFile> GenerateProductSourceFiles(BuildProject budget);
        protected abstract IEnumerable<SourceFile> GenerateDatasetSourceFiles(BuildProject budget);
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