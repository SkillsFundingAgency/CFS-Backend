using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CalculateFunding.Models.Calcs;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class VisualBasicSourceFileGenerator : RoslynSourceFileGenerator
    {

        public VisualBasicSourceFileGenerator(ILogger<VisualBasicSourceFileGenerator> logger) : base(logger)
        {
        }

        protected override IEnumerable<SourceFile> GenerateProductSourceFiles(Implementation budget)
        {
            var productTypeGenerator = new ProductTypeGenerator();
            return productTypeGenerator.GenerateCalcs(budget);
        }

        protected override IEnumerable<SourceFile> GenerateDatasetSourceFiles(Implementation budget)
        {
            var datasetTypeGenerator = new DatasetTypeGenerator();
            return datasetTypeGenerator.GenerateDatasets(budget);
        }

        protected override IEnumerable<SourceFile> GenerateStaticSourceFiles(Implementation budget)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var codeFiles = Assembly.GetExecutingAssembly().GetManifestResourceNames();

            var prefix = $"{GetType().Namespace}.CodeResources.";
            foreach (var codeFile in codeFiles)
            {
                var relative = codeFile.Replace(prefix, string.Empty);

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

        public override string GetIdentifier(string name)
        {
            return VisualBasicTypeGenerator.Identifier(name);
        }
    }
}