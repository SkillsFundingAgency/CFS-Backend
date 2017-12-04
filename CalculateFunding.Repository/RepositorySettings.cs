namespace CalculateFunding.Repository
{
    public class RepositorySettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
        public string PartitionKey { get; set; }

    }
}