namespace CalculateFunding.Services.Results.Models
{
    public class ProviderResultDataKey
    {
        public ProviderResultDataKey(string providerResultId, string partitionKey)
        {
            ProviderResultId = providerResultId;
            PartitionKey = partitionKey;
        }

        public string ProviderResultId { get; private set; }
        public string PartitionKey { get; private set; }

        public override string ToString()
        {
            return $"ProviderResultId: {ProviderResultId}; PartitionKey: {PartitionKey}";
        }
    }
}
