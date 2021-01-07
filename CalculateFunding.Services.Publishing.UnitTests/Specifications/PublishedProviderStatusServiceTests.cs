using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Core.Interfaces;
using Microsoft.Azure.Storage.Blob;
using System.IO;
using CalculateFunding.Common.Storage;

namespace CalculateFunding.Services.Publishing.UnitTests.Specifications
{
    [TestClass]
    public class PublishedProviderStatusServiceTests
    {
        private const string BlobContainerName = "publishingconfirmation";

        private PublishedProviderStatusService _service;

        private ISpecificationIdServiceRequestValidator _validator;
        private ValidationResult _validationResult;
        private ISpecificationService _specificationService;
        private IPublishedFundingRepository _publishedFundingRepository;
        private IPublishedProviderFundingCountProcessor _fundingCountProcessor;
        private IPublishedProviderFundingCsvDataProcessor _fundingCsvDataProcessor;
        private ICsvUtils _csvUtils;
        private IBlobClient _blobClient;
        private string _specificationId;

        private IActionResult _actionResult;

        private SpecificationSummary _specificationSummary;

        [TestInitialize]
        public void SetUp()
        {
            _specificationId = NewRandomString();
            _validationResult = new ValidationResult();

            _validator = Substitute.For<ISpecificationIdServiceRequestValidator>();
            _validator.Validate(_specificationId)
                .Returns(_validationResult);

            _specificationService = Substitute.For<ISpecificationService>();
            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();
            _fundingCountProcessor = Substitute.For<IPublishedProviderFundingCountProcessor>();
            _fundingCsvDataProcessor = Substitute.For<IPublishedProviderFundingCsvDataProcessor>();
            _csvUtils = Substitute.For<ICsvUtils>();
            _blobClient = Substitute.For<IBlobClient>();

            _service = new PublishedProviderStatusService(_validator, _specificationService, _publishedFundingRepository, new ResiliencePolicies
            {
                PublishedFundingRepository = Polly.Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Polly.Policy.NoOpAsync(),
                BlobClient = Polly.Policy.NoOpAsync()
            },
                _fundingCountProcessor,
                _fundingCsvDataProcessor,
                _csvUtils,
                _blobClient);
        }

        [TestMethod]
        public async Task ReturnsBadRequestWhenSuppliedSpecificationIdFailsValidation()
        {
            string[] expectedErrors = { NewRandomString(), NewRandomString() };

            GivenTheValidationErrors(expectedErrors);

            await WhenThePublishedProvidersStatusAreQueried();

            ThenTheResponseShouldBe<BadRequestObjectResult>();
        }

