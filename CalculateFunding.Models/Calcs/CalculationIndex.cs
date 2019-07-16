using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Calcs
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "calculationindex")]
    public class CalculationIndex
    {
        [Key]
        [IsSearchable]
        [IsRetrievable(true)]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSearchable]
        [IsRetrievable(true)]
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("calculationSpecificationId")]
        [IsRetrievable(true)]
        public string CalculationSpecificationId { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsRetrievable(true)]
        [JsonProperty("calculationSpecificationName")]
        public string CalculationSpecificationName { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsFacetable]
        [JsonProperty("specificationName")]
        [IsRetrievable(true)]
        public string SpecificationName { get; set; }

        [JsonProperty("specificationId")]
        [IsRetrievable(true)]
        public string SpecificationId { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("fundingPeriodName")]
        public string FundingPeriodName { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("fundingPeriodId")]
        public string FundingPeriodId { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsSearchable, IsRetrievable(true)]
        [JsonProperty("allocationLineName")]
        public string AllocationLineName { get; set; }

        [JsonProperty("allocationLineId")]
        [IsRetrievable(true)]
        public string AllocationLineId{ get; set; }

        [IsSearchable, IsRetrievable(true)]
        [JsonProperty("sourceCode")]
        public string SourceCode { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsSearchable, IsRetrievable(true)]
        [JsonProperty("status")]
        public string Status { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsSearchable, IsRetrievable(true)]
        [JsonProperty("fundingStreamName")]
        public string FundingStreamName { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [IsFilterable, IsSortable, IsRetrievable(true)]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset? LastUpdatedDate { get; set; }

        [IsFilterable, IsFacetable, IsSearchable, IsRetrievable(true)]
        [JsonProperty("calculationType")]
        public string CalculationType { get; set; }

        [IsSearchable, IsRetrievable(true)]
        [JsonProperty("sourceCodeName")]
        public string SourceCodeName { get; set; }
    }
}