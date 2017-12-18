namespace CalculateFunding.Models.Specs
{
    public class CalculationSpecificationCommand : Command<Specification>
    {
        public string SpecificationId { get; set; }
    }
}