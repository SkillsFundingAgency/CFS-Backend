using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Migrations.Calculations.Etl.Migrations;
using CommandLine;

namespace CalculateFunding.Migrations.Calculations.Etl
{
    internal class Program
    {
        private static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<MigrateOptions>(args)
                .MapResult(
                    RunEtl,
                    errs => Task.FromResult(1));
        }

        private static async Task RunEtl(MigrateOptions options)
        {
            Guard.ArgumentNotNull(options, "You must supply \"migrate\" settings to migration calculations between two specifications");

            IServiceProvider sourceServices = ConfigureApis(new ApiOptions
            {
                CalculationsApiKey = options.SourceCalculationsApiKey,
                CalculationsApiUri = options.SourceCalculationsApiUri,
                DataSetsApiKey = options.SourceDataSetsApiKey,
                DataSetsApiUri = options.SourceDataSetsApiUri,
                SpecificationsApiKey = options.SourceSpecificationsApiKey,
                SpecificationsApiUri = options.SourceSpecificationsApiUri
            });

            IServiceProvider destinationServices = ConfigureApis(new ApiOptions
            {
                CalculationsApiKey = options.DestinationCalculationsApiKey,
                CalculationsApiUri = options.DestinationCalculationsApiUri,
                DataSetsApiKey = options.DestinationDataSetsApiKey,
                DataSetsApiUri = options.DestinationDataSetsApiUri,
                SpecificationsApiKey = options.DestinationSpecificationsApiKey,
                SpecificationsApiUri = options.DestinationSpecificationsApiUri
            });

            CalculationsMigration migration = new CalculationsMigration(sourceServices, destinationServices, options.PreventWrites);

            await migration.Run(options.SourceSpecificationId, options.DestinationSpecificationId);
        }

        private static IServiceProvider ConfigureApis(IApiOptions options)
        {
            return BootStrapper.BuildServiceProvider(options);
        }
    }
}