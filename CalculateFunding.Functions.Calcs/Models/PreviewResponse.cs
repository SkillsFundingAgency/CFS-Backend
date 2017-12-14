using System.Collections.Generic;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Compiler;

namespace CalculateFunding.Functions.Calcs.Models
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