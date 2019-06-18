using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results.Search
{
    [SearchIndex(IndexerType = IndexerType.Search, IndexName = "testscenarioresultindex")]
    public class TestScenarioResultIndex
    {
        /// <summary>
        /// ID is the TestScenarioId and ProviderId combined with an _
        /// </summary>
        [Key]
        [IsSearchable]
        [IsRetrievable(true)]
        [JsonProperty("id")]
        public string Id
        {
            get
            {
                return $"{TestScenarioId}_{ProviderId}";
            }
        }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("testResult")]
        public string TestResult { get; set; }

        [IsFilterable, IsFacetable, IsRetrievable(true)]
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsSortable]
        [IsFilterable]
        [IsRetrievable(true)]
        [JsonProperty("specificationName")]
        public string SpecificationName { get; set; }

        [IsFilterable]
        [IsFacetable]
        [IsRetrievable(true)]
        [JsonProperty("testScenarioId")]
        public string TestScenarioId { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsSortable]
        [IsRetrievable(true)]
        [JsonProperty("testScenarioName")]
        public string TestScenarioName { get; set; }

        [IsFilterable]
        [IsFacetable]
        [IsRetrievable(true)]
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsSortable]
        [IsRetrievable(true)]
        [JsonProperty("providerName")]
        public string ProviderName { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [IsSortable]
        [IsRetrievable(true)]
        [JsonProperty("providerType")]
        public string ProviderType { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [IsSortable]
        [IsRetrievable(true)]
        [JsonProperty("localAuthority")]
        public string LocalAuthority { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [IsSortable]
        [IsRetrievable(true)]
        [JsonProperty("providerSubType")]
        public string ProviderSubType { get; set; }

        [IsFilterable, IsSortable, IsRetrievable(true)]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset LastUpdatedDate { get; set; }

        [JsonProperty("ukPrn")]
        [IsRetrievable(true)]
        public string UKPRN { get; set; }

        [JsonProperty("urn")]
        [IsRetrievable(true)]
        public string URN { get; set; }

        [JsonProperty("upin")]
        [IsRetrievable(true)]
        public string UPIN { get; set; }

	    [JsonProperty("establishmentNumber")]
        [IsRetrievable(true)]
        public string EstablishmentNumber { get; set; }

        [JsonProperty("openDate")]
        [IsRetrievable(true)]
        public DateTimeOffset? OpenDate { get; set; }
    }
}