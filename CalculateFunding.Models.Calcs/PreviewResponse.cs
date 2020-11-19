namespace CalculateFunding.Models.Calcs
{
    public class PreviewResponse
    {
        public CalculationResponseModel Calculation { get; set; }

        public Build CompilerOutput { get; set; }

        public PreviewProviderCalculationResponseModel PreviewProviderCalculation { get; set; }
    }
}