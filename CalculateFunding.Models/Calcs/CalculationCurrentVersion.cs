using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Models.Calcs
{
    public class CalculationCurrentVersion : Reference
    {
        public string SpecificationId { get; set; }

        public string PeriodName { get; set; }

        public Reference CalculationSpecification { get; set; }

        public string Status { get; set; }

        public string SourceCode { get; set; }

        public DateTime? Date { get; set; }

        public Reference Author { get; set; }

        public int Version { get; set; }
    }
}
