using System.Collections.Generic;
using Allocations.Models.Results;
using Allocations.Services.Calculator;
using Allocations.Services.Compiler;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;

namespace Allocations.Functions.Engine.Models
{
    public class PreviewResponse
    {
        public PreviewResponse()
        {
            TestResults = new List<ProviderTestResult>();
        }
        public Product Product { get; set; }
        public BudgetCompilerOutput CompilerOutput { get; set; }
        public List<ProviderTestResult> TestResults { get; set; }
    }
}