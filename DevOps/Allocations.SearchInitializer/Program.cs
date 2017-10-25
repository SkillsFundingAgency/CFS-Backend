using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Allocations.Search;
using CommandLine;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;
using Microsoft.Spatial;
using Newtonsoft.Json;

namespace Allocations.SearchInitializer
{
    class Options
    {
        [Option("documentDbConnectionString", HelpText = @"Azure Document DB connection string")]
        public string DocumentDbConnectionString { get; set; }

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
           // Logger.Info($"Started with {args}");
            try
            {
                Task.Run(async () =>
                {


                    //var documentDatabaseInitializer = new DocumentDatabaseInitializer(result.Value.DocumentDbEndpoint, result.Value.DocumentDbPrimaryKey, result.Value.DocumentDbDatabaseName, result.Value.DocumentDbCollectionName);
                    //await documentDatabaseInitializer.EnsureInitialisedAsync(entityAssembly);

                    var searchInitializer = new Search.SearchInitializer(result.Value.SearchServiceName, result.Value.SearchPrimaryKey, result.Value.DocumentDbConnectionString);
                    await searchInitializer.Initialise(typeof(ProductTestScenarioResultIndex));

                   // await UploadDataAsync(result.Value.DocumentDbEndpoint, result.Value.DocumentDbPrimaryKey, result.Value.DocumentDbDatabaseName, result.Value.DocumentDbCollectionName);


                }).Wait();
              //  Logger.Info("Completed successfully");
                return 0;
            }
            catch (Exception e)
            {
             //   Logger.Error(e);
                Console.WriteLine(e.Message);
            }
            return -1;
        }
    }



}
