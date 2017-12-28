
using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using System.Linq;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage;
using System;

namespace CalculateFunding.Functions.Datasets
{
    public static class SourceDatasets
    {
        [FunctionName("source-datasets")]
        public static async System.Threading.Tasks.Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)]HttpRequest req, TraceWriter log)
        {
            log.Info("C# HTTP trigger function processed a request.");

            var file = req.Form.Files.FirstOrDefault();

            if (file == null) return new BadRequestResult();

            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(
                Environment.GetEnvironmentVariable("DatasetStorage"));
            var blobClient = storageAccount.CreateCloudBlobClient();
            
            var container = blobClient.GetContainerReference("source-datasets");
            await container.CreateIfNotExistsAsync();

            var blockBlob = container.GetBlockBlobReference(file.FileName);
           
            // Create or overwrite the "myblob" blob with contents from a local file.
            using (var fileStream = file.OpenReadStream())
            {
                await blockBlob.UploadFromStreamAsync(fileStream);
            }

            return new AcceptedResult();
        }
    }
}
