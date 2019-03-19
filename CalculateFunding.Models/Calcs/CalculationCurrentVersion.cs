using System;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Versioning;

namespace CalculateFunding.Models.Calcs
{
    public class CalculationCurrentVersion : Reference
    {
        public string SpecificationId { get; set; }

        public string FundingPeriodName { get; set; }

        public string FundingPeriodId { get; set; }

        public Reference CalculationSpecification { get; set; }

        public string SourceCode { get; set; }

        public DateTimeOffset? Date { get; set; }

        public Reference Author { get; set; }

        public int Version { get; set; }

        public string CalculationType { get; set; }

        public PublishStatus PublishStatus { get; set; }

        public string SourceCodeName { get; set; }
    }
}
