using System.ComponentModel;
using Newtonsoft.Json;

namespace Allocations.Models.Specs
{

    public class Product
    {
        [JsonProperty("id")]
        public string Id => $"{Name}".ToSlug();
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("featureFile")]
        public string FeatureFile { get; set; }
        [JsonProperty("calculation")]
        public ProductCalculation Calculation { get; set; }
        [JsonProperty("testProviders")]
        public Reference[] TestProviders { get; set; }
    }

    //[] Move to budget/folder??
    public enum CalculationType
    {
        VisualBasic,
        CSharp
    }

    public class ProductCalculation
    {
        [JsonProperty("description")]
        public CalculationType CalculationType { get; set; }
        [JsonProperty("sourceCode")]
        public string SourceCode { get; set; }
    }
}