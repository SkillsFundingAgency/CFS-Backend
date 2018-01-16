using System.Collections.Generic;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Compiler;
using Calculation = CalculateFunding.Models.Calcs.Calculation;

namespace CalculateFunding.Functions.Calcs.Models
{
    public class PreviewResponse
    {
        public PreviewResponse()
        {
            TestResults = new List<ProviderTestResult>();
        }
        public Calculation Product { get; set; }
        public Build CompilerOutput { get; set; }
        public List<ProviderTestResult> TestResults { get; set; }
    }
}