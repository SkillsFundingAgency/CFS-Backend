
namespace CalculateFunding.Models.Specs
{
    public class CalculationEditModel
    {
        /// <summary>
        /// Gets or sets the id of the calculation from the specs service (Calculation Specification Id)
        /// </summary>
        public string CalculationId { get; set; }

        public string SpecificationId { get; set; }

        public string AllocationLineId { get; set; }

        public string Description { get; set; }

        public string Name { get; set; }

        public CalculationType CalculationType { get; set; }
    }
}
