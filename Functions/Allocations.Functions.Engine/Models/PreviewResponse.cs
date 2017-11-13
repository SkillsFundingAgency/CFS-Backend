using System.Collections.Generic;
using Allocations.Models.Results;
using Allocations.Models.Specs;
using Allocations.Services.Calculator;
using Allocations.Services.Compiler;

namespace Allocations.Functions.Engine.Models
{
    public class PreviewResponse
    {
        public Product Product { get; set; }
        public BudgetCompilerOutput CompilerOutput { get; set; }
        public List<ProviderTestResult> TestResults { get; set; }
    }
}