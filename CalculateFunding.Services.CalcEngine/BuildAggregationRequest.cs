namespace CalculateFunding.Services.CalcEngine
{
    public class BuildAggregationRequest
    {
        public string SpecificationId { get; set; }
        public bool GenerateCalculationAggregationsOnly { get; set; }
        public int BatchCount { get; set; }
    }
}
