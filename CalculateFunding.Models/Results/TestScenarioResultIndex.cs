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
        [JsonProperty("testResult")]
        public string TestResult { get; set; }

        [IsFacetable]
        [JsonProperty("specificationId")]
        public string SpecificationId { get; set; }

        [IsSearchable]
        [IsFacetable]
        [JsonProperty("specificationName")]
        public string SpecificationName { get; set; }

        [IsFacetable]
        [JsonProperty("testScenarioId")]
        public string TestScenarioId { get; set; }

        [IsSearchable]
        [IsFacetable]
        [JsonProperty("testScenarioName")]
        public string TestScenarioName { get; set; }

        [IsFacetable]
        [JsonProperty("providerId")]
        public string ProviderId { get; set; }

        [IsSearchable]
        [IsFacetable]
        [JsonProperty("providerName")]
        public string ProviderName { get; set; }
    }
}