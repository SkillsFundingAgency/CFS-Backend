using System;
using System.Collections.Generic;

namespace CalculateFunding.Models
{
    public class CalculationResult
    {
        public string CalculationId { get; set; }
        public Reference CalculationSpecification { get; set; }
        public Reference AllocationLine { get; set; }
        public List<Reference> PolicySpecifications{ get; set; }

        public decimal Value { get; set; }

        public Exception Exception { get; set; }

    }
}