namespace CalculateFunding.Models.Specs
{
    public class CalculationEditModel
    {
        public string CalculationId { get; set; }

        public string SpecificationId { get; set; }

        public string AllocationLineId { get; set; }

        public string PolicyId { get; set; }

        public string Description { get; set; }

        public string Name { get; set; }

        public string CalculationType { get; set; }

        public bool IsPublic { get; set; }
    }
}
