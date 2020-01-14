using CalculateFunding.Common.CosmosDb;
using CalculateFunding.Common.Utility;
using CalculateFunding.Migrations.Specification.Etl.Migrations;
using CalculateFunding.Migrations.Specifications.Etl.Migrations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CommandLine;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Migrations.Specifications.Etl
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
            Guard.ArgumentNotNull(options, "You must supply \"migrate\" settings to migrate a specification between two environments");

            CosmosDb sourceCosmosDb = new CosmosDb(options.SourceAccountKey,
                    options.SourceAccountEndpoint,
                    options.SourceDatabase
                );

            CosmosDb destCosmosDb = new CosmosDb(options.TargetAccountKey,
                        options.TargetAccountEndpoint,
                        options.TargetDatabase
                    );

            List<Container> containers = new List<Container>();

            containers.Add(new Container { Name = "publishedfunding", Query = "SELECT * FROM c WHERE c.content.current.specificationId = '{0}' OR c.content.specificationId = '{0}'", MaxThroughPut = options.MaxThroughPut, PartitionKey = "/content/partitionKey", HasPost = true });
            containers.Add(new Container { Name = "specs", Query = "SELECT * FROM c WHERE c.content.current.specificationId = '{0}' OR c.content.specificationId = '{0}'", HasPreReqs = true, MaxThroughPut = options.MaxThroughPut, HasPost = true });
            containers.Add(new Container { Name = "datasets", Query = "SELECT * FROM c WHERE c.content.Specification.id = '{0}'", HasPreReqs = true, MaxThroughPut = options.MaxThroughPut, HasPost = true });
            containers.Add(new Container { Name = "calculationresults", Query = "SELECT * FROM c WHERE c.content.specificationId = '{0}'", MaxThroughPut = options.MaxThroughPut, PartitionKey = "/content/provider/id", HasPost = true });
            containers.Add(new Container { Name = "calcs", Query = "SELECT * FROM c WHERE c.content.specificationId = '{0}'", MaxThroughPut = options.MaxThroughPut, HasPost = true });
            containers.Add(new Container { Name = "providerdatasets", Query = "SELECT * FROM c WHERE c.content.specificationId = '{0}'", MaxThroughPut = options.MaxThroughPut, PartitionKey = "/content/providerId" });

            List<(BlobContainer sourceContainer, BlobContainer destinationContainer)> blobs = new List<(BlobContainer sourceContainer, BlobContainer destinationContainer)>();

            blobs.Add((new BlobContainer { Name = "source", AccountName = options.SourceStorageAccountName, AccountKey = options.SourceStorageAccountKey, Blobs = new string[] { $"{options.SourceSpecificationId}/{options.SourceSpecificationId}-preview.zip", $"{options.SourceSpecificationId}/{options.SourceSpecificationId}-release.zip", $"{options.SourceSpecificationId}/implementation.dll" } }, new BlobContainer { Name = "source", AccountName = options.TargetStorageAccountName, AccountKey = options.TargetStorageAccountKey }));
            blobs.Add((new BlobContainer { Name = "datasets", AccountName = options.SourceStorageAccountName, AccountKey = options.SourceStorageAccountKey}, new BlobContainer { Name = "datasets", AccountName = options.TargetStorageAccountName, AccountKey = options.TargetStorageAccountKey }));

            SpecificationMigration specificationMigration = new SpecificationMigration(
                    sourceCosmosDb,
                    destCosmosDb,
                    containers,
                    blobs,
                    new Search(options.SearchKey, options.SearchName),
                    "specs",
                    @"SELECT * FROM c
                                        WHERE c.documentType = 'Specification'
                                        AND c.id = @specificationId"
                );

            await specificationMigration.Run(options.SourceSpecificationId, options.UnpublishSpecificationId);
        }
    }
}
