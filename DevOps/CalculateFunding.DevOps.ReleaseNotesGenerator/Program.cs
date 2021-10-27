using CalculateFunding.DevOps.ReleaseNotesGenerator.Options;
using CommandLine;
using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.DevOps.ReleaseNotesGenerator.Generators;

namespace CalculateFunding.DevOps.ReleaseNotesGenerator
{
    internal class Program
    {
        private static async Task Main(string[] args)
            => await Parser.Default.ParseArguments<ConsoleOptions>(args)
                .MapResult(
                    RunGenerateReleaseNotes,
                    errs => Task.FromResult(1));

        private static async Task RunGenerateReleaseNotes(ConsoleOptions consoleOptions)
        {
            Console.WriteLine("Starting GenerateReleaseNotes ...");

            Guard.ArgumentNotNull(consoleOptions, "You must supply \"migrate\" settings to generate release notes with work items between two environments");

            IServiceProvider serviceProvider = BootStrapper.BuildServiceProvider(consoleOptions);
            INotesGenerator notesGenerator = 
                (INotesGenerator)serviceProvider.GetService(typeof(INotesGenerator));

            await notesGenerator.Generate(consoleOptions);

            Console.WriteLine("Completed GenerateReleaseNotes.");
        }
    }
}
