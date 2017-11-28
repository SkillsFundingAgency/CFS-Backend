using System.Collections.Generic;
using System.Reflection;
using CalculateFunding.Models.Specs;
using Newtonsoft.Json;

namespace CalculateFunding.Services.Compiler
{
    public class BudgetCompilerOutput
    {
        [JsonIgnore]
        public Assembly Assembly { get; set; }
        public bool Success { get; set; }
        public Budget Budget { get; set; }
        public List<CompilerMessage> CompilerMessages { get; set; }
        public string DatasetSourceCode { get; set; }
        public string CalculationSourceCode { get; set; }
    }
}
