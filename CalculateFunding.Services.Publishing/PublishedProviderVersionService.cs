using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing
{
    public class PublishedProviderVersionService : IPublishedProviderVersionService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly IBlobClient _blobClient;
        private readonly IJobManagement _jobManagement;
        private readonly AsyncPolicy _blobClientPolicy;

        public PublishedProviderVersionService(
            ILogger logger,
            IBlobClient blobClient,
            IPublishingResiliencePolicies resiliencePolicies, 
            IJobManagement jobManagement)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies?.BlobClient, nameof(resiliencePolicies.BlobClient));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));

            _logger = logger;
            _blobClient = blobClient;
            _jobManagement = jobManagement;
            _blobClientPolicy = resiliencePolicies.BlobClient;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = await _blobClient.IsHealthOk();
         
            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishedProviderVersionService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = _blobClient.GetType().GetFriendlyName(), Message = Message });
         
            return health;
        }

        public async Task<IActionResult> GetPublishedProviderVersionBody(string publishedProviderVersionId)
        {
            if (string.IsNullOrWhiteSpace(publishedProviderVersionId))
            {
                return new BadRequestObjectResult("Null or empty id provided.");
            }

            string blobName = $"{publishedProviderVersionId}.json";

            bool exists = await _blobClientPolicy.ExecuteAsync(() => _blobClient.BlobExistsAsync($"{publishedProviderVersionId}.json"));

            if (!exists)
            {
                _logger.Error($"Blob '{blobName}' does not exist.");

                return new NotFoundResult();
            }

            string template = string.Empty;

            try
            {
                ICloudBlob blob = await _blobClientPolicy.ExecuteAsync(() => _blobClient.GetBlobReferenceFromServerAsync(blobName));

                using Stream blobStream = await _blobClientPolicy.ExecuteAsync(() => _blobClient.DownloadToStreamAsync(blob));

                using StreamReader streamReader = new StreamReader(blobStream);

                template = await streamReader.ReadToEndAsync();
            }
            catch(Exception ex)
            {
                string errorMessage = $"Failed to fetch blob '{blobName}' from azure storage";

                _logger.Error(ex, errorMessage);

                return new InternalServerErrorResult(errorMessage);
            }

            return new OkObjectResult(template);
        }

        public async Task SavePublishedProviderVersionBody(
            string publishedProviderVersionId, 
            string publishedProviderVersionBody, 
            string specificationId)
        {
            Guard.IsNullOrWhiteSpace(publishedProviderVersionId, nameof(publishedProviderVersionId));
            Guard.IsNullOrWhiteSpace(publishedProviderVersionBody, nameof(publishedProviderVersionBody));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            string blobName = $"{publishedProviderVersionId}.json";

            try
            {
                ICloudBlob blob = _blobClient.GetBlockBlobReference(blobName);

                await _blobClientPolicy.ExecuteAsync(() => UploadBlob(blob, publishedProviderVersionBody, GetMetadata(specificationId)));
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to save blob '{blobName}' to azure storage";

                _logger.Error(ex, errorMessage);

                throw new Exception(errorMessage, ex);
            }
        }

        private async Task UploadBlob(ICloudBlob blob, string contents, IDictionary<string, string> metadata)
        {
            await _blobClient.UploadAsync(blob, contents);
            await _blobClient.AddMetadataAsync(blob, metadata);
        }

        private IDictionary<string, string> GetMetadata(string specificationId)
        {
            return new Dictionary<string, string>
            {
                { "specification-id", specificationId }
            };
        }

        public async Task<IActionResult> ReIndex(Reference user, string correlationId)
        {
            Guard.ArgumentNotNull(user, nameof(user));
            Guard.IsNullOrWhiteSpace(correlationId, nameof(correlationId));

            await CreateReIndexJob(user, correlationId);

            return new NoContentResult();
        }

        public async Task<Job> CreateReIndexJob(Reference user, string correlationId)
        {
            try
            {
                Job job = await _jobManagement.QueueJob(new JobCreateModel
                {
                    JobDefinitionId = JobConstants.DefinitionNames.ReIndexPublishedProvidersJob,
                    InvokerUserId = user.Id,
                    InvokerUserDisplayName = user.Name,
                    CorrelationId = correlationId,
                    Trigger = new Trigger
                    {
                        Message = "ReIndexing PublishedProviders",
                        EntityType = nameof(PublishedProviderIndex),
                    }
                });

                if (job != null)
                {
                    _logger.Information($"New job of type '{job.JobDefinitionId}' created with id: '{job.Id}'");
                }
                else
                {
                    string errorMessage = $"Failed to create job of type '{JobConstants.DefinitionNames.ReIndexPublishedProvidersJob}'";

                    _logger.Error(errorMessage);
                }

                return job;
            }
            catch (Exception ex)
            {
                string error = $"Failed to queue published provider re-index job";

                _logger.Error(ex, error);

                throw new Exception(error);
            }
        }
    }
}
