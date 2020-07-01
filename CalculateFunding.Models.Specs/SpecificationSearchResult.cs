using System;

namespace CalculateFunding.Models.Specs
{
    public class SpecificationDatasetRelationshipsSearchResult
    {
        public string SpecificationId { get; set; }

        public string SpecificationName { get; set; }

        public int DefinitionRelationshipCount { get; set; }
        
        public string[] FundingStreamNames { get; set; }
        
        public string FundingPeriodName { get; set; }
        
        public DateTimeOffset? MapDatasetLastUpdated { get; set; }
        
        public int TotalMappedDataSets { get; set; }
    }
}
