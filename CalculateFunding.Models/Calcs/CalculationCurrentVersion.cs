using CalculateFunding.Models.Results;
using System;

namespace CalculateFunding.Models.Calcs
{
    public class CalculationCurrentVersion : Reference
    {
        public string SpecificationId { get; set; }

        public string FundingPeriodName { get; set; }

        public Reference CalculationSpecification { get; set; }

        public string Status { get; set; }

        public string SourceCode { get; set; }

        public DateTime? Date { get; set; }

        public Reference Author { get; set; }

        public int Version { get; set; }

        public string CalculationType { get; set; }

        public SpecificationSummary Specification { get; set; }
    }
}
