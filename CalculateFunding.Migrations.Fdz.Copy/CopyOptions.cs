using CommandLine;

namespace CalculateFunding.Migrations.Fdz.Copy
{
    internal class CopyOptions
    {
        [Option("src-conn", Required = true, HelpText = "Set the source FDZ connection string")]
        public string SourceConnectionString { get; set; }

        [Option("src-snapshot-id", Required = true, HelpText = "Provider snapshot id to copy")]
        public int SourceSnapshotId { get; set; }

        [Option("trg-conn", Required = true, HelpText = "Set the target FDZ connection string")]
        public string TargetConnectionString { get; set; }

        [Option("trg-snapshot-name", Required = false, HelpText = "Name of snapshot in target environment, leave blank to use source name")]
        public string TargetSnapshotName { get; set; }

        [Option("trg-snapshot-date", Required = false, HelpText = "Target date of snapshot in target environment, leave blank to use source date")]
        public DateTime? TargetSnapshotDate { get; set; }

        [Option("trg-snapshot-desc", Required = false, HelpText = "Description of snapshot in target environment, leave blank to use source description")]
        public string TargetSnapshotDescription { get; set; }

        [Option("trg-snapshot-ver", Required = false, HelpText = "Version number of snapshot in target environment, leave blank to use source version")]
        public int TargetSnapshotVersion { get; set; }

        [Option("trg-snapshot-funding-stream-id", Required = false, HelpText = "Funding stream id of snapshot in target environment, leave blank to use source id")]
        public string TargetSnapshotFundingStreamId { get; set; }
    }
}
