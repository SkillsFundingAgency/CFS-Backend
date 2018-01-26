using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CalculateFunding.Models.Calcs;
using Serilog;

namespace CalculateFunding.Services.CodeGeneration.VisualBasic
{
    public class VisualBasicSourceFileGenerator : RoslynSourceFileGenerator
    {

        public VisualBasicSourceFileGenerator(ILogger logger) : base(logger)
        {
        }

        protected override IEnumerable<SourceFile> GenerateProductSourceFiles(BuildProject budget)
        {
            var productTypeGenerator = new ProductTypeGenerator();
            return productTypeGenerator.GenerateCalcs(budget);
        }

        protected override IEnumerable<SourceFile> GenerateDatasetSourceFiles(BuildProject budget)
        {
            var datasetTypeGenerator = new DatasetTypeGenerator();
            return datasetTypeGenerator.GenerateDatasets(budget);
        }


        public override string GetIdentifier(string name)
        {
            return VisualBasicTypeGenerator.Identifier(name);
        }
    }
}