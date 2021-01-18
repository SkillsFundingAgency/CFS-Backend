using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Polly;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Core.Interfaces;
using System;
using Microsoft.Azure.Storage.Blob;
using System.IO;
using CalculateFunding.Common.Storage;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class PublishedProviderStatusService : IPublishedProviderStatusService
    {
        private const string BlobContainerName = "publishingconfirmation";

        private readonly IPublishedProviderFundingCountProcessor _fundingCountProcessor;
        private readonly ISpecificationIdServiceRequestValidator _validator;
        private readonly ISpecificationService _specificationService;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly AsyncPolicy _publishedFundingRepositoryResilience;
        private readonly AsyncPolicy _specificationsRepositoryPolicy;
        private readonly AsyncPolicy _blobClientPolicy;
        private readonly IPublishedProviderFundingCsvDataProcessor _fundingCsvDataProcessor;
        private readonly ICsvUtils _csvUtils;
        private readonly IBlobClient _blobClient;

        public PublishedProviderStatusService(
            ISpecificationIdServiceRequestValidator validator,
            ISpecificationService specificationService,
            IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPublishedProviderFundingCountProcessor fundingCountProcessor,
            IPublishedProviderFundingCsvDataProcessor fundingCsvDataProcessor,
            ICsvUtils csvUtils,
            IBlobClient blobClient)
        {
            Guard.ArgumentNotNull(validator, nameof(validator));
            Guard.ArgumentNotNull(specificationService, nameof(specificationService));
            Guard.ArgumentNotNull(publishedFundingRepository, nameof(publishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies, nameof(publishingResiliencePolicies));
            Guard.ArgumentNotNull(publishingResiliencePolicies.PublishedFundingRepository, nameof(publishingResiliencePolicies.PublishedFundingRepository));
            Guard.ArgumentNotNull(publishingResiliencePolicies.SpecificationsRepositoryPolicy, nameof(publishingResiliencePolicies.SpecificationsRepositoryPolicy));
            Guard.ArgumentNotNull(publishingResiliencePolicies.BlobClient, nameof(publishingResiliencePolicies.BlobClient));
            Guard.ArgumentNotNull(fundingCountProcessor, nameof(fundingCountProcessor));
            Guard.ArgumentNotNull(fundingCsvDataProcessor, nameof(fundingCsvDataProcessor));
            Guard.ArgumentNotNull(csvUtils, nameof(csvUtils));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));

            _validator = validator;
            _specificationService = specificationService;
            _publishedFundingRepository = publishedFundingRepository;
            _fundingCountProcessor = fundingCountProcessor;
            _publishedFundingRepositoryResilience = publishingResiliencePolicies.PublishedFundingRepository;
            _specificationsRepositoryPolicy = publishingResiliencePolicies.SpecificationsRepositoryPolicy;
            _blobClientPolicy = publishingResiliencePolicies.BlobClient;
            _fundingCsvDataProcessor = fundingCsvDataProcessor;
            _csvUtils = csvUtils;
            _blobClient = blobClient;
        }

        public async Task<IActionResult> GetProviderBatchCountForApproval(PublishedProviderIdsRequest providerIds,
            string specificationId)
            => await GetProviderBatchCountForStatuses(providerIds, specificationId, PublishedProviderStatus.Draft, PublishedProviderStatus.Updated);

        public async Task<IActionResult> GetProviderBatchCountForRelease(PublishedProviderIdsRequest providerIds,
            string specificationId)
            => await GetProviderBatchCountForStatuses(providerIds, specificationId, PublishedProviderStatus.Approved);

        private async Task<IActionResult> GetProviderBatchCountForStatuses(PublishedProviderIdsRequest providerIds,
            string specificationId,
            params PublishedProviderStatus[] statuses)
        {
            PublishedProviderFundingCount fundingCount = await _fundingCountProcessor.GetFundingCount(providerIds.PublishedProviderIds,
                specificationId,
                statuses);

            return new ObjectResult(fundingCount);
        }

        public async Task<IActionResult> GetProviderStatusCounts(string specificationId, string providerType, string localAuthority, string status)
        {
            ValidationResult validationResults = _validator.Validate(specificationId);

            if (!validationResults.IsValid) return validationResults.AsBadRequest();

            SpecificationSummary specificationSummary =
                await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationService.GetSpecificationSummaryById(specificationId));

            IEnumerable<PublishedProviderFundingStreamStatus> publishedProviderFundingStreamStatuses =
                await _publishedFundingRepositoryResilience.ExecuteAsync(() => _publishedFundingRepository.GetPublishedProviderStatusCounts(specificationId, providerType, localAuthority, status));

            List<ProviderFundingStreamStatusResponse> response = new List<ProviderFundingStreamStatusResponse>();

            foreach (IGrouping<string, PublishedProviderFundingStreamStatus> publishedProviderFundingStreamGroup in publishedProviderFundingStreamStatuses.GroupBy(x => x.FundingStreamId))
            {
                if (!specificationSummary.FundingStreams.Select(x => x.Id).Contains(publishedProviderFundingStreamGroup.Key))
                {
                    continue;
                }

                response.Add(new ProviderFundingStreamStatusResponse
                {
                    FundingStreamId = publishedProviderFundingStreamGroup.Key,
                    ProviderApprovedCount = GetCountValueOrDefault(publishedProviderFundingStreamGroup, "Approved"),
                    ProviderDraftCount = GetCountValueOrDefault(publishedProviderFundingStreamGroup, "Draft"),
                    ProviderReleasedCount = GetCountValueOrDefault(publishedProviderFundingStreamGroup, "Released"),
                    ProviderUpdatedCount = GetCountValueOrDefault(publishedProviderFundingStreamGroup, "Updated"),
                    TotalFunding = publishedProviderFundingStreamGroup.Sum(x => x.TotalFunding)
                });
            }

            return new OkObjectResult(response);
        }

        private static int GetCountValueOrDefault(IGrouping<string, PublishedProviderFundingStreamStatus> publishedProviderFundingStreamGroup, string statusName)
        {
            PublishedProviderFundingStreamStatus publishedProviderFundingStreamStatus = publishedProviderFundingStreamGroup.SingleOrDefault(x => x.Status == statusName);

            return publishedProviderFundingStreamStatus == null ? default : publishedProviderFundingStreamStatus.Count;
        }

        public async Task<IActionResult> GetProviderDataForBatchApprovalAsCsv(PublishedProviderIdsRequest providerIds, string specificationId)
        {
            return await GetProviderDataAsCsv(
                providerIds, 
                specificationId, 
                $"ProvidersToApprove-{DateTime.UtcNow:yyyyMMdd-HHmmssffff}", 
                PublishedProviderStatus.Draft, 
                PublishedProviderStatus.Updated);
        }

        public async Task<IActionResult> GetProviderDataForBatchReleaseAsCsv(PublishedProviderIdsRequest providerIds, string specificationId)
        {
            return await GetProviderDataAsCsv(
                providerIds, 
                specificationId, 
                $"ProvidersToRelease-{DateTime.UtcNow:yyyyMMdd-HHmmssffff}", 
                PublishedProviderStatus.Approved);
        }

        public async Task<IActionResult> GetProviderDataForAllApprovalAsCsv(string specificationId)
        {
            IEnumerable<string> publishedProviderIds = 
                await _publishedFundingRepositoryResilience.ExecuteAsync(() => 
                    _publishedFundingRepository.GetPublishedProviderPublishedProviderIds(specificationId));
            PublishedProviderIdsRequest publishedProviderIdsRequest = new PublishedProviderIdsRequest { PublishedProviderIds = publishedProviderIds };

            return await GetProviderDataAsCsv(
                publishedProviderIdsRequest, 
                specificationId, 
                $"ProvidersToApprove-{DateTime.UtcNow:yyyyMMdd-HHmmssffff}", 
                PublishedProviderStatus.Draft, 
                PublishedProviderStatus.Updated);
        }

        public async Task<IActionResult> GetProviderDataForAllReleaseAsCsv(string specificationId)
        {
            IEnumerable<string> publishedProviderIds = 
                await _publishedFundingRepositoryResilience.ExecuteAsync(() => 
                    _publishedFundingRepository.GetPublishedProviderPublishedProviderIds(specificationId));
            PublishedProviderIdsRequest publishedProviderIdsRequest = new PublishedProviderIdsRequest { PublishedProviderIds = publishedProviderIds };

            return await GetProviderDataAsCsv(
                publishedProviderIdsRequest, 
                specificationId, 
                $"ProvidersToRelease-{DateTime.UtcNow:yyyyMMdd-HHmmssffff}", 
                PublishedProviderStatus.Approved);
        }

        private async Task<IActionResult> GetProviderDataAsCsv(PublishedProviderIdsRequest providerIds, string specificationId, string csvFileSuffix, params PublishedProviderStatus[] statuses)
        {
            ValidationResult validationResults = _validator.Validate(specificationId);

            if (!validationResults.IsValid) return validationResults.AsBadRequest();

            if (providerIds.PublishedProviderIds.IsNullOrEmpty())
            {
                return new BadRequestObjectResult("Provider ids must be provided");
            }

            IEnumerable<PublishedProviderFundingCsvData> publishedProviderFundingData = await _fundingCsvDataProcessor.GetFundingData(
                                                                providerIds.PublishedProviderIds,
                                                                specificationId,
                                                                statuses);

            if (publishedProviderFundingData.IsNullOrEmpty())
            {
                return new NotFoundObjectResult("No data found for given specification and published provider ids.");
            }

            IEnumerable<dynamic> csvRows = publishedProviderFundingData.Select(x => new
            {
                UKPRN = x.Ukprn,
                URN = x.Urn,
                UPIN = x.Upin,
                ProviderName = x.ProviderName,
                FundingAmount = x.TotalFunding
            });

            string csvFileData = _csvUtils.AsCsv(csvRows, true);
            string fundingStreamId = publishedProviderFundingData.First().FundingStreamId;
            string fundingPeriodId = publishedProviderFundingData.First().FundingPeriodId;
            string csvFileName = $"{fundingStreamId}-{fundingPeriodId}-{csvFileSuffix}.csv";

            string blobName = $"{csvFileName}";
            string blobUrl = string.Empty;

            await _blobClientPolicy.ExecuteAsync(async () =>
            {
                ICloudBlob blob = _blobClient.GetBlockBlobReference(blobName, BlobContainerName);
                blob.Properties.ContentDisposition = $"attachment; filename={csvFileName}";

                using (MemoryStream stream = new MemoryStream(csvFileData.AsUTF8Bytes()))
                {
                    await blob.UploadFromStreamAsync(stream);
                }

                blob.Metadata["fundingStreamId"] = fundingStreamId;
                blob.Metadata["fundingPeriodId"] = fundingPeriodId;
                blob.Metadata["specificationId"] = specificationId;
                blob.Metadata["fileName"] = Path.GetFileNameWithoutExtension(csvFileName);
                blob.SetMetadata();

                blobUrl = _blobClient.GetBlobSasUrl(blobName, DateTimeOffset.Now.AddDays(1), SharedAccessBlobPermissions.Read, BlobContainerName);
            });

            return new OkObjectResult(new PublishedProviderDataDownload() { Url = blobUrl });
        }
    }
}
