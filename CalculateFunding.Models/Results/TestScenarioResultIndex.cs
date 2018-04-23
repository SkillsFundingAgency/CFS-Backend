using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.Azure.Search;
using Newtonsoft.Json;

namespace CalculateFunding.Models.Results
{
    [SearchIndex()]
    public class TestScenarioResultIndex
    {
        /// <summary>
        /// ID is the TestScenarioId and ProviderId combined with an _
        /// </summary>
        [Key]
        [IsSearchable]
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
        [JsonProperty("testResult")]
        public string TestResult { get; set; }

        [IsFilterable]
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsSortable]
        [JsonProperty("specificationName")]
        public string SpecificationName { get; set; }

        [IsFilterable]
        [IsFacetable]
        [JsonProperty("testScenarioId")]
        public string TestScenarioId { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsSortable]
        [JsonProperty("testScenarioName")]
        public string TestScenarioName { get; set; }

        [IsFilterable]
        [IsFacetable]
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsSortable]
        [JsonProperty("providerName")]
        public string ProviderName { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [IsSortable]
        [JsonProperty("providerType")]
        public string ProviderType { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [IsSortable]
        [JsonProperty("localAuthority")]
        public string LocalAuthority { get; set; }

        [IsSearchable]
        [IsFacetable]
        [IsFilterable]
        [IsSortable]
        [JsonProperty("providerSubType")]
        public string ProviderSubType { get; set; }

        [IsFilterable, IsSortable]
        [JsonProperty("lastUpdatedDate")]
        public DateTimeOffset LastUpdatedDate { get; set; }

        [JsonProperty("ukPrn")]
        public string UKPRN { get; set; }

        [JsonProperty("urn")]
        public string URN { get; set; }

        [JsonProperty("upin")]
        public string UPIN { get; set; }

	    [JsonProperty("establishmentNumber")]
        public string EstablishmentNumber { get; set; }

        [JsonProperty("openDate")]
        public DateTimeOffset? OpenDate { get; set; }
    }
}