using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Extensions;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;
using JsonExtensions = CalculateFunding.Common.Extensions.JsonExtensions;

namespace CalculateFunding.Services.Publishing.Batches
{
    public class BatchUploadValidationService : JobProcessingService, IBatchUploadValidationService
    {
        private const string BatchPublishedProviderValidationJob = JobConstants.DefinitionNames.BatchPublishedProviderValidationJob;
        private const string ContainerName = "batchuploads";

        private readonly IBatchUploadReaderFactory _batchUploadReaderFactory;
        private readonly IPublishedFundingRepository _publishedFunding;
        private readonly IBlobClient _blobClient;
        private readonly IValidator<BatchUploadValidationRequest> _requestValidation;
        private readonly AsyncPolicy _publishedFundingResilience;
        private readonly AsyncPolicy _blobClientResilience;
        private readonly ILogger _logger;

        public BatchUploadValidationService(IJobManagement jobManagement,
            IValidator<BatchUploadValidationRequest> requestValidation,
            IBatchUploadReaderFactory batchUploadReaderFactory,
            IPublishedFundingRepository publishedFunding,
            IBlobClient blobClient,
            IPublishingResiliencePolicies resiliencePolicies,
            ILogger logger) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(requestValidation, nameof(requestValidation));
            Guard.ArgumentNotNull(batchUploadReaderFactory, nameof(batchUploadReaderFactory));
            Guard.ArgumentNotNull(publishedFunding, nameof(publishedFunding));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedFundingRepository, nameof(resiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.BlobClient, "resiliencePolicies.BlobClient");
            
            _requestValidation = requestValidation;
            _batchUploadReaderFactory = batchUploadReaderFactory;
            _publishedFunding = publishedFunding;
            _publishedFundingResilience = resiliencePolicies.PublishedFundingRepository;
            _blobClientResilience = resiliencePolicies.BlobClient;
            _logger = logger;
            _blobClient = blobClient;
        }

        public async Task<IActionResult> QueueBatchUploadValidation(BatchUploadValidationRequest batchUploadValidationRequest,
            Reference user,
            string correlationId)
        {
            ValidationResult validationResult = await _requestValidation.ValidateAsync(batchUploadValidationRequest);

            if (!validationResult.IsValid)
            {
                BadRequestObjectResult badRequest = validationResult.AsBadRequest();
                
                _logger.Warning($"Unable to queue batch upload validation job as validation errors.\n{badRequest.Value}");

                return badRequest;
            }

            BatchUploadValidationProperties properties = batchUploadValidationRequest;

            Job job = await QueueJob(new JobCreateModel
            {
                CorrelationId = correlationId,
                InvokerUserId = user?.Id,
                InvokerUserDisplayName = user?.Name,
                JobDefinitionId = BatchPublishedProviderValidationJob,
                SpecificationId = batchUploadValidationRequest.SpecificationId,
                Trigger = new Trigger(),
                Properties = (Dictionary<string, string>) properties
            });

            return new OkObjectResult(new
            {
                JobId = job.Id
            });
        }

        public override async Task Process(Message message)
        {
            BatchUploadValidationProperties properties = message;

            string batchId = properties.BatchId;
            string fundingStreamId = properties.FundingStreamId;
            string fundingPeriodId = properties.FundingPeriodId;
            
            BatchUploadBlobName blobName = new BatchUploadBlobName(batchId);

            IBatchUploadReader batchUploadReader =  _batchUploadReaderFactory.CreateBatchUploadReader();

            await batchUploadReader.LoadBatchUpload(blobName);
            
            List<string> publishedProviderIds = new List<string>();
            List<string> missingUkprns = new List<string>();
            
            _logger.Information($"Starting validation for batch {batchId} with a total of {batchUploadReader.Count} to check");

            while (batchUploadReader.HasPages)
            {
                string[] page = batchUploadReader.NextPage()?.ToArray() ?? new string[0];

                IDictionary<string, string> publishedProviderIdPage = await _publishedFundingResilience.ExecuteAsync(() => _publishedFunding.GetPublishedProviderIdsForUkprns(fundingStreamId,
                    fundingPeriodId,
                    page)) ?? new Dictionary<string, string>();

                if (page.Length != publishedProviderIdPage.Count)
                {
                    missingUkprns.AddRange(page.Except(publishedProviderIdPage.Keys));
                }
                
                publishedProviderIds.AddRange(publishedProviderIdPage.Values);

                ItemsProcessed = ItemsProcessed.GetValueOrDefault() + publishedProviderIdPage.Count;
            }

            ItemsProcessed = batchUploadReader.Count;
            
            if (missingUkprns.Any())
            {
                ItemsFailed = missingUkprns.Count;
                
                throw new NonRetriableException(
                    $"Did not locate the following ukprns for {fundingStreamId} and {fundingPeriodId}:\n{missingUkprns.JoinWith(',')}");
            }
            
            byte[] publishedProviderIdsJsonBytes = JsonExtensions.AsJsonBytes(publishedProviderIds.ToArray());
            
            BatchUploadProviderIdsBlobName batchUploadProviderIdsBlobName = new BatchUploadProviderIdsBlobName(batchId);

            ICloudBlob cloudBlob = _blobClient.GetBlockBlobReference(batchUploadProviderIdsBlobName, ContainerName);
            
            await _blobClientResilience.ExecuteAsync(() => cloudBlob.UploadFromByteArrayAsync(publishedProviderIdsJsonBytes, 0, publishedProviderIdsJsonBytes.Length));
        }

        private class BatchUploadValidationProperties
        {
            private const string BatchIdKey = "batch-id";
            private const string FundingStreamIdKey = "funding-stream-id";
            private const string FundingPeriodIdKey = "funding-period-id";
            private const string SpecificationIdKey = "specification-id";
            
            public string BatchId { get; private set; }
            
            public string FundingStreamId {get; private set; }
            
            public string FundingPeriodId { get; private set; }
            
            public string SpecificationId { get; private set; }

            public static implicit operator Dictionary<string, string>(BatchUploadValidationProperties properties)
            {
                Guard.ArgumentNotNull(properties, nameof(properties));
                
                return new Dictionary<string, string>
                {
                    {BatchIdKey, properties.BatchId},   
                    {FundingStreamIdKey, properties.FundingStreamId},   
                    {FundingPeriodIdKey, properties.FundingPeriodId},
                    {SpecificationIdKey, properties.SpecificationId}
                };
            }
            
            public static implicit operator BatchUploadValidationProperties(BatchUploadValidationRequest request)
            {
                Guard.ArgumentNotNull(request, nameof(request));
                
                return new BatchUploadValidationProperties
                {
                    BatchId = request.BatchId,
                    FundingStreamId = request.FundingStreamId,
                    FundingPeriodId = request.FundingPeriodId,
                    SpecificationId = request.SpecificationId
                };
            }

            public static implicit operator BatchUploadValidationProperties(Message message)
            {
                Guard.ArgumentNotNull(message?.UserProperties, nameof(message.UserProperties));

                return new BatchUploadValidationProperties
                {
                    BatchId = message.GetUserProperty<string>(BatchIdKey) ?? throw new NonRetriableException($"No {BatchIdKey} property in message"),
                    FundingStreamId = message.GetUserProperty<string>(FundingStreamIdKey) ?? throw new NonRetriableException($"No {FundingStreamIdKey} property in message"),
                    FundingPeriodId = message.GetUserProperty<string>(FundingPeriodIdKey) ?? throw new NonRetriableException($"No {FundingPeriodIdKey} property in message"),
                    SpecificationId = message.GetUserProperty<string>(SpecificationIdKey) ?? throw new NonRetriableException($"No {SpecificationIdKey} property in message"),
                };
            }
        }
    }
}