using CommandLine;

namespace CalculateFunding.Migrations.Specifications.Etl
{
    public class MigrateOptions
    {
        [Option("search-name", Required = true, HelpText = "Set the search name")]
        public string SearchName { get; set; }

        [Option("search-key", Required = true, HelpText = "Set the search key")]
        public string SearchKey { get; set; }

        [Option("src-storage-account-name", Required = true, HelpText = "Set the source storage account name")]
        public string SourceStorageAccountName { get; set; }

        [Option("src-storage-account-key", Required = true, HelpText = "Set the source storage account key")]
        public string SourceStorageAccountKey { get; set; }

        [Option("src-spec-id", Required = true, HelpText = "Set the source specification id")]
        public string SourceSpecificationId { get; set; }

        [Option("unpublish-spec-id", Required = false, HelpText = "Set the specification id to un-publish")]
        public string UnpublishSpecificationId { get; set; }

        [Option("src-account-endpoint", Required = true, HelpText = "Set the source account endpoint")]
        public string SourceAccountEndpoint { get; set; }
        
        [Option("src-account-key", Required = true, HelpText = "Set the source account key")]
        public string SourceAccountKey { get; set; }
        
        [Option("src-database", Required = true, HelpText = "Set the source database")]
        public string SourceDatabase { get; set; }

        [Option("dst-storage-account-name", Required = true, HelpText = "Set the destination storage account name")]
        public string TargetStorageAccountName { get; set; }

        [Option("dst-storage-account-key", Required = true, HelpText = "Set the destination storage account key")]
        public string TargetStorageAccountKey { get; set; }

        [Option("dst-account-endpoint", Required = true, HelpText = "Set the destination account endpoint")]
        public string TargetAccountEndpoint { get; set; }

        [Option("dst-account-key", Required = true, HelpText = "Set the destination account key")]
        public string TargetAccountKey { get; set; }

        [Option("dst-database", Required = true, HelpText = "Set the destination database")]
        public string TargetDatabase { get; set; }

        [Option("maxthroughput", Required = true, HelpText = "Set the maximum throughput of the collection")]
        public int MaxThroughPut { get; set; }
    }
}