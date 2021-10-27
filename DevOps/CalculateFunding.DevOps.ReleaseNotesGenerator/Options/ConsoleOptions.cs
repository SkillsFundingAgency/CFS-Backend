using CommandLine;

namespace CalculateFunding.DevOps.ReleaseNotesGenerator.Options
{
    public class ConsoleOptions
    {
        [Option(
            "base-url", 
            Required = false, 
            HelpText = "Set the Base URL of ADO instance. ie: https://dev.azure.com/dfe-gov-uk",
            Default = "https://dev.azure.com/dfe-gov-uk")]
        public string BaseURL { get; set; }
        
        [Option(
            "project-name", 
            Required = false, 
            HelpText = "Set the Project Name. ie. Funding Transformation Programme",
            Default = "Funding Transformation Programme")]
        public string ProjectName { get; set; }

        [Option(
            "pat", 
            Required = true, 
            HelpText = "Set the Personal Access Token. Please ensure it is valid and has [WorkItems] Read, [Release] Read permissions, [Wiki] Read&Write - if wikiCreate option enabled")]
        public string PAT { get; set; }

        [Option(
            "source-release-phase", 
            Required = false, 
            HelpText = "Set the Source Release Phase Name. Value has to match given config on [friendlyName] field on [stageNames] section on appsettings.json ie. Test",
            Default = "Test")]
        public string SourceReleasePhase { get; set; }
        
        [Option(
            "destination-release-phase", 
            Required = false, 
            HelpText = "Set the Destination Release Phase Name. Value has to match given config on [friendlyName] field on [stageNames] section on appsettings.json ie. Production",
            Default = "Production")]
        public string DestinationReleasePhase { get; set; }

        [Option(
            "release-note-file-path", 
            Required = false, 
            HelpText = "Set the Release Note Path", 
            Default = "release-notes.md")]
        public string ReleaseNoteFilePath { get; set; }

        [Option(
            "create-wiki-page",
            Required = false,
            HelpText = "Option to create wiki page by ReleaseNotesGenerator",
            Default = true)]
        public bool CreateWikiPage { get; set; }

        [Option(
            "wiki-identifier",
            Required = false,
            HelpText = "Set the Wiki Identifier",
            Default = "Calculate-Funding-Service.wiki")]
        public string WikiIdentifier { get; set; }

        [Option(
            "wiki-path",
            Required = false,
            HelpText = "Set the new Wiki page path",
            Default = "/CFS Wiki/Release Notes Generator/sample-release-page")]
        public string WikiPath { get; set; }

        [Option(
            "app-insights-instrumentation-key",
            Required = false,
            HelpText = "Set the App Insights Instrumentation Key")]
        public string AppInsightsInstrumentationKey { get; set; }
    }
}
