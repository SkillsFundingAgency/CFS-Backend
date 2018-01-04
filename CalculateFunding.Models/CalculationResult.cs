using System;

namespace CalculateFunding.Models
{
    public class CalculationResult
    {
        public string CalculationId { get; set; }
        public string CalculationName { get; set; }
        public string PolicyId { get; set; }
        public string PolicyName { get; set; }
        public string AllocationLineId { get; set; }
        public string AllocationLineName { get; set; }
        public decimal Value { get; set; }

        public Exception Exception { get; set; }
    }
}