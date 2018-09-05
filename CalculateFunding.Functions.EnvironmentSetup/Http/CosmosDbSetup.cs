using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using CalculateFunding.Repositories.Common.Cosmos;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CalculateFunding.Functions.EnvironmentSetup.Http
{
    public static class CosmosDbSetup
    {
        [FunctionName("cosmos-db-setup")]
        public static async Task<IActionResult> Run(
           [HttpTrigger(AuthorizationLevel.Function, "get")] HttpRequest req, ILogger log)
        {
            IConfigurationBuilder builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", true)
                .AddJsonFile("local.settings.json", optional: true)
                .AddEnvironmentVariables();

            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                // Add user secrets for CalculateFunding.Functions.LocalDebugProxy
                builder.AddUserSecrets("df0d69d5-a6db-4598-909f-262fc39cb8c8");
            }

            IConfiguration configuration = builder.Build();

            if (log == null)
            {
                Console.WriteLine("ILogger is null");
                return new StatusCodeResult(500);
            }

            string cosmosDbConnectionString = configuration.GetValue<string>("CosmosDbSettings:ConnectionString");
            if (string.IsNullOrWhiteSpace(cosmosDbConnectionString))
            {
                log.LogCritical("CosmosDB Connection String is not set at CosmosDbSettings:ConnectionString");
                return new StatusCodeResult(500);
            }

            DocumentClient client = DocumentDbConnectionString.Parse(cosmosDbConnectionString);

            await CreateDatabaseAndCollections(client, "calculate-funding", new[] { "specs", "calcs", "tests", "datasets" }, log);

            // Create unlimited sized collection for providersourcedatasets
            await CreateDatabaseAndCollection(client, "calculate-funding",
                new DocumentCollection()
                {
                    Id = "providerdatasets",
                    PartitionKey = new PartitionKeyDefinition()
                    {
                        Paths = new Collection<string>() { "/content/providerId" }
                    },
                },
                new RequestOptions()
                {
                    // 1100 to get over 1000, you get up to 50000RU, rather than 10000 when created with 1000
                    OfferThroughput = 1100
                }
                , log);

            // Create unlimited sized collection for calculationresults
            await CreateDatabaseAndCollection(client, "calculate-funding",
                new DocumentCollection()
                {
                    Id = "calculationresults",
                    PartitionKey = new PartitionKeyDefinition()
                    {
                        Paths = new Collection<string>() { "/content/provider/id" }
                    },
                },
                new RequestOptions()
                {
                    // 1100 to get over 1000, you get up to 50000RU, rather than 10000 when created with 1000
                    OfferThroughput = 1100
                }
                , log);

            // Create unlimited sized collection for calculationresults
            await CreateDatabaseAndCollection(client, "calculate-funding",
                new DocumentCollection()
                {
                    Id = "testresults",
                    PartitionKey = new PartitionKeyDefinition()
                    {
                        Paths = new Collection<string>() { "/content/provider/id" }
                    },
                },
                new RequestOptions()
                {
                    // 1100 to get over 1000, you get up to 50000RU, rather than 10000 when created with 1000
                    OfferThroughput = 1100
                }
                , log);

            // Create unlimited sized collection for published provider results
            await CreateDatabaseAndCollection(client, "calculate-funding",
                new DocumentCollection()
                {
                    Id = "publishedproviderresults",
                    PartitionKey = new PartitionKeyDefinition()
                    {
                        Paths = new Collection<string>() { "/content/providerId" }
                    },
                },
                new RequestOptions()
                {
                    // 1100 to get over 1000, you get up to 50000RU, rather than 10000 when created with 1000
                    OfferThroughput = 1100
                }
                , log);

            // Create unlimited sized collection for published provider calc results
            await CreateDatabaseAndCollection(client, "calculate-funding",
                new DocumentCollection()
                {
                    Id = "publishedprovidercalcresults",
                    PartitionKey = new PartitionKeyDefinition()
                    {
                        Paths = new Collection<string>() { "/content/providerId" }
                    },
                },
                new RequestOptions()
                {
                    // 1100 to get over 1000, you get up to 50000RU, rather than 10000 when created with 1000
                    OfferThroughput = 1100
                }
                , log);

            // Create unlimited sized collection for users
            await CreateDatabaseAndCollection(client, "calculate-funding",
                new DocumentCollection()
                {
                    Id = "users"
                },
                new RequestOptions()
                {
                    // 1100 to get over 1000, you get up to 50000RU, rather than 10000 when created with 1000
                    OfferThroughput = 1100
                }
                , log);

            return new OkResult();
        }

        private static async Task CreateDatabaseAndCollections(DocumentClient client, string databaseName, string[] collectionNames, ILogger log)
        {
            Database databaes = new Database()
            {
                Id = databaseName
            };

            log.LogInformation($"Ensuring database exists: '{databaseName}");
            ResourceResponse<Database> createdDatabase = await client.CreateDatabaseIfNotExistsAsync(databaes);

            Uri databaseUri = UriFactory.CreateDatabaseUri(databaes.Id);

            foreach (string collectionName in collectionNames)
            {
                log.LogInformation($"Ensuring Collection Exists: {collectionName}");
                DocumentCollection documentCollection = new DocumentCollection()
                {
                    Id = collectionName,
                };

                await client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, documentCollection);
            }
        }

        private static async Task CreateDatabaseAndCollection(DocumentClient client, string databaseName, DocumentCollection documentCollection, RequestOptions requestOptions, ILogger log)
        {
            Database databaes = new Database()
            {
                Id = databaseName
            };

            log.LogInformation($"Ensuring database exists: '{databaseName}");
            ResourceResponse<Database> createdDatabase = await client.CreateDatabaseIfNotExistsAsync(databaes);

            Uri databaseUri = UriFactory.CreateDatabaseUri(databaes.Id);


            log.LogInformation($"Ensuring Collection Exists: {documentCollection.Id}");

            await client.CreateDocumentCollectionIfNotExistsAsync(databaseUri, documentCollection, requestOptions);

        }
    }
}
