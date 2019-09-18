using CommandLine;

namespace CalculateFunding.Generators.NavFeed.Options
{
    public class FeedOptions
    {
        [Option('v', "file-version", Required = true, HelpText = "Set file version")]
        public int FileVersion { get; set; }

        [Option('i', "input-path", Required = true, HelpText = "Set local input file path")]
        public string InputFilePath { get; set; }

        [Option('o', "output-path", Required = true, HelpText = "Set local output folder path")]
        public string OutputFolderPath { get; set; }

        [Option('s', "store", Required = true, HelpText = "Set feed store type")]
        public FeedStorageType FeedStorageType { get; set; }

        [Option('p', "provider-version", Required = true, HelpText = "Set provider version to search the providers. Sample value is 1")]
        public string ProviderVersion { get; set; }
    }
}
