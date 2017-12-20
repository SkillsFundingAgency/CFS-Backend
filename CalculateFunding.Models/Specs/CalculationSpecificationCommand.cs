namespace CalculateFunding.Models.Specs
{
    public class CalculationSpecificationCommand : Command<CalculationSpecification>
    {
        public string SpecificationId { get; set; }
        public string PolicyId { get; set; }
    }
}