        [TestMethod]
        public async Task ReturnsThePublishedProviderStatusFromPublishedProviderRepository()
        {
            string fundingStreamId1 = NewRandomString();
            string fundingStreamId2 = NewRandomString();

            const string approvedStatus = "Approved";
            const string draftStatus = "Draft";
            const string releasedStatus = "Released";
            const string updatedStatus = "Updated";

            int fs1ApprovedCount = NewRandomNumber();
            int fs1DraftCount = NewRandomNumber();
            int fs1ReleasedCount = NewRandomNumber();
            int fs1UpdatedCount = NewRandomNumber();

            int fs2ApprovedCount = NewRandomNumber();
            int fs2DraftCount = NewRandomNumber();
            int fs2ReleasedCount = NewRandomNumber();
            int fs2UpdatedCount = NewRandomNumber();

            decimal fs1ApprovedTotalFunding = NewRandomNumber();
            decimal fs1DraftTotalFunding = NewRandomNumber();
            decimal fs1ReleasedTotalFunding = NewRandomNumber();
            decimal fs1UpdatedTotalFunding = NewRandomNumber();

            decimal fs2ApprovedTotalFunding = NewRandomNumber();
            decimal fs2DraftTotalFunding = NewRandomNumber();
            decimal fs2ReleasedTotalFunding = NewRandomNumber();
            decimal fs2UpdatedTotalFunding = NewRandomNumber();

            ProviderFundingStreamStatusResponse firstExpectedResponse = NewProviderFundingStreamStatusResponse(_ => _
                .WithFundingStreamId(fundingStreamId1)
                .WithProviderApprovedCount(fs1ApprovedCount)
                .WithProviderReleasedCount(fs1ReleasedCount)
                .WithProviderUpdatedCount(fs1UpdatedCount)
                .WithProviderDraftCount(fs1DraftCount)
                .WithTotalFunding(fs1ApprovedTotalFunding + fs1DraftTotalFunding + fs1ReleasedTotalFunding + fs1UpdatedTotalFunding));

            ProviderFundingStreamStatusResponse secondExpectedResponse = NewProviderFundingStreamStatusResponse(_ => _
                .WithFundingStreamId(fundingStreamId2)
                .WithProviderApprovedCount(fs2ApprovedCount)
                .WithProviderReleasedCount(fs2ReleasedCount)
                .WithProviderUpdatedCount(fs2UpdatedCount)
                .WithProviderDraftCount(fs2DraftCount)
                .WithTotalFunding(fs2ApprovedTotalFunding + fs2DraftTotalFunding + fs2ReleasedTotalFunding + fs2UpdatedTotalFunding));

            GivenThePublishedProvidersForTheSpecificationId(
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId1).WithCount(fs1ApprovedCount).WithStatus(approvedStatus).WithTotalFunding(fs1ApprovedTotalFunding)),
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId1).WithCount(fs1DraftCount).WithStatus(draftStatus).WithTotalFunding(fs1DraftTotalFunding)),
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId1).WithCount(fs1ReleasedCount).WithStatus(releasedStatus).WithTotalFunding(fs1ReleasedTotalFunding)),
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId1).WithCount(fs1UpdatedCount).WithStatus(updatedStatus).WithTotalFunding(fs1UpdatedTotalFunding)),
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId2).WithCount(fs2ApprovedCount).WithStatus(approvedStatus).WithTotalFunding(fs2ApprovedTotalFunding)),
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId2).WithCount(fs2DraftCount).WithStatus(draftStatus).WithTotalFunding(fs2DraftTotalFunding)),
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId2).WithCount(fs2ReleasedCount).WithStatus(releasedStatus).WithTotalFunding(fs2ReleasedTotalFunding)),
                NewPublishedProviderFundingStreamStatus(_ => _.WithFundingStreamId(fundingStreamId2).WithCount(fs2UpdatedCount).WithStatus(updatedStatus).WithTotalFunding(fs2UpdatedTotalFunding)));

            AndTheSpecificationSummaryIsRetrieved(NewSpecificationSummary(s =>
            {
                s.WithId(_specificationId);
                s.WithFundingStreamIds(new[] { fundingStreamId1, fundingStreamId2 });
            }));

            await WhenThePublishedProvidersStatusAreQueried();

            ThenTheResponseShouldBe<OkObjectResult>(_ =>
                ((IEnumerable<ProviderFundingStreamStatusResponse>)_.Value).SequenceEqual(new[]
                {
                    firstExpectedResponse,
                    secondExpectedResponse
                }, new ProviderFundingStreamStatusResponseComparer()));
        }

        [TestMethod]
        public async Task GetProviderBatchCountForApproval()
        {
            IEnumerable<string> publishedProviderIds = ArraySegment<string>.Empty;
            string specificationId = NewRandomString();

            PublishedProviderFundingCount expectedFundingCount = NewPublishedProviderFundingCount();

            GivenTheFundingCount(expectedFundingCount, publishedProviderIds, specificationId, PublishedProviderStatus.Draft, PublishedProviderStatus.Updated);

            OkObjectResult result = await WhenTheBatchCountForApprovalIsQueried(publishedProviderIds, specificationId) as OkObjectResult;

            result
                ?.Value
                .Should()
                .BeSameAs(expectedFundingCount);
        }

        [TestMethod]
        public async Task GetProviderBatchCountForRelease()
        {
            IEnumerable<string> publishedProviderIds = ArraySegment<string>.Empty;
            string specificationId = NewRandomString();

            PublishedProviderFundingCount expectedFundingCount = NewPublishedProviderFundingCount();

            GivenTheFundingCount(expectedFundingCount, publishedProviderIds, specificationId, PublishedProviderStatus.Approved);

            OkObjectResult result = await WhenTheBatchCountForApprovalIsQueried(publishedProviderIds, specificationId) as OkObjectResult;

            result
                ?.Value
                .Should()
                .BeSameAs(expectedFundingCount);
        }

        [TestMethod]
        public async Task GetProviderDataForBatchApprovalAsCsv_ShouldSaveCsvFileInStorageConstinerAndReturnsBlobUrl()
        {
            IEnumerable<string> publishedProviderIds = new[] { NewRandomString(), NewRandomString() };
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string providerName1 = NewRandomString();
            string providerName2 = NewRandomString();
            _validator.Validate(Arg.Is(specificationId))
                .Returns(new ValidationResult());

            IEnumerable<PublishedProviderFundingCsvData> expectedCsvData = new[]
            {
                new PublishedProviderFundingCsvData()
                {
                    FundingStreamId = fundingStreamId,
                    FundingPeriodId = fundingPeriodId,
                    SpecificationId = specificationId,
                    ProviderName = providerName1,
                    TotalFunding = 123
                },
                new PublishedProviderFundingCsvData()
                {
                    FundingStreamId = fundingStreamId,
                    FundingPeriodId = fundingPeriodId,
                    SpecificationId = specificationId,
                    ProviderName = providerName2,
                    TotalFunding = 4567
                }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob.Properties.Returns(new BlobProperties());
            blob.Metadata.Returns(new Dictionary<string, string>());

            string expectedUrl = "https://blob.test.com/tst";

            PublishedProviderStatus[] statuses = new[] { PublishedProviderStatus.Draft, PublishedProviderStatus.Updated };
            string blobNamePrefix = $"{fundingStreamId}-{fundingPeriodId}";

            GivenTheFundingDataForCsv(expectedCsvData, publishedProviderIds, specificationId, statuses);
            GivenBolbReference(blobNamePrefix, blob);
            GivenBlobUrl(blobNamePrefix, expectedUrl);

            OkObjectResult result = await WhenTheGetProviderDataForBatchApprovalAsCsvExecuted(publishedProviderIds, specificationId) as OkObjectResult;

            result
                .Should()
                .NotBeNull();

            result.Value
                .Should()
                .BeOfType<PublishedProviderDataDownload>()
                .Which
                .Url
                .Should()
                .Be(expectedUrl);

            await _fundingCsvDataProcessor
                .Received(1)
                .GetFundingData(
                Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(publishedProviderIds)),
                Arg.Is(specificationId),
                Arg.Is<PublishedProviderStatus[]>(s => s.SequenceEqual(statuses)));

            _blobClient
                .Received(1)
                .GetBlockBlobReference(Arg.Is<string>(x => x.StartsWith(blobNamePrefix)), Arg.Is(BlobContainerName));
            await blob
                .Received(1)
                .UploadFromStreamAsync(Arg.Any<Stream>());

            _blobClient
                .Received(1)
                .GetBlobSasUrl(Arg.Is<string>(x => x.StartsWith(blobNamePrefix)),
                Arg.Any<DateTimeOffset>(),
                Arg.Is(SharedAccessBlobPermissions.Read),
                Arg.Is(BlobContainerName));
        }

        [TestMethod]
        public async Task GetProviderDataForAllApprovalAsCsv_ShouldSaveCsvFileInStorageConstinerAndReturnsBlobUrl()
        {
            IEnumerable<string> publishedProviderIds = new[] { NewRandomString(), NewRandomString() };
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string providerName1 = NewRandomString();
            string providerName2 = NewRandomString();
            _validator.Validate(Arg.Is(_specificationId))
                .Returns(new ValidationResult());

            IEnumerable<PublishedProviderFundingCsvData> expectedCsvData = new[]
            {
                new PublishedProviderFundingCsvData()
                {
                    FundingStreamId = fundingStreamId,
                    FundingPeriodId = fundingPeriodId,
                    SpecificationId = _specificationId,
                    ProviderName = providerName1,
                    TotalFunding = 123
                },
                new PublishedProviderFundingCsvData()
                {
                    FundingStreamId = fundingStreamId,
                    FundingPeriodId = fundingPeriodId,
                    SpecificationId = _specificationId,
                    ProviderName = providerName2,
                    TotalFunding = 4567
                }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob.Properties.Returns(new BlobProperties());
            blob.Metadata.Returns(new Dictionary<string, string>());

            string expectedUrl = "https://blob.test.com/tst";

            PublishedProviderStatus[] statuses = new[] { PublishedProviderStatus.Draft, PublishedProviderStatus.Updated };
            string blobNamePrefix = $"{fundingStreamId}-{fundingPeriodId}";

            GivenTheFundingDataForCsv(expectedCsvData, publishedProviderIds, _specificationId, statuses);
            GivenBolbReference(blobNamePrefix, blob);
            GivenBlobUrl(blobNamePrefix, expectedUrl);
            GivenThePublishedProviderIdsForTheSpecificationId(publishedProviderIds);

            OkObjectResult result = await WhenTheGetProviderDataForAllApprovalAsCsvExecuted(_specificationId) as OkObjectResult;

            result
                .Should()
                .NotBeNull();

            result.Value
                .Should()
                .BeOfType<PublishedProviderDataDownload>()
                .Which
                .Url
                .Should()
                .Be(expectedUrl);

            await _fundingCsvDataProcessor
                .Received(1)
                .GetFundingData(
                Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(publishedProviderIds)),
                Arg.Is(_specificationId),
                Arg.Is<PublishedProviderStatus[]>(s => s.SequenceEqual(statuses)));

            _blobClient
                .Received(1)
                .GetBlockBlobReference(Arg.Is<string>(x => x.StartsWith(blobNamePrefix)), Arg.Is(BlobContainerName));
            await blob
                .Received(1)
                .UploadFromStreamAsync(Arg.Any<Stream>());

            _blobClient
                .Received(1)
                .GetBlobSasUrl(Arg.Is<string>(x => x.StartsWith(blobNamePrefix)),
                Arg.Any<DateTimeOffset>(),
                Arg.Is(SharedAccessBlobPermissions.Read),
                Arg.Is(BlobContainerName));
        }

        [TestMethod]
        public async Task GetProviderDataForBatchReleaseAsCsv_ShouldSaveCsvFileInStorageConstinerAndReturnsBlobUrl()
        {
            IEnumerable<string> publishedProviderIds = new[] { NewRandomString(), NewRandomString() };
            string specificationId = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string providerName1 = NewRandomString();
            string providerName2 = NewRandomString();
            _validator.Validate(Arg.Is(specificationId))
                .Returns(new ValidationResult());

            IEnumerable<PublishedProviderFundingCsvData> expectedCsvData = new[]
            {
                new PublishedProviderFundingCsvData()
                {
                    FundingStreamId = fundingStreamId,
                    FundingPeriodId = fundingPeriodId,
                    SpecificationId = specificationId,
                    ProviderName = providerName1,
                    TotalFunding = 123
                },
                new PublishedProviderFundingCsvData()
                {
                    FundingStreamId = fundingStreamId,
                    FundingPeriodId = fundingPeriodId,
                    SpecificationId = specificationId,
                    ProviderName = providerName2,
                    TotalFunding = 4567
                }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob.Properties.Returns(new BlobProperties());
            blob.Metadata.Returns(new Dictionary<string, string>());

            string expectedUrl = "https://blob.test.com/tst";

            PublishedProviderStatus[] statuses = new[] { PublishedProviderStatus.Approved };
            string blobNamePrefix = $"{fundingStreamId}-{fundingPeriodId}";

            GivenTheFundingDataForCsv(expectedCsvData, publishedProviderIds, specificationId, statuses);
            GivenBolbReference(blobNamePrefix, blob);
            GivenBlobUrl(blobNamePrefix, expectedUrl);

            OkObjectResult result = await WhenTheGetProviderDataForBatchReleaseAsCsvExecuted(publishedProviderIds, specificationId) as OkObjectResult;

            result
                .Should()
                .NotBeNull();

            result.Value
                .Should()
                .BeOfType<PublishedProviderDataDownload>()
                .Which
                .Url
                .Should()
                .Be(expectedUrl);

            await _fundingCsvDataProcessor
                .Received(1)
                .GetFundingData(
                Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(publishedProviderIds)),
                Arg.Is(specificationId),
                Arg.Is<PublishedProviderStatus[]>(s => s.SequenceEqual(statuses)));

            _blobClient
                .Received(1)
                .GetBlockBlobReference(Arg.Is<string>(x => x.StartsWith(blobNamePrefix)), Arg.Is(BlobContainerName));
            await blob
                .Received(1)
                .UploadFromStreamAsync(Arg.Any<Stream>());

            _blobClient
                .Received(1)
                .GetBlobSasUrl(Arg.Is<string>(x => x.StartsWith(blobNamePrefix)),
                Arg.Any<DateTimeOffset>(),
                Arg.Is(SharedAccessBlobPermissions.Read),
                Arg.Is(BlobContainerName));
        }

        [TestMethod]
        public async Task GetProviderDataForAllReleaseAsCsv_ShouldSaveCsvFileInStorageConstinerAndReturnsBlobUrl()
        {
            IEnumerable<string> publishedProviderIds = new[] { NewRandomString(), NewRandomString() };
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string providerName1 = NewRandomString();
            string providerName2 = NewRandomString();
            _validator.Validate(Arg.Is(_specificationId))
                .Returns(new ValidationResult());

            IEnumerable<PublishedProviderFundingCsvData> expectedCsvData = new[]
            {
                new PublishedProviderFundingCsvData()
                {
                    FundingStreamId = fundingStreamId,
                    FundingPeriodId = fundingPeriodId,
                    SpecificationId = _specificationId,
                    ProviderName = providerName1,
                    TotalFunding = 123
                },
                new PublishedProviderFundingCsvData()
                {
                    FundingStreamId = fundingStreamId,
                    FundingPeriodId = fundingPeriodId,
                    SpecificationId = _specificationId,
                    ProviderName = providerName2,
                    TotalFunding = 4567
                }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob.Properties.Returns(new BlobProperties());
            blob.Metadata.Returns(new Dictionary<string, string>());

            string expectedUrl = "https://blob.test.com/tst";

            PublishedProviderStatus[] statuses = new[] { PublishedProviderStatus.Approved };
            string blobNamePrefix = $"{fundingStreamId}-{fundingPeriodId}";

            GivenTheFundingDataForCsv(expectedCsvData, publishedProviderIds, _specificationId, statuses);
            GivenBolbReference(blobNamePrefix, blob);
            GivenBlobUrl(blobNamePrefix, expectedUrl);
            GivenThePublishedProviderIdsForTheSpecificationId(publishedProviderIds);

            OkObjectResult result = await WhenTheGetProviderDataForAllReleaseAsCsvExecuted(_specificationId) as OkObjectResult;

            result
                .Should()
                .NotBeNull();

            result.Value
                .Should()
                .BeOfType<PublishedProviderDataDownload>()
                .Which
                .Url
                .Should()
                .Be(expectedUrl);

            await _fundingCsvDataProcessor
                .Received(1)
                .GetFundingData(
                Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(publishedProviderIds)),
                Arg.Is(_specificationId),
                Arg.Is<PublishedProviderStatus[]>(s => s.SequenceEqual(statuses)));

            _blobClient
                .Received(1)
                .GetBlockBlobReference(Arg.Is<string>(x => x.StartsWith(blobNamePrefix)), Arg.Is(BlobContainerName));
            await blob
                .Received(1)
                .UploadFromStreamAsync(Arg.Any<Stream>());

            _blobClient
                .Received(1)
                .GetBlobSasUrl(Arg.Is<string>(x => x.StartsWith(blobNamePrefix)),
                Arg.Any<DateTimeOffset>(),
                Arg.Is(SharedAccessBlobPermissions.Read),
                Arg.Is(BlobContainerName));
        }

        [TestMethod]
        public async Task GetProviderDataForReleaseAsCsv_ShouldReturnNotFoundResultWhenNoDataFoundForGivenPublishedProviderIds()
        {
            IEnumerable<string> publishedProviderIds = new[] { NewRandomString(), NewRandomString() };
            string specificationId = NewRandomString();
            _validator.Validate(Arg.Is(specificationId))
                .Returns(new ValidationResult());

            PublishedProviderStatus[] statuses = new[] { PublishedProviderStatus.Approved };

            GivenTheFundingDataForCsv(Enumerable.Empty<PublishedProviderFundingCsvData>(), publishedProviderIds, specificationId, statuses);

            NotFoundObjectResult result = await WhenTheGetProviderDataForBatchReleaseAsCsvExecuted(publishedProviderIds, specificationId) as NotFoundObjectResult;

            result
                .Should()
                .NotBeNull();

            result.Value
                .Should()
                .BeOfType<string>()
                .Which
                .Should()
                .Be("No data found for given specification and published provider ids.");
        }

        public void GivenBlobUrl(string blobNamePrefix, string expectedUrl)
        {
            _blobClient.GetBlobSasUrl(Arg.Is<string>(x => x.StartsWith(blobNamePrefix)), Arg.Any<DateTimeOffset>(), Arg.Is(SharedAccessBlobPermissions.Read), Arg.Is(BlobContainerName))
                .Returns(expectedUrl);
        }

        public void GivenBolbReference(string blobNamePrefix, ICloudBlob cloudBlob)
        {
            _blobClient.GetBlockBlobReference(Arg.Is<string>(x => x.StartsWith(blobNamePrefix)), Arg.Is(BlobContainerName))
                .Returns(cloudBlob);
        }

        private void GivenTheFundingDataForCsv(IEnumerable<PublishedProviderFundingCsvData> fundingData,
            IEnumerable<string> publishedProviderIds,
            string specificationId,
            params PublishedProviderStatus[] statuses)
        {
            _fundingCsvDataProcessor.GetFundingData(
                Arg.Is<IEnumerable<string>>(ids => ids.SequenceEqual(publishedProviderIds)),
                Arg.Is(specificationId),
                Arg.Is<PublishedProviderStatus[]>(s => s.SequenceEqual(statuses)))
                .Returns(fundingData);
        }

        private async Task<IActionResult> WhenTheBatchCountForApprovalIsQueried(IEnumerable<string> publishedProviderIds,
            string specificationId)
            => await _service.GetProviderBatchCountForApproval(NewPublishedProviderIdsRequest(_ => _.WithProviders(publishedProviderIds.ToArray())),
                specificationId);

        private async Task<IActionResult> WhenTheBatchCountForReleaseIsQueried(IEnumerable<string> publishedProviderIds,
            string specificationId)
            => await _service.GetProviderBatchCountForRelease(NewPublishedProviderIdsRequest(_ => _.WithProviders(publishedProviderIds.ToArray())),
                specificationId);

        private async Task<IActionResult> WhenTheGetProviderDataForBatchApprovalAsCsvExecuted(IEnumerable<string> publishedProviderIds,
            string specificationId)
            => await _service.GetProviderDataForBatchApprovalAsCsv(NewPublishedProviderIdsRequest(_ => _.WithProviders(publishedProviderIds.ToArray())),
                specificationId);

        private async Task<IActionResult> WhenTheGetProviderDataForAllApprovalAsCsvExecuted(string specificationId)
            => await _service.GetProviderDataForAllApprovalAsCsv(specificationId);

        private async Task<IActionResult> WhenTheGetProviderDataForBatchReleaseAsCsvExecuted(IEnumerable<string> publishedProviderIds,
            string specificationId)
            => await _service.GetProviderDataForBatchReleaseAsCsv(NewPublishedProviderIdsRequest(_ => _.WithProviders(publishedProviderIds.ToArray())),
                specificationId);

        private async Task<IActionResult> WhenTheGetProviderDataForAllReleaseAsCsvExecuted(string specificationId)
            => await _service.GetProviderDataForAllReleaseAsCsv(specificationId);

        private void GivenTheFundingCount(PublishedProviderFundingCount fundingCount,
            IEnumerable<string> publishedProviderIds,
            string specificationId,
            params PublishedProviderStatus[] statuses)
        {
            _fundingCountProcessor.GetFundingCount(Arg.Is(publishedProviderIds),
                    Arg.Is(specificationId),
                    Arg.Is<PublishedProviderStatus[]>(sts => sts.SequenceEqual(statuses)))
                .Returns(fundingCount);
        }

        private PublishedProviderFundingCount NewPublishedProviderFundingCount() => new PublishedProviderFundingCountBuilder()
            .Build();

        private void AndTheSpecificationSummaryIsRetrieved(SpecificationSummary specificationSummary)
        {
            _specificationSummary = specificationSummary;
            _specificationService
                .GetSpecificationSummaryById(Arg.Is(_specificationId))
                .Returns(_specificationSummary);
        }

        private PublishedProviderFundingStreamStatus NewPublishedProviderFundingStreamStatus(Action<PublishedProviderFundingStreamStatusBuilder> setUp = null)
        {
            PublishedProviderFundingStreamStatusBuilder builder = new PublishedProviderFundingStreamStatusBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private ProviderFundingStreamStatusResponse NewProviderFundingStreamStatusResponse(Action<ProviderFundingStreamStatusResponseBuilder> setUp = null)
        {
            ProviderFundingStreamStatusResponseBuilder builder = new ProviderFundingStreamStatusResponseBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private PublishedProviderIdsRequest NewPublishedProviderIdsRequest(Action<PublishedProviderIdsRequestBuilder> setUp = null)
        {
            PublishedProviderIdsRequestBuilder publishedProviderIdsRequestBuilder = new PublishedProviderIdsRequestBuilder();

            setUp?.Invoke(publishedProviderIdsRequestBuilder);

            return publishedProviderIdsRequestBuilder.Build();
        }

        private void GivenThePublishedProvidersForTheSpecificationId(params PublishedProviderFundingStreamStatus[] publishedProviderFundingStreamStatuses)
        {
            _publishedFundingRepository.GetPublishedProviderStatusCounts(_specificationId, string.Empty, string.Empty, string.Empty)
                .Returns(publishedProviderFundingStreamStatuses);
        }

        private void GivenThePublishedProviderIdsForTheSpecificationId(IEnumerable<string> publishedProviderIds)
        {
            _publishedFundingRepository.GetPublishedProviderIds(_specificationId)
                .Returns(publishedProviderIds);
        }

        private async Task WhenThePublishedProvidersStatusAreQueried()
        {
            _actionResult = await _service.GetProviderStatusCounts(_specificationId, string.Empty, string.Empty, string.Empty);
        }

        private void GivenTheValidationErrors(params string[] errors)
        {
            foreach (var error in errors) _validationResult.Errors.Add(new ValidationFailure(error, error));
        }

        private void ThenTheResponseShouldBe<TActionResult>(Expression<Func<TActionResult, bool>> matcher = null)
            where TActionResult : IActionResult
        {
            _actionResult
                .Should()
                .BeOfType<TActionResult>();

            if (matcher == null) return;

            ((TActionResult)_actionResult)
                .Should()
                .Match(matcher);
        }

        private string NewRandomString()
        {
            return new RandomString();
        }

        private int NewRandomNumber()
        {
            return new RandomNumberBetween(1, 10000);
        }

        private SpecificationSummary NewSpecificationSummary(Action<SpecificationSummaryBuilder> setUp = null)
        {
            SpecificationSummaryBuilder builder = new SpecificationSummaryBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

    }
}
