namespace CalculateFunding.Models.Specs
{
    public class CalculationVersionComparisonModel
    {
        public string CalculationId { get; set; }

        public string SpecificationId { get; set; }

        public Calculation Current { get; set; }

        public Calculation Previous { get; set; }
    }
}