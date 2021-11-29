using CommandLine;

namespace CalculateFunding.Migrations.Specification.Clone
{
    public class CloneOptions
    {
        [Option("src-spec-id", Required = true, HelpText = "Set the source specification id")]
        public string SourceSpecificationId { get; set; }

        [Option("trg-period-id", Required = true, HelpText = "Set the target period id. ie: AS-2223")]
        public string TargetPeriodId { get; set; }

        [Option("trg-funding-template-version", Required = false, HelpText = "Set the target funding template id. ie: 1.0", Default = "1.0")]
        public string TargetFundingTemplateVersion { get; set; }

        [Option("include-released-data-dataset", Required = false, HelpText = "Flat to include or exclude cloning released data datasets. Requires SpecificationMappingOptions option to be set if included", Default = false)]
        public bool? IncludeReleasedDataDateset { get; set; }
    }
}
