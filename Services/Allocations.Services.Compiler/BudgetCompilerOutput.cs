using System.Collections.Generic;
using System.Reflection;
using Allocations.Models.Specs;

namespace Allocations.Services.Compiler
{
    public class BudgetCompilerOutput
    {
        public Assembly Assembly { get; set; }
        public bool Success { get; set; }
        public Budget Budget { get; set; }
        public List<CompilerMessage> CompilerMessages { get; set; }
        public string[] DatasetSourceCode { get; set; }
        public string CalculationSourceCode { get; set; }
    }
}
