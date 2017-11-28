using Newtonsoft.Json;

namespace CalculateFunding.Models.Datasets
{
    public class AggregateDataset : DocumentEntity
    {
        public override string Id => $"{BudgetId}-{DatasetName}-aggregate".ToSlug();

        [JsonProperty("budgetId")]
        public string BudgetId { get; set; }
        [JsonProperty("datasetName")]
        public string DatasetName { get; set; }
        [JsonProperty("aggregateFields")]
        public AggregateField[] AggregateFields { get; set; }
    }
}