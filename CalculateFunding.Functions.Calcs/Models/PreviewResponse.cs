using CalculateFunding.Models.Calcs;
using Calculation = CalculateFunding.Models.Calcs.Calculation;

namespace CalculateFunding.Functions.Calcs.Models
{
    public class PreviewResponse
    {
        public Calculation Calculation { get; set; }
        public Build CompilerOutput { get; set; }

    }
}