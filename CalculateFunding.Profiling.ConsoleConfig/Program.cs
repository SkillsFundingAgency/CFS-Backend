namespace CalculateFunding.Profiling.ConsoleConfig
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;
    using Dtos;
    using Extensions;
    using Microsoft.Extensions.Configuration;
    using Newtonsoft.Json;
    using Persistence;

    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Initialising database and collection...");

            IConfiguration config = InitializeConfigBuilder();

            CosmosDbSettings cosmosDbSettings = BindCosmosVariablesToDto(config);

            Console.WriteLine(cosmosDbSettings.ToString());

            ProfilePatternRepository.InitialiseDocumentClient(cosmosDbSettings);

            Task.WaitAll(ProfilePatternRepository.CreateDatabaseAndCollectionIfNotExists());

            Console.WriteLine("Database and collection initialised successfully");

            // load in profile patterns from resources
            Task.Run(async () =>
            {
                IReadOnlyCollection<FundingStreamPeriodProfilePatternDocument> documents =
                    Assembly.GetExecutingAssembly().GetManifestResourceNames()
                        .Where(rn => rn.EndsWith(".json"))
                        .Select(rn => Assembly.GetExecutingAssembly().GetManifestResourceStream(rn))
                        .Select(ConvertToModel)
                        .ToList();

                await ProfilePatternRepository.RemoveWhereNoIdMatch(documents.Select(d => d.id));

                await documents.ForEachAsync(ProfilePatternRepository.UpsertPattern);
            })
                .GetAwaiter().GetResult();

            Console.WriteLine("Finished writing all profiling patterns to database");
        }

        private static CosmosDbSettings BindCosmosVariablesToDto(IConfiguration config)
        {
            CosmosDbSettings cosmosDbSettings = new CosmosDbSettings();

            config.Bind("CosmosDbSettings", cosmosDbSettings);
            return cosmosDbSettings;
        }

        private static IConfiguration InitializeConfigBuilder()
        {
            IConfiguration config = new ConfigurationBuilder()
                .AddEnvironmentVariables()
                .Build();
            return config;
        }

        private static FundingStreamPeriodProfilePatternDocument ConvertToModel(Stream fileStream)
        {
            using (var sr = new StreamReader(fileStream))
            {
                FundingStreamPeriodProfilePattern pattern =
                    JsonConvert.DeserializeObject<FundingStreamPeriodProfilePattern>(sr.ReadToEnd());

                return FundingStreamPeriodProfilePatternDocument.CreateFromPattern(pattern);
            }
        }

        private static string GetArg(string[] args, int index)
        {
            if (args.Length > index)
            {
                return args[index];
            }

            return null;
        }
    }
}
