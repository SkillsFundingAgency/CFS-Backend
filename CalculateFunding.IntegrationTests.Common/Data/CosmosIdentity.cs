namespace CalculateFunding.IntegrationTests.Common.Data
{
    public readonly struct CosmosIdentity
    {
        public CosmosIdentity(string id,
            string partitionKey)
        {
            Id = id;
            PartitionKey = partitionKey;
        }

        public string Id { get; }

        public string PartitionKey { get; }
    }
}