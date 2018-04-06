namespace CalculateFunding.Functions.Calcs.Models
{

    public class PreviewRequest
    {
        public string SpecificationId { get; set; }
        public string CalculationId { get; set; }

        public decimal? DecimalPlaces { get; set; }
        public string SourceCode { get; set; }
    }
}

