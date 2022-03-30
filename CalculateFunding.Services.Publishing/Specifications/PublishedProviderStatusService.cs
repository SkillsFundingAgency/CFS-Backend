using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Helpers;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Specifications
{
    public class PublishedProviderStatusService : IPublishedProviderStatusService
    {
        private const string BlobContainerName = "publishingconfirmation";

        private readonly IPublishedProviderFundingCountProcessor _fundingCountProcessor;
        private readonly IPublishedProviderFundingSummaryProcessor _publishedProviderFundingSummaryProcessor;
        private readonly ISpecificationIdServiceRequestValidator _validator;
        private readonly ISpecificationService _specificationService;
        private readonly IPublishedFundingRepository _publishedFundingRepository;
        private readonly AsyncPolicy _publishedFundingRepositoryResilience;
        private readonly AsyncPolicy _specificationsRepositoryPolicy;
        private readonly AsyncPolicy _blobClientPolicy;
        private readonly IPublishedProviderFundingCsvDataProcessor _fundingCsvDataProcessor;
        private readonly ICsvUtils _csvUtils;
        private readonly IBlobClient _blobClient;
        private readonly IPoliciesService _policiesService;
        private readonly IReleaseManagementRepository _releaseManagementRepository;
        private readonly ILogger _logger;

        public PublishedProviderStatusService(
            ISpecificationIdServiceRequestValidator validator,
            ISpecificationService specificationService,
            IPublishedFundingRepository publishedFundingRepository,
            IPublishingResiliencePolicies publishingResiliencePolicies,
            IPublishedProviderFundingCountProcessor fundingCountProcessor,
            IPublishedProviderFundingCsvDataProcessor fundingCsvDataProcessor,
            ICsvUtils csvUtils,
            IBlobClient blobClient,
            IPublishedProviderFundingSummaryProcessor publishedProviderFundingSummaryProcessor,
            IPoliciesService policiesService,
            IReleaseManagementRepository releaseManagementRepository,
            ILogger logger)
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
            Guard.ArgumentNotNull(publishedProviderFundingSummaryProcessor, nameof(publishedProviderFundingSummaryProcessor));
            Guard.ArgumentNotNull(policiesService, nameof(policiesService));
            Guard.ArgumentNotNull(releaseManagementRepository, nameof(releaseManagementRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

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
            _publishedProviderFundingSummaryProcessor = publishedProviderFundingSummaryProcessor;
            _policiesService = policiesService;
            _releaseManagementRepository = releaseManagementRepository;
            _logger = logger;
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

        public async Task<IActionResult> GetApprovedPublishedProviderReleaseFundingSummary(ReleaseFundingPublishProvidersRequest request,
            string specificationId)
        {
            SpecificationSummary specificationSummary = await _specificationService.GetSpecificationSummaryById(specificationId);

            FundingConfiguration fundingConfiguration = await _policiesService.GetFundingConfiguration(
                specificationSummary.FundingStreams.First().Id, specificationSummary.FundingPeriod.Id);

            try
            {
                ReleaseFundingPublishedProvidersSummary fundingSummary =
                    await _publishedProviderFundingSummaryProcessor.GetFundingSummaryForApprovedPublishedProvidersByChannel(
                        request.PublishedProviderIds,
                        specificationSummary,
                        fundingConfiguration,
                        request.ChannelCodes);

                return new OkObjectResult(fundingSummary);
            }
            catch (KeyNotFoundException ex)
            {
                return new BadRequestObjectResult(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Error in GetPublishedProviderReleaseFundingSummary for specification {specificationId} with params {JsonConvert.SerializeObject(request)}: {ex.Message}");
                return new InternalServerErrorResult("An error occurred while generating funding summary");
            }
        }

        public async Task<IActionResult> GetProviderStatusCounts(string specificationId,
            string providerType,
            string localAuthority,
            string status,
            bool? isIndicative = null,
            string monthYearOpened = null)
        {
            ValidationResult validationResults = _validator.Validate(specificationId);

            if (!validationResults.IsValid) return validationResults.AsBadRequest();

            SpecificationSummary specificationSummary =
                await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationService.GetSpecificationSummaryById(specificationId));

            IEnumerable<PublishedProviderFundingStreamStatus> publishedProviderFundingStreamStatuses =
                await _publishedFundingRepositoryResilience.ExecuteAsync(() => _publishedFundingRepository.GetPublishedProviderStatusCounts(specificationId,
                    providerType,
                    localAuthority,
                    status,
                    isIndicative,
                    monthYearOpened));

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
            return await GetProviderDataAsCsv(
                null,
                specificationId,
                $"ProvidersToApprove-{DateTime.UtcNow:yyyyMMdd-HHmmssffff}",
                PublishedProviderStatus.Draft,
                PublishedProviderStatus.Updated);
        }

        public async Task<IActionResult> GetProviderDataForAllReleaseAsCsv(string specificationId)
        {
            return await GetProviderDataAsCsv(
                null,
                specificationId,
                $"ProvidersToRelease-{DateTime.UtcNow:yyyyMMdd-HHmmssffff}",
                PublishedProviderStatus.Approved);
        }

        public async Task<IActionResult> GetPublishedProviderTransactions(string specificationId,
            string providerId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));

            Task<IEnumerable<PublishedProviderVersion>> unreleasedProviderVersionsQuery = _publishedFundingRepositoryResilience.ExecuteAsync(() =>
                _publishedFundingRepository.GetUnreleasedPublishedProviderVersions(specificationId, providerId));

            Task<IEnumerable<ReleasedDataAllocationHistory>> releasedDataQuery =
                 _releaseManagementRepository.GetPublishedProviderTransactionHistory(specificationId, providerId);

            Task<SpecificationSummary> specificationQuery = _specificationService.GetSpecificationSummaryById(specificationId);

            await TaskHelper.WhenAllAndThrow(unreleasedProviderVersionsQuery, releasedDataQuery, specificationQuery);

            IEnumerable<PublishedProviderVersion> unreleasedProviderVersions = unreleasedProviderVersionsQuery.Result;
            IEnumerable<ReleasedDataAllocationHistory> releasedData = releasedDataQuery.Result;
            SpecificationSummary specificationSummary = specificationQuery.Result;

            FundingConfiguration fundingConfiguration =await _policiesService.GetFundingConfiguration(
                specificationSummary.FundingStreams.First().Id, specificationSummary.FundingPeriod.Id);

            IEnumerable<string> visibleChannels = fundingConfiguration.ReleaseChannels
                .Where(_ => _.IsVisible)
                .Select(_ => _.ChannelCode);

            IEnumerable<ReleasePublishedProviderTransaction> unreleasedTransaction = unreleasedProviderVersions.Select(x => new ReleasePublishedProviderTransaction
            {
                ProviderId = x.ProviderId,
                Author = x.Author,
                Date = x.Date,
                Status = x.Status,
                TotalFunding = x.TotalFunding,
                MajorVersion = x.MajorVersion,
                MinorVersion = x.MinorVersion,
                ChannelCode = null,
                ChannelName = null,
                VariationReasons = x.VariationReasons?.Select(_ => _.ToString()).ToArray()
            });

            IEnumerable<ReleasePublishedProviderTransaction> releasedTransactions = releasedData
                .GroupBy(c => new
                {
                    c.ProviderId,
                    c.AuthorName,
                    c.AuthorId,
                    c.StatusChangedDate,
                    c.ChannelCode,
                    c.ChannelName,
                    c.MajorVersion,
                    c.MinorVersion,
                    c.TotalFunding
                }).Where(_ => visibleChannels.Contains(_.Key.ChannelCode))
                .Select(x => new ReleasePublishedProviderTransaction
                {
                    ProviderId = x.Key.ProviderId,
                    Author = new Reference(x.Key.AuthorId, x.Key.AuthorName),
                    Date = x.Key.StatusChangedDate,
                    Status = PublishedProviderStatus.Released,
                    MajorVersion = x.Key.MajorVersion,
                    MinorVersion = x.Key.MinorVersion,
                    ChannelCode = x.Key.ChannelCode,
                    ChannelName = x.Key.ChannelName,
                    VariationReasons = x.Select(s => s.VariationReasonName).ToArray(),
                    TotalFunding = x.Key.TotalFunding,
                });

            return new OkObjectResult(
                unreleasedTransaction
                .Concat(releasedTransactions)
                .OrderByDescending(_ => _.Date)
                .ThenBy(_ => _.ChannelCode)
                .ToList());
        }

        private async Task<IActionResult> GetProviderDataAsCsv(PublishedProviderIdsRequest providerIds, string specificationId, string csvFileSuffix, params PublishedProviderStatus[] statuses)
        {
            ValidationResult validationResults = _validator.Validate(specificationId);

            if (!validationResults.IsValid) return validationResults.AsBadRequest();

            IEnumerable<PublishedProviderFundingCsvData> publishedProviderFundingData = await _fundingCsvDataProcessor.GetFundingData(
                                                                providerIds?.PublishedProviderIds,
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
                IsIndicative = x.IsIndicative,
                MajorVersion = x.MajorVersion,
                MinorVersion = x.MinorVersion,
                ProviderName = x.ProviderName,
                FundingAmount = x.TotalFunding,
                PreviousReleasedFundingAmount = x.LastReleasedTotalFunding == null ? "N/A" : x.LastReleasedTotalFunding.ToString(),
                Difference = x.LastReleasedTotalFunding == null ? 0 : (x.TotalFunding ?? 0) - x.LastReleasedTotalFunding,
                VariationReasons = string.Join(';', x.VariationReasons)
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
