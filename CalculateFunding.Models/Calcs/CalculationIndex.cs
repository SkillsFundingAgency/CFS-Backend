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
        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("id")]
        public string Id { get; set; }

        [IsSearchable, IsSortable, IsFilterable, IsRetrievable(true)]
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("specificationId")]
        [IsFilterable, IsRetrievable(true)]
        public string SpecificationId { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsSearchable, IsRetrievable(true)]
        [JsonProperty("specificationName")]
        public string SpecificationName { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("valueType")]
        public string ValueType { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("calculationType")]
        public string CalculationType { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("namespace")]
        public string Namespace { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("fundingStreamId")]
        public string FundingStreamId { get; set; }

        [IsFilterable, IsSortable, IsSearchable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("fundingStreamName")]
        public string FundingStreamName { get; set; }

        [IsFilterable, IsRetrievable(true)]
        [JsonProperty("wasTemplatecalculation")]
        public bool WasTemplateCalculation { get; set; }

        [IsRetrievable(true)]
        [JsonProperty("description")]
        public string Description { get; set; }

        [IsFilterable, IsSortable, IsFacetable, IsSearchable, IsRetrievable(true)]
        [JsonProperty("status")]
        public string Status { get; set; }

        [IsFilterable, IsSortable, IsRetrievable(true)]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset? LastUpdatedDate { get; set; }
    }
}