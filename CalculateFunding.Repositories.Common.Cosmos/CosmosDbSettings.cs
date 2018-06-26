namespace CalculateFunding.Repositories.Common.Cosmos
{
    public class CosmosDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
        public string PartitionKey { get; set; }

    }
}