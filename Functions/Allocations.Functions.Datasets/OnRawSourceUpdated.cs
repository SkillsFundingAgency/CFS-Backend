using System.IO;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;

namespace Allocations.Functions.Datasets
{
    public static class OnRawSourceUpdated
    {
        [FunctionName("OnRawSourceUpdated")]
        public static void Run([BlobTrigger("raw-datasets/{name}", Connection = "DatasetStorage")]Stream myBlob, string name, TraceWriter log)
        {
            log.Info($"C# Blob trigger function Processed blob\n Name:{name} \n Size: {myBlob.Length} Bytes");
        }
    }
}
