using CommandLine;

namespace CalculateFunding.Migrations.DSG.RollBack
{
    public class MigrateOptions
    {
        [Option("collection-name", 
            Required = false, 
            HelpText = "Set the target collection name for the published funding documents")]
        public string CollectionName { get; set; }
        
        [Option("document-version",
            Required = true,
            HelpText = "Roll back all published funding documents at this or later document versions")]
        public string DocumentVersion { get; set; }
        
        [Option("funding-period",
            Required = true,
            HelpText = "The funding period to restrict the roll back to")]
        public string FundingPeriod { get; set; }
    }
}