using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    public class CalculationVersionComparisonModel
    {
        public string CalculationId { get; set; }

        public string SpecificationId { get; set; }

        public string CurrentName { get; set; }

        public string PreviousName { get; set; }

        public string Namespace { get; set; }

        public CalculationDataType CalculationDataType { get; set; }
    }
}