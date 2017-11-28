using System.IO;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Allocations.Services.DataImporter;

namespace Allocations.Functions.Datasets
{
    public static class OnRawSourceUpdated
    {
        [FunctionName("on-raw-source-updated")]
        public static async Task Run([BlobTrigger("raw-datasets/{name}", Connection = "DatasetStorage")]Stream blob, string name, TraceWriter log)
        {
            
            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {blob.Length} Bytes");
            var importer = new DataImporterService();
            await importer.GetSourceDataAsync(name, blob, "budget-gag1718");
        }
    }
}
