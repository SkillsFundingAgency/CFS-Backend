using System.Collections.Generic;
using Allocations.Models.Results;
using Allocations.Models.Specs;
using Allocations.Services.Calculator;

namespace Allocations.Functions.Engine
{
    public class PreviewResponse
    {
        public Product Product { get; set; }
        public BudgetCompilerOutput CompilerOutput { get; set; }
        public List<ProviderTestResult> TestResults { get; set; }
    }
}