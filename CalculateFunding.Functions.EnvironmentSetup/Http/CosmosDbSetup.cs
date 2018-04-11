using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
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

            // Create unlimited sized collection for providersources
            await CreateDatabaseAndCollection(client, "calculate-funding",
                new DocumentCollection()
                {
                    Id = "providersources",
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


            try
            {
                await CreateStoredProcedures(client, "calculate-funding");
            }
            catch (Exception ex)
            {
                log.LogCritical($"Failed to create stored procedures with message: {ex.Message}");
                return new StatusCodeResult(500);
            }

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

        private static async Task CreateStoredProcedures(DocumentClient client, string databaseName)
        {
            var spDefinition = new StoredProcedure
            {
                Id = "usp_update_provider_results",
                Body = GetUpdateProviderResultsStoredProcedure()
            };

            try
            {
                await client.ReadStoredProcedureAsync(UriFactory.CreateStoredProcedureUri(databaseName, "results", "usp_update_provider_results"));
            }
            catch (DocumentClientException dex)
            {
                if (dex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    StoredProcedure sproc = await client.CreateStoredProcedureAsync(UriFactory.CreateDocumentCollectionUri(databaseName, "results"), spDefinition);
                }
                else
                {
                    throw;
                }
            }
        }

        private static string GetUpdateProviderResultsStoredProcedure()
        {
            var sb = new StringBuilder();
            sb.AppendLine(@"function bulkImport(docs) {");
            sb.AppendLine(@"    var collection = getContext().getCollection();");
            sb.AppendLine(@"    var collectionLink = collection.getSelfLink();");
            sb.AppendLine(@"");
            sb.AppendLine(@"    // The count of imported docs, also used as current doc index.");
            sb.AppendLine(@"    var count = 0;");
            sb.AppendLine(@"");
            sb.AppendLine(@"    // Validate input.");
            sb.AppendLine(@"    if (!docs) throw new Error(""The array is undefined or null."");");
            sb.AppendLine(@"");
            sb.AppendLine(@"    var docsLength = docs.length;");
            sb.AppendLine(@"    if (docsLength == 0) {");
            sb.AppendLine(@"        getContext().getResponse().setBody(0);");
            sb.AppendLine(@"    }");
            sb.AppendLine(@"");
            sb.AppendLine(@"    // Call the CRUD API to create a document.");
            sb.AppendLine(@"    tryUpdate(docs[count], callback);");
            sb.AppendLine(@"");
            sb.AppendLine(@"   ");
            sb.AppendLine(@"    function tryUpdate(doc, callback) {");
            sb.AppendLine(@"        var options = {");
            sb.AppendLine(@"            disableAutomaticIdGeneration: true");
            sb.AppendLine(@"        };");
            sb.AppendLine(@"       var isAccepted = true;");
            sb.AppendLine(@"        var isFound = collection.queryDocuments(collectionLink, 'SELECT * FROM root r WHERE r.id = ""' + doc.id + '""', function (err, feed, options) {");
            sb.AppendLine(@"            if (err) throw err;");
            sb.AppendLine(@"            if (!feed || !feed.length) {");
            sb.AppendLine(@"                isAccepted = collection.createDocument(collectionLink, doc, callback);");
            sb.AppendLine(@"            }");
            sb.AppendLine(@"            else {");
            sb.AppendLine(@"                // The metadata document.");
            sb.AppendLine(@"                var existingDoc = feed[0];");
            sb.AppendLine(@"                isAccepted = collection.replaceDocument(existingDoc._self, doc, callback);");
            sb.AppendLine(@"            }");
            sb.AppendLine(@"             if (!isAccepted) getContext().getResponse().setBody(count);");
            sb.AppendLine(@"        });");
            sb.AppendLine(@"    }");
            sb.AppendLine(@"");
            sb.AppendLine(@"    // This is called when collection.createDocument is done and the document has been persisted.");
            sb.AppendLine(@"    function callback(err, doc, options) {");
            sb.AppendLine(@"        if (err) throw err;");
            sb.AppendLine(@"");
            sb.AppendLine(@"        // One more document has been inserted, increment the count.");
            sb.AppendLine(@"        count++;");
            sb.AppendLine(@"");
            sb.AppendLine(@"        if (count >= docsLength) {");
            sb.AppendLine(@"            // If we have created all documents, we are done. Just set the response.");
            sb.AppendLine(@"            getContext().getResponse().setBody(count);");
            sb.AppendLine(@"        } else {");
            sb.AppendLine(@"            // Create next document.");
            sb.AppendLine(@"            tryUpdate(docs[count], callback);");
            sb.AppendLine(@"        }");
            sb.AppendLine(@"    }");
            sb.AppendLine(@"}");

            return sb.ToString();
        }
    }
}
