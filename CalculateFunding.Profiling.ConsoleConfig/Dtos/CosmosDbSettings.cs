namespace CalculateFunding.Profiling.ConsoleConfig.Dtos
{
	public class CosmosDbSettings
	{
		public string ConnectionString { get; set; }
		public string DatabaseName { get; set; }
		public string CollectionName { get; set; }
		public string PartitionKey { get; set; }

		public override string ToString()
		{
			return $"{nameof(ConnectionString)}: {ConnectionString}, {nameof(DatabaseName)}: {DatabaseName}, {nameof(CollectionName)}: {CollectionName}, {nameof(PartitionKey)}: {PartitionKey}";
		}
	}
}