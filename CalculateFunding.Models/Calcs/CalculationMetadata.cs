namespace CalculateFunding.Models.Calcs
{
    public class CalculationMetadata
    {
        public string CalculationId { get; set; }

        public CalculationType CalculationType { get; set; }

        public string SourceCodeName { get; set; }

        public string Name { get; set; }

        public CalculationNamespace Namespace { get; set; }

        public bool WasTemplateCalculation { get; set; }

        public CalculationValueType ValueType { get; set; }

        public string Description { get; set; }

        public string SpecificationId { get; set; }

        public string FundingStreamId { get; set; }
    }
}
