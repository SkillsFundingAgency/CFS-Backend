using System;
using Newtonsoft.Json;

namespace Allocations.Models.Datasets
{
    public class ProviderSourceDataset : DocumentEntity
    {
        public override string Id => $"{BudgetId}-{ProviderUrn}-{DatasetName}".ToSlug();

        [JsonProperty("budgetId")]
        public string BudgetId { get; set; }
        [JsonProperty("providerUrn")]
        public string ProviderUrn { get; set;  }
        [JsonProperty("datasetName")]
        public string DatasetName { get; set; }

    }
}

