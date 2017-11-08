using System.Collections.Generic;
using System.Reflection;
using Allocations.Models.Specs;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Allocations.Services.Calculator
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum Severity
    {
        Hidden,
        Info,
        Warning,
        Error,
    }
    public class BudgetCompilerOutput
    { 
        public Assembly Assembly { get; set; }
        public bool Success { get; set; }
        public Budget Budget { get; set; }
        public List<CompilerMessage> CompilerMessages { get; set; }
        public string DatasetSourceCode { get; set; }
        public string CalculationSourceCode { get; set; }
    }

    public class CompilerMessage
    {
        public Severity Severity { get; set; }
        public string Message { get; set; }
    }
}