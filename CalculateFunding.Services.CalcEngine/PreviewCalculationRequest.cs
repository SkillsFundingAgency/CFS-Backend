using CalculateFunding.Models.Calcs;

namespace CalculateFunding.Services.CalcEngine
{
    public class PreviewCalculationRequest
    {
        public byte[] AssemblyContent { get; set; }
        public CalculationSummaryModel PreviewCalculationSummaryModel { get; set; }
    }
}
