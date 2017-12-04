using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Bootstrapper;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Allocations.Boostrapper
{
    class Program
    {
        public static IConfigurationRoot Configuration { get; set; }
        static int Main(string[] args)
        {
            var dict = new Dictionary<string, string>
            {
                {"Profile:MachineName", "MairaPC"},
                {"App:MainWindow:Left", "1980"}
            };

            var builder = new ConfigurationBuilder();

            builder.AddInMemoryCollection(dict)
                .AddCommandLine(args)
                .AddJsonFile("appsettings.json");

            Configuration = builder.Build();


            var cosmosDbConnectionString = Configuration["cosmosDbConnectionString"];
            var searchServiceName = Configuration["searchServiceName"];
            var searchPrimaryKey = Configuration["searchPrimaryKey"];

            bool isValid = true;
            if (string.IsNullOrWhiteSpace(cosmosDbConnectionString))
            {
                Console.WriteLine("cosmosDbConnectionString must be provided");
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(searchServiceName))
            {
                Console.WriteLine("searchServiceName must be specified");
                isValid = false;
            }
            if (string.IsNullOrWhiteSpace(searchPrimaryKey))
            {
                Console.WriteLine("searchPrimaryKey must be specified");
                isValid = false;
            }

            if (isValid)
            {
                Console.WriteLine($"Started with {args}");
                try
                {
                    Task.Run(async () =>
                    {
                        try
                        {
                            var searchInitializer = new SearchInitializer(searchServiceName,
                                searchPrimaryKey, cosmosDbConnectionString);

                            await searchInitializer.Initialise(typeof(ProductTestScenarioResultIndex));
                            await searchInitializer.Initialise(typeof(ProviderResultIndex));
                        }
                        catch (Exception e)
                        {

                            Console.WriteLine(e);
                            throw;
                        }

                        Console.WriteLine("Seed budget");
                        try
                        {
                            var settings = new RepositorySettings
                            {
                                ConnectionString = cosmosDbConnectionString,
                                DatabaseName = "calculate-funding",
                                CollectionName = "specs"
                            };

                            var logger = new LoggerFactory()
                                .AddConsole()
                                .CreateLogger("Bootstrapper");
                            var repo = new Repository<Budget>(settings, logger);
                            
                            await repo.CreateAsync(SeedData.CreateGeneralAnnualGrant());
                            
                        }
                        catch (Exception e)
                        {
                            Console.Write(e.ToString());
                        }

                    }).Wait();
                    Console.WriteLine("Completed successfully");
                    return 0;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return -1;
            /*
             */

        }


    }
}
