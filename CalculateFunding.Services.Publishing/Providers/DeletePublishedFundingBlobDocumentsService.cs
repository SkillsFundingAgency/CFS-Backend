using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.Providers
{
    public class DeletePublishedFundingBlobDocumentsService : IDeletePublishedFundingBlobDocumentsService
    {
        private readonly Policy _blobClientPolicy;
        private readonly IBlobClient _blobClient;
        private readonly ILogger _logger;

        public DeletePublishedFundingBlobDocumentsService(IPublishingResiliencePolicies resiliencePolicies, 
            IBlobClient blobClient, 
            ILogger logger)
        {
            Guard.ArgumentNotNull(resiliencePolicies?.BlobClient, "resiliencePolicies.BlobClient");
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            
            _blobClientPolicy = resiliencePolicies.BlobClient;
            _blobClient = blobClient;
            _logger = logger;
        }

        public async Task DeletePublishedFundingBlobDocuments(string fundingStreamId, string fundingPeriodId, string containerName)
        {
            _logger.Information($"Deleting {containerName} documents from blob storage for {fundingStreamId} {fundingPeriodId}");

            List<string> blobsToDelete = new List<string>();

            string fileStart = $"{fundingStreamId}-{fundingPeriodId}".ToLower();

            await _blobClientPolicy.ExecuteAsync(() =>
                _blobClient.BatchProcessBlobs(blobs =>
                {
                    blobsToDelete.AddRange(blobs.Select(_ => _.Uri.GetComponents(UriComponents.Path, UriFormat.Unescaped))
                        .Where(_ => _.ToLower().StartsWith(fileStart)));

                    return Task.CompletedTask;
                }, containerName));

            SemaphoreSlim throttle = new SemaphoreSlim(10);
            List<Task> deleteTasks = new List<Task>();

            foreach (string blobName in blobsToDelete)
            {
                await throttle.WaitAsync();

                deleteTasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        ICloudBlob cloudBlob = await _blobClientPolicy.ExecuteAsync(() =>
                            _blobClient.GetBlobReferenceFromServerAsync(blobName, containerName));

                        await cloudBlob.DeleteAsync();
                    }
                    finally
                    {
                        throttle.Release();
                    }
                }));
            }

            await TaskHelper.WhenAllAndThrow(deleteTasks.ToArray());
        }     
    }
}