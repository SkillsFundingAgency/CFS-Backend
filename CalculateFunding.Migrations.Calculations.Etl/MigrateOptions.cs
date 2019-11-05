using CommandLine;

namespace CalculateFunding.Migrations.Calculations.Etl
{
    public class MigrateOptions
    {
        [Option("src-calcs-uri", Required = true, HelpText = "Set the source calculations api uri")]
        public string SourceCalculationsApiUri { get; set; }
        
        [Option("src-calcs-key", Required = true, HelpText = "Set the source calculations api key")]
        public string SourceCalculationsApiKey { get; set; }
        
        [Option("src-specs-key", Required = true, HelpText = "Set the source specifications api key")]
        public string SourceSpecificationsApiKey { get; set; }
        
        [Option("src-specs-uri", Required = true, HelpText = "Set the source specifications api uri")]
        public string SourceSpecificationsApiUri { get; set; }
        
        [Option("src-data-sets-key", Required = true, HelpText = "Set the source data sets api key")]
        public string SourceDataSetsApiKey { get; set; }
        
        [Option("src-data-sets-uri", Required = true, HelpText = "Set the source data sets api uri")]
        public string SourceDataSetsApiUri { get; set; }
        
        [Option("dest-calcs-uri", Required = true, HelpText = "Set the destination calculations api uri")]
        public string DestinationCalculationsApiUri { get; set; }
        
        [Option("dest-calcs-key", Required = true, HelpText = "Set the destination calculations api key")]
        public string DestinationCalculationsApiKey { get; set; }
        
        [Option("dest-specs-key", Required = true, HelpText = "Set the destination specifications api key")]
        public string DestinationSpecificationsApiKey { get; set; }
        
        [Option("dest-specs-uri", Required = true, HelpText = "Set the destination specifications api uri")]
        public string DestinationSpecificationsApiUri { get; set; }
        
        [Option("dest-data-sets-key", Required = true, HelpText = "Set the destination data sets api key")]
        public string DestinationDataSetsApiKey { get; set; }
        
        [Option("dest-data-sets-uri", Required = true, HelpText = "Set the destination data sets api uri")]
        public string DestinationDataSetsApiUri { get; set; }
        
        [Option("src-spec-id", Required = true, HelpText = "Set the source specification id")]
        public string SourceSpecificationId { get; set; }
        
        [Option("dest-spec-id", Required = true, HelpText = "Set the destination specification id")]
        public string DestinationSpecificationId { get; set; }
        
        [Option("prevent-writes", Required = false, HelpText = "Set flag indicating whether to skip writes to the destination specification")]
        public bool PreventWrites { get; set; }
    }
}