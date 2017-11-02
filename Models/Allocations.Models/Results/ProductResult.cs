using System;
using System.Runtime.Serialization;
using Allocations.Models.Specs;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Schema;

namespace Allocations.Models.Results
{
    public class ProviderResult : DocumentEntity
    {
        public override string Id => $"{DocumentType}-{Budget.Id}-{Provider.Id}".ToSlug();

        [JsonProperty("budget")]
        public Reference Budget { get; set; }
        [JsonProperty("provider")]
        public Reference Provider { get; set; }

        [JsonProperty("sourceDatasets")]
        public object[] SourceDatasets { get; set; }

        [JsonProperty("products")]
        public ProductResult[] ProductResults { get; set; }
    }

    public class ProductResult 
    {

        [JsonProperty("fundingPolicy")]
        public Reference FundingPolicy { get; set; }
        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }
        [JsonProperty("productFolder")]
        public Reference ProductFolder { get; set; }
        [JsonProperty("product")]
        public Product Product { get; set; }
        [JsonProperty("value")]
        public decimal? Value { get; set; }

    }

    public class ProviderTestResult : DocumentEntity
    {
        public override string Id => $"{DocumentType}-{Budget.Id}-{Provider.Id}".ToSlug();

        [JsonProperty("budget")]
        public Reference Budget { get; set; }
        [JsonProperty("provider")]
        public Reference Provider { get; set; }

        [JsonProperty("scenarioResults")]
        public ProductTestScenarioResult[] ScenarioResults { get; set; }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum TestResult
    {
        Inconclusive,
        Failed,
        Passed
    }

    public class ProductTestScenarioResult
    {
        [JsonProperty("fundingPolicy")]
        public Reference FundingPolicy { get; set; }
        [JsonProperty("allocationLine")]
        public Reference AllocationLine { get; set; }
        [JsonProperty("productFolder")]
        public Reference ProductFolder { get; set; }
        [JsonProperty("product")]
        public Product Product { get; set; }
        [JsonProperty("scenarioName")]
        public string ScenarioName { get; set; }
        [JsonProperty("scenarioDescription")]
        public string ScenarioDescription { get; set; }

        [JsonProperty("testResult")]
        public TestResult TestResult { get; set; }

        [JsonProperty("lastFailedDate")]
        public DateTime? LastFailedDate { get; set; }

        [JsonProperty("lastPassedDate")]
        public DateTime? LastPassedDate { get; set; }

        [JsonProperty("tags")]
        public string[] Tags { get; set; }
    }

}