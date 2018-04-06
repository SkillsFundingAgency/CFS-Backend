//using System;
//using System.Threading.Tasks;
//using CalculateFunding.Services.Datasets.Interfaces;
//using Microsoft.Azure.WebJobs;
//using Microsoft.Azure.WebJobs.Host;
//using Microsoft.Extensions.DependencyInjection;
//using Microsoft.WindowsAzure.Storage.Blob;
//using Serilog;

//namespace CalculateFunding.Functions.Datasets.Storage
//{
//    public static class DatasetBlobTriggers
//    {
//        [FunctionName("dataset-blob")]
//        async public static Task Run([BlobTrigger("datasets/{name}", Connection = "AzureStorageSettings:ConnectionString")]ICloudBlob blob, string name, TraceWriter log)
//        {
//            using (var scope = IocConfig.Build().CreateScope())
//            {
//                IDatasetService datasetService = scope.ServiceProvider.GetService<IDatasetService>();
//                ILogger logger = scope.ServiceProvider.GetService<ILogger>();

//                try
//                {
//                    logger.Information($"Starting to process blob: {name}");

//                    //await datasetService.SaveNewDataset(blob);

//                    logger.Information($"Completed processing of blob: {name}");
//                }
//                catch (Exception exception)
//                {
//                    logger.Error(exception, $"An error occcurred whilst processing blob: {name}");
//                }
//            }
//        }
//    }
//}
