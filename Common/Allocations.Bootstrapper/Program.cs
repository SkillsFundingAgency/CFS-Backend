using System;
using System.Threading.Tasks;
using Allocations.Models.Results;
using Allocations.Repository;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CommandLine;

namespace Allocations.Boostrapper
{
    class Options
    {
        [Option("cosmosDBConnectionString", Required = true, HelpText = @"Azure Document DB connection string")]
        public string CosmosDBConnectionString { get; set; }

        [Option("searchServiceName", Required = true, HelpText = "Azure search service name (just the name, not the full endpoint)")]
        public string SearchServiceName { get; set; }

        [Option("searchPrimaryKey", Required = true, HelpText = "Azure search service primary key")]
        public string SearchPrimaryKey { get; set; }
    }
    class Program
    {
        static int Main(string[] args)
        { 
            
            var result = Parser.Default.ParseArguments<Options>(args);
            Console.WriteLine($"Started with {args}");
            try
            {
                 Task.Run(async () =>
                {
                    try
                    {
                        var searchInitializer = new SearchInitializer(result.Value.SearchServiceName,
                            result.Value.SearchPrimaryKey, result.Value.CosmosDBConnectionString);

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
                        using (var repo = new Repository<Budget>("specs", result.Value.CosmosDBConnectionString))
                        {
                            await repo.CreateAsync(SeedData.CreateGeneralAnnualGrant());
                        }
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
            return -1;
            /*
             */

        }



    }
}
