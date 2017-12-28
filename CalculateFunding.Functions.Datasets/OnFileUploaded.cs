using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Functions.Common;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Repositories.Providers;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;

namespace CalculateFunding.Functions.Datasets
{
    public static class OnBlobUpdated
    {
        [FunctionName("on-blob-updated")]
        public static async Task RunAsync([BlobTrigger("edubase/{name}", Connection = "ProvidersStorage")]Stream blob, string name, TraceWriter log)
        {
            var repository = ServiceFactory.GetService<CosmosRepository>();
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var importer = new EdubaseImporterService();
            using (var reader = new StreamReader(blob))
            {
                var providers = importer.ImportEdubaseCsv(name, reader);

                await repository.BulkCreateAsync(providers.ToList(), 100);

            }

            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {blob.Length} Bytes");

        }
    }
}
