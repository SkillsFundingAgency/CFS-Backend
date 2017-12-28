using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class ProviderSourceDataset : Reference
    {
        [JsonProperty("budgetId")]
        public string BudgetId { get; set; }
        [JsonProperty("providerUrn")]
        public string ProviderUrn { get; set;  }
        [JsonProperty("providerName")]
        public string ProviderName { get; set; }
        [JsonProperty("datasetName")]
        public string DatasetName { get; set; }

    }
}

