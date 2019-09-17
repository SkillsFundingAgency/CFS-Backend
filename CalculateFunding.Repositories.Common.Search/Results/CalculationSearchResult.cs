using System;

namespace CalculateFunding.Repositories.Common.Search.Results
{

    public class CalculationSearchResult
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string FundingStreamId { get; set; }
        public string SpecificationId { get; set; }
        public string SpecificationName { get; set; }
        public string ValueType { get; set; }
        public string CalculationType { get; set; }
        public string Namespace { get; set; }
        public bool WasTemplateCalculation { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public DateTimeOffset? LastUpdatedDate { get; set; }
    }
}
