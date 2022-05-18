﻿using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.ApiClient.Policies.Models;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Storage;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.Interfaces;
using CalculateFunding.Services.Publishing.FundingManagement.SqlModels.QueryResults;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Models;
using CalculateFunding.Services.Publishing.Specifications;
using CalculateFunding.Services.Publishing.UnitTests.ReleaseManagement;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

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
        private IPublishedProviderFundingSummaryProcessor _publishedProviderFundingSummaryProcessor;
        private ICsvUtils _csvUtils;
        private IBlobClient _blobClient;
        private IPoliciesService _policiesService;
        private IReleaseManagementRepository _releaseManagementRepository;
        private ILogger _logger;
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
            _policiesService = Substitute.For<IPoliciesService>();
            _publishedFundingRepository = Substitute.For<IPublishedFundingRepository>();
            _fundingCountProcessor = Substitute.For<IPublishedProviderFundingCountProcessor>();
            _fundingCsvDataProcessor = Substitute.For<IPublishedProviderFundingCsvDataProcessor>();
            _publishedProviderFundingSummaryProcessor = Substitute.For<IPublishedProviderFundingSummaryProcessor>();
            _releaseManagementRepository = Substitute.For<IReleaseManagementRepository>();
            _csvUtils = Substitute.For<ICsvUtils>();
            _blobClient = Substitute.For<IBlobClient>();
            _logger = Substitute.For<ILogger>();

            _service = new PublishedProviderStatusService(_validator, _specificationService, _publishedFundingRepository, new ResiliencePolicies
            {
                PublishedFundingRepository = Polly.Policy.NoOpAsync(),
                SpecificationsRepositoryPolicy = Polly.Policy.NoOpAsync(),
                BlobClient = Polly.Policy.NoOpAsync()
            },
                _fundingCountProcessor,
                _fundingCsvDataProcessor,
                _csvUtils,
                _blobClient,
                _publishedProviderFundingSummaryProcessor,
                _policiesService,
                _releaseManagementRepository,
                _logger);
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
        [DataRow(null, null)]
        [DataRow(true, null)]
        [DataRow(false, null)]
        [DataRow(false, "Updated, Draft")]
        [DataRow(false, "Approved")]
        [DataRow(false, "Released")]
        public async Task ReturnsThePublishedProviderStatusFromPublishedProviderRepository(bool? isIndicative, string statuses)
        {
            string[] statusArray = statuses?.Split();
            string fundingStreamId1 = NewRandomString();
            string fundingStreamId2 = NewRandomString();

            const string approvedStatus = "Approved";
            const string draftStatus = "Draft";
            const string releasedStatus = "Released";
            const string updatedStatus = "Updated";

            string monthYearOpened = NewRandomString();

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

            int expectedFs1ApprovedCount = statusArray.IsNullOrEmpty() || statusArray.Contains("Approved") ? fs1ApprovedCount : 0;
            int expectedFs1ReleasedCount = statusArray.IsNullOrEmpty() || statusArray.Contains("Released") ? fs1ReleasedCount : 0;
            int expectedFs1UpdatedCount = statusArray.IsNullOrEmpty() || statusArray.Contains("Updated") ? fs1UpdatedCount : 0;
            int expectedFs1DraftCount = statusArray.IsNullOrEmpty() || statusArray.Contains("Draft") ? fs1DraftCount : 0;

            decimal expectedFs1ApprovedTotalFunding = statusArray.IsNullOrEmpty() || statusArray.Contains("Approved") ? fs1ApprovedTotalFunding : 0;
            decimal expectedFs1ReleasedTotalFunding = statusArray.IsNullOrEmpty() || statusArray.Contains("Released") ? fs1ReleasedTotalFunding : 0;
            decimal expectedFs1UpdatedTotalFunding = statusArray.IsNullOrEmpty() || statusArray.Contains("Updated") ? fs1UpdatedTotalFunding : 0;
            decimal expectedFs1DraftTotalFunding = statusArray.IsNullOrEmpty() || statusArray.Contains("Draft") ? fs1DraftTotalFunding : 0;

            int expectedFs2ApprovedCount = statusArray.IsNullOrEmpty() || statusArray.Contains("Approved") ? fs2ApprovedCount : 0;
            int expectedFs2ReleasedCount = statusArray.IsNullOrEmpty() || statusArray.Contains("Released") ? fs2ReleasedCount : 0;
            int expectedFs2UpdatedCount = statusArray.IsNullOrEmpty() || statusArray.Contains("Updated") ? fs2UpdatedCount : 0;
            int expectedFs2DraftCount = statusArray.IsNullOrEmpty() || statusArray.Contains("Draft") ? fs2DraftCount : 0;

            decimal expectedFs2ApprovedTotalFunding = statusArray.IsNullOrEmpty() || statusArray.Contains("Approved") ? fs2ApprovedTotalFunding : 0;
            decimal expectedFs2ReleasedTotalFunding = statusArray.IsNullOrEmpty() || statusArray.Contains("Released") ? fs2ReleasedTotalFunding : 0;
            decimal expectedFs2UpdatedTotalFunding = statusArray.IsNullOrEmpty() || statusArray.Contains("Updated") ? fs2UpdatedTotalFunding : 0;
            decimal expectedFs2DraftTotalFunding = statusArray.IsNullOrEmpty() || statusArray.Contains("Draft") ? fs2DraftTotalFunding : 0;

            ProviderFundingStreamStatusResponse firstExpectedResponse = NewProviderFundingStreamStatusResponse(_ => _
                .WithFundingStreamId(fundingStreamId1)
                .WithProviderApprovedCount(expectedFs1ApprovedCount)
                .WithProviderReleasedCount(expectedFs1ReleasedCount)
                .WithProviderUpdatedCount(expectedFs1UpdatedCount)
                .WithProviderDraftCount(expectedFs1DraftCount)
                .WithTotalFunding(expectedFs1ApprovedTotalFunding + expectedFs1DraftTotalFunding + expectedFs1ReleasedTotalFunding + expectedFs1UpdatedTotalFunding));

            ProviderFundingStreamStatusResponse secondExpectedResponse = NewProviderFundingStreamStatusResponse(_ => _
                .WithFundingStreamId(fundingStreamId2)
                .WithProviderApprovedCount(expectedFs2ApprovedCount)
                .WithProviderReleasedCount(expectedFs2ReleasedCount)
                .WithProviderUpdatedCount(expectedFs2UpdatedCount)
                .WithProviderDraftCount(expectedFs2DraftCount)
                .WithTotalFunding(expectedFs2ApprovedTotalFunding + expectedFs2DraftTotalFunding + expectedFs2ReleasedTotalFunding + expectedFs2UpdatedTotalFunding));

            GivenThePublishedProvidersForTheSpecificationId(isIndicative,
                monthYearOpened,
                statusArray,
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
                s.WithFundingStreamIds(fundingStreamId1, fundingStreamId2);
            }));

            await WhenThePublishedProvidersStatusAreQueried(isIndicative, statusArray, monthYearOpened);

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
            string providerIdOne = NewRandomString();
            string providerIdTwo = NewRandomString();
            int majorVersionOne = NewRandomNumber();
            int majorVersionTwo = NewRandomNumber();
            int minorVersionOne = NewRandomNumber();
            int minorVersionTwo = NewRandomNumber();

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
                    TotalFunding = 123,
                    ProviderId = providerIdOne
                },
                new PublishedProviderFundingCsvData()
                {
                    FundingStreamId = fundingStreamId,
                    FundingPeriodId = fundingPeriodId,
                    SpecificationId = specificationId,
                    ProviderName = providerName2,
                    TotalFunding = 4567,
                    ProviderId = providerIdTwo
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

            IEnumerable<Publishing.FundingManagement.SqlModels.ProviderVersionInChannel> providerVersionInChannels = new List<Publishing.FundingManagement.SqlModels.ProviderVersionInChannel>
            {
                NewProviderVersionInChannel(_ => _.WithProviderId(providerIdOne).WithChannelCode("Statement").WithMajorVersion(majorVersionOne).WithMinorVersion(minorVersionOne)),
                NewProviderVersionInChannel(_ => _.WithProviderId(providerIdTwo).WithChannelCode("Contracting").WithMajorVersion(majorVersionTwo).WithMinorVersion(minorVersionTwo))
            };

            AndProviderVersionInFundingConfigurationIsRetrieved(providerVersionInChannels);

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
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string providerName1 = NewRandomString();
            string providerName2 = NewRandomString();
            string providerIdOne = NewRandomString();
            string providerIdTwo = NewRandomString();
            int majorVersionOne = NewRandomNumber();
            int majorVersionTwo = NewRandomNumber();
            int minorVersionOne = NewRandomNumber();
            int minorVersionTwo = NewRandomNumber();

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
                    TotalFunding = 123,
                    ProviderId = providerIdOne
                },
                new PublishedProviderFundingCsvData()
                {
                    FundingStreamId = fundingStreamId,
                    FundingPeriodId = fundingPeriodId,
                    SpecificationId = _specificationId,
                    ProviderName = providerName2,
                    TotalFunding = 4567,
                    ProviderId = providerIdTwo
                }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob.Properties.Returns(new BlobProperties());
            blob.Metadata.Returns(new Dictionary<string, string>());

            string expectedUrl = "https://blob.test.com/tst";

            PublishedProviderStatus[] statuses = new[] { PublishedProviderStatus.Draft, PublishedProviderStatus.Updated };
            string blobNamePrefix = $"{fundingStreamId}-{fundingPeriodId}";

            GivenTheFundingDataForCsv(expectedCsvData, _specificationId, statuses);
            GivenBolbReference(blobNamePrefix, blob);
            GivenBlobUrl(blobNamePrefix, expectedUrl);

            IEnumerable<Publishing.FundingManagement.SqlModels.ProviderVersionInChannel> providerVersionInChannels = new List<Publishing.FundingManagement.SqlModels.ProviderVersionInChannel>
            {
                NewProviderVersionInChannel(_ => _.WithProviderId(providerIdOne).WithChannelCode("Statement").WithMajorVersion(majorVersionOne).WithMinorVersion(minorVersionOne)),
                NewProviderVersionInChannel(_ => _.WithProviderId(providerIdTwo).WithChannelCode("Contracting").WithMajorVersion(majorVersionTwo).WithMinorVersion(minorVersionTwo))
            };

            AndProviderVersionInFundingConfigurationIsRetrieved(providerVersionInChannels);

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
                null,
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
            string providerIdOne = NewRandomString();
            string providerIdTwo = NewRandomString();
            int majorVersionOne = NewRandomNumber();
            int majorVersionTwo = NewRandomNumber();
            int minorVersionOne = NewRandomNumber();
            int minorVersionTwo = NewRandomNumber();

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
                    TotalFunding = 123,
                    ProviderId = providerIdOne
                },
                new PublishedProviderFundingCsvData()
                {
                    FundingStreamId = fundingStreamId,
                    FundingPeriodId = fundingPeriodId,
                    SpecificationId = specificationId,
                    ProviderName = providerName2,
                    TotalFunding = 4567,
                    ProviderId = providerIdTwo
                }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob.Properties.Returns(new BlobProperties());
            blob.Metadata.Returns(new Dictionary<string, string>());

            string expectedUrl = "https://blob.test.com/tst";

            PublishedProviderStatus[] statuses = new[] { PublishedProviderStatus.Approved, PublishedProviderStatus.Released };
            string blobNamePrefix = $"{fundingStreamId}-{fundingPeriodId}";

            GivenTheFundingDataForCsv(expectedCsvData, publishedProviderIds, specificationId, statuses);
            GivenBolbReference(blobNamePrefix, blob);
            GivenBlobUrl(blobNamePrefix, expectedUrl);

            IEnumerable<Publishing.FundingManagement.SqlModels.ProviderVersionInChannel> providerVersionInChannels = new List<Publishing.FundingManagement.SqlModels.ProviderVersionInChannel>
            {
                NewProviderVersionInChannel(_ => _.WithProviderId(providerIdOne).WithChannelCode("Statement").WithMajorVersion(majorVersionOne).WithMinorVersion(minorVersionOne)),
                NewProviderVersionInChannel(_ => _.WithProviderId(providerIdTwo).WithChannelCode("Contracting").WithMajorVersion(majorVersionTwo).WithMinorVersion(minorVersionTwo))
            };

            AndProviderVersionInFundingConfigurationIsRetrieved(providerVersionInChannels);

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
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string providerName1 = NewRandomString();
            string providerName2 = NewRandomString();
            string providerIdOne = NewRandomString();
            string providerIdTwo = NewRandomString();
            int majorVersionOne = NewRandomNumber();
            int majorVersionTwo = NewRandomNumber();
            int minorVersionOne = NewRandomNumber();
            int minorVersionTwo = NewRandomNumber();

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
                    TotalFunding = 123,
                    ProviderId = providerIdOne
                },
                new PublishedProviderFundingCsvData()
                {
                    FundingStreamId = fundingStreamId,
                    FundingPeriodId = fundingPeriodId,
                    SpecificationId = _specificationId,
                    ProviderName = providerName2,
                    TotalFunding = 4567,
                    ProviderId = providerIdTwo
                }
            };

            ICloudBlob blob = Substitute.For<ICloudBlob>();
            blob.Properties.Returns(new BlobProperties());
            blob.Metadata.Returns(new Dictionary<string, string>());

            string expectedUrl = "https://blob.test.com/tst";

            PublishedProviderStatus[] statuses = new[] { PublishedProviderStatus.Approved };
            string blobNamePrefix = $"{fundingStreamId}-{fundingPeriodId}";

            GivenTheFundingDataForCsv(expectedCsvData, _specificationId, statuses);
            GivenBolbReference(blobNamePrefix, blob);
            GivenBlobUrl(blobNamePrefix, expectedUrl);

            IEnumerable<Publishing.FundingManagement.SqlModels.ProviderVersionInChannel> providerVersionInChannels = new List<Publishing.FundingManagement.SqlModels.ProviderVersionInChannel>
            {
                NewProviderVersionInChannel(_ => _.WithProviderId(providerIdOne).WithChannelCode("Statement").WithMajorVersion(majorVersionOne).WithMinorVersion(minorVersionOne)),
                NewProviderVersionInChannel(_ => _.WithProviderId(providerIdTwo).WithChannelCode("Contracting").WithMajorVersion(majorVersionTwo).WithMinorVersion(minorVersionTwo))
            };

            AndProviderVersionInFundingConfigurationIsRetrieved(providerVersionInChannels);

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
                null,
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

        [TestMethod]
        public async Task GetApprovedPublishedProviderReleaseFundingSummary_ShouldReturnOk()
        {
            IEnumerable<string> publishedProviderIds = new[] { NewRandomString(), NewRandomString() };
            IEnumerable<string> channelCodes = new[] { NewRandomString(), NewRandomString() };
            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                Id = _specificationId,
                FundingPeriod = new Reference { Id = NewRandomString() },
                FundingStreams = new List<Reference> { new Reference { Id = NewRandomString() } }
            };

            GivenFundingSummaryData();
            AndTheSpecificationSummaryIsRetrieved(specificationSummary);
            AndTheFundingConfigurationIsRetrieved(new FundingConfiguration());

            OkObjectResult result = await WhenReleaseSummaryExecuted(publishedProviderIds, channelCodes, specificationSummary.Id) as OkObjectResult;

            result
                .Should()
                .NotBeNull();

            result.Value
                .Should()
                .BeOfType<ReleaseFundingPublishedProvidersSummary>();
        }

        [TestMethod]
        public async Task GetApprovedPublishedProviderReleaseFundingSummary_WhenChannelCodeNotFound_ShouldReturnBadRequest()
        {
            IEnumerable<string> publishedProviderIds = new[] { NewRandomString(), NewRandomString() };
            IEnumerable<string> channelCodes = new[] { NewRandomString(), NewRandomString() };
            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                Id = _specificationId,
                FundingPeriod = new Reference { Id = NewRandomString() },
                FundingStreams = new List<Reference> { new Reference { Id = NewRandomString() } }
            };

            GivenFundingSummaryDataExceptionThrown(new KeyNotFoundException());
            AndTheSpecificationSummaryIsRetrieved(specificationSummary);
            AndTheFundingConfigurationIsRetrieved(new FundingConfiguration());

            BadRequestObjectResult result = await WhenReleaseSummaryExecuted(publishedProviderIds, channelCodes, specificationSummary.Id) as BadRequestObjectResult;

            result
                .Should()
                .NotBeNull();

            result.Value
                .Should()
                .BeOfType<string>();
        }

        [TestMethod]
        public async Task GetApprovedPublishedProviderReleaseFundingSummary_WhenExceptionThrown_ShouldReturnInternalServerError()
        {
            IEnumerable<string> publishedProviderIds = new[] { NewRandomString(), NewRandomString() };
            IEnumerable<string> channelCodes = new[] { NewRandomString(), NewRandomString() };
            SpecificationSummary specificationSummary = new SpecificationSummary
            {
                Id = _specificationId,
                FundingPeriod = new Reference { Id = NewRandomString() },
                FundingStreams = new List<Reference> { new Reference { Id = NewRandomString() } }
            };

            GivenFundingSummaryDataExceptionThrown(new Exception());
            AndTheSpecificationSummaryIsRetrieved(specificationSummary);
            AndTheFundingConfigurationIsRetrieved(new FundingConfiguration());

            InternalServerErrorResult result = await WhenReleaseSummaryExecuted(publishedProviderIds, channelCodes, specificationSummary.Id) as InternalServerErrorResult;

            result
                .Should()
                .NotBeNull();

            result.Value
                .Should()
                .BeOfType<string>();
        }

        [TestMethod]
        public async Task GetPublishedProviderTransactions_ReturnsSuccess()
        {
            string providerId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string fundingPeriodId = NewRandomString();

            Reference author = new Reference(NewRandomString(), NewRandomString());

            string statementChannelCode = NewRandomString();
            string contractingChannelCode = NewRandomString();
            string specToSpecChannelCode = NewRandomString();

            DateTime statusChangedDate = new RandomDateTime();

            decimal approvedFundingAmount = new decimal(NewRandomNumber());

            GivenTheUnreleasedPublishedProviderVersions(providerId, new PublishedProviderVersion
            {
                ProviderId = providerId,
                Author = author,
                Date = statusChangedDate,
                Status = PublishedProviderStatus.Approved,
                TotalFunding = approvedFundingAmount,
                MajorVersion = 0,
                MinorVersion = 1,
                VariationReasons = new List<VariationReason>
                {
                    VariationReason.AuthorityFieldUpdated,
                    VariationReason.CalculationValuesUpdated
                }
            });

            AndTheSpecificationSummaryIsRetrieved(NewSpecificationSummary(s =>
            {
                s.WithId(_specificationId);
                s.WithFundingStreamIds(fundingStreamId);
                s.WithFundingPeriodId(fundingPeriodId);
            }));

            AndTheFundingConfigurationIsRetrieved(NewFundingConfiguration(f =>
            {
                f.WithFundingPeriodId(fundingPeriodId);
                f.WithFundingStreamId(fundingStreamId);
                f.WithReleaseChannels(
                    NewFundingConfigurationChannel(c => c
                        .WithChannelCode(statementChannelCode)
                        .WithIsVisible(true)),
                    NewFundingConfigurationChannel(c => c
                        .WithChannelCode(contractingChannelCode)
                        .WithIsVisible(true)),
                    NewFundingConfigurationChannel(c => c
                        .WithChannelCode(specToSpecChannelCode)
                        .WithIsVisible(false)));
        }));

            string authorId = NewRandomString();
            string authorName = NewRandomString();

            string author2Id = NewRandomString();
            string author2Name = NewRandomString();

            decimal totalFunding = new decimal(NewRandomNumber());

            string variationReasonsForStatement = NewRandomString();
            string variationReasonsForContracting = NewRandomString();

            GivenTheReleasedDataAllocationHistory(providerId, new ReleasedDataAllocationHistory
            {
                ProviderId = providerId,
                AuthorId = authorId,
                AuthorName = authorName,
                StatusChangedDate = statusChangedDate.AddHours(2),
                MajorVersion = 2,
                MinorVersion = 0,
                TotalFunding = totalFunding,
                ChannelCode = statementChannelCode,
                ChannelName = "Statement",
                VariationReasonName = variationReasonsForStatement,
            }, new ReleasedDataAllocationHistory
            {
                ProviderId = providerId,
                AuthorId = author2Id,
                AuthorName = author2Name,
                StatusChangedDate = statusChangedDate.AddHours(1),
                MajorVersion = 1,
                MinorVersion = 0,
                TotalFunding = totalFunding,
                ChannelCode = contractingChannelCode,
                ChannelName = "Contracting channel name",
                VariationReasonName = variationReasonsForContracting,
            }, new ReleasedDataAllocationHistory
            {
                ProviderId = providerId,
                AuthorId = author2Id,
                AuthorName = author2Name,
                StatusChangedDate = statusChangedDate.AddHours(1),
                MajorVersion = 1,
                MinorVersion = 0,
                TotalFunding = totalFunding,
                ChannelCode = specToSpecChannelCode,
                ChannelName = "SpecToSpec",
                VariationReasonName = variationReasonsForContracting,
            });

            OkObjectResult result = await WhenGetPublishedProviderTransactions(_specificationId, providerId) as OkObjectResult;

            result
                .Should()
                .NotBeNull();

            result.Value
                .Should()
                .BeOfType<List<ReleasePublishedProviderTransaction>>();

            List<ReleasePublishedProviderTransaction> values = result.Value as List<ReleasePublishedProviderTransaction>;

            values
                .Should()
                .HaveCount(3);

            values
                .Where(_ => _.Status == PublishedProviderStatus.Released)
                .Should()
                .HaveCount(2);

            values.Should().BeEquivalentTo(new List<ReleasePublishedProviderTransaction>()
            {
                new ReleasePublishedProviderTransaction()
                {
                    Author = new Reference(authorId, authorName),
                    ChannelCode = statementChannelCode,
                    ChannelName = "Statement",
                    Date = statusChangedDate.AddHours(2),
                    MajorVersion = 2,
                    MinorVersion = 0,
                    ProviderId = providerId,
                    Status = PublishedProviderStatus.Released,
                    TotalFunding = totalFunding,
                    VariationReasons = new string[]{ variationReasonsForStatement },
                },
                new ReleasePublishedProviderTransaction()
                {
                    Author = new Reference(author2Id, author2Name),
                    ChannelCode = contractingChannelCode,
                    ChannelName = "Contracting channel name",
                    Date = statusChangedDate.AddHours(1),
                    MajorVersion = 1,
                    MinorVersion = 0,
                    ProviderId = providerId,
                    Status = PublishedProviderStatus.Released,
                    TotalFunding = totalFunding,
                    VariationReasons = new string[]{ variationReasonsForContracting },
                },
                new ReleasePublishedProviderTransaction()
                {
                    Author = author,
                    ChannelCode = null,
                    ChannelName = null,
                    Date = statusChangedDate,
                    MajorVersion = 0,
                    MinorVersion = 1,
                    ProviderId = providerId,
                    Status = PublishedProviderStatus.Approved,
                    TotalFunding = approvedFundingAmount,
                    VariationReasons = new [] { "AuthorityFieldUpdated", "CalculationValuesUpdated" }
                },
            });
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

        private void GivenFundingSummaryData()
        {
            _publishedProviderFundingSummaryProcessor.GetFundingSummaryForApprovedPublishedProvidersByChannel(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SpecificationSummary>(),
                Arg.Any<FundingConfiguration>(),
                Arg.Any<IEnumerable<string>>())
                .Returns(new ReleaseFundingPublishedProvidersSummary());
        }

        private void GivenFundingSummaryDataExceptionThrown(Exception exception)
        {
            _publishedProviderFundingSummaryProcessor.GetFundingSummaryForApprovedPublishedProvidersByChannel(
                Arg.Any<IEnumerable<string>>(),
                Arg.Any<SpecificationSummary>(),
                Arg.Any<FundingConfiguration>(),
                Arg.Any<IEnumerable<string>>())
                .Throws(exception);
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

        private void GivenTheFundingDataForCsv(IEnumerable<PublishedProviderFundingCsvData> fundingData,
            string specificationId,
            params PublishedProviderStatus[] statuses)
        {
            _fundingCsvDataProcessor.GetFundingData(
                null,
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

        private async Task<IActionResult> WhenReleaseSummaryExecuted(IEnumerable<string> publishedProviderIds,
            IEnumerable<string> channelCodes, string specificationId)
            => await _service.GetApprovedPublishedProviderReleaseFundingSummary(NewReleaseSummaryRequest(
                _ => _.WithProviders(publishedProviderIds.ToArray()).WithChannelCodes(channelCodes.ToArray())), specificationId);

        private async Task<IActionResult> WhenGetPublishedProviderTransactions(string specificationId, string providerId)
            => await _service.GetPublishedProviderTransactions(specificationId, providerId);

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

        private void AndTheFundingConfigurationIsRetrieved(FundingConfiguration fundingConfiguration)
        {
            _policiesService
                .GetFundingConfiguration(Arg.Any<string>(), Arg.Any<string>())
                .Returns(fundingConfiguration);
        }

        private void AndProviderVersionInFundingConfigurationIsRetrieved(IEnumerable<Publishing.FundingManagement.SqlModels.ProviderVersionInChannel> providerVersionInChannels)
        {
            _publishedProviderFundingSummaryProcessor
                .GetProviderVersionInFundingConfiguration(_specificationId, Arg.Any<FundingConfiguration>())
                .Returns(providerVersionInChannels);
        }

        private PublishedProviderFundingStreamStatus NewPublishedProviderFundingStreamStatus(Action<PublishedProviderFundingStreamStatusBuilder> setUp = null)
        {
            PublishedProviderFundingStreamStatusBuilder builder = new PublishedProviderFundingStreamStatusBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private Publishing.FundingManagement.SqlModels.ProviderVersionInChannel NewProviderVersionInChannel(Action<ProviderVersionInChannelBuilder> setUp = null)
        {
            ProviderVersionInChannelBuilder builder = new ProviderVersionInChannelBuilder();

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

        private ReleaseFundingPublishProvidersRequest NewReleaseSummaryRequest(Action<PublishedProviderFundingSummaryRequestBuilder> setUp = null)
        {
            PublishedProviderFundingSummaryRequestBuilder publishedProviderFundingSummaryBuilder = new PublishedProviderFundingSummaryRequestBuilder();

            setUp?.Invoke(publishedProviderFundingSummaryBuilder);

            return publishedProviderFundingSummaryBuilder.Build();
        }

        private void GivenThePublishedProvidersForTheSpecificationId(bool? isIndicative,
            string monthYearOpened,
            string[] statuses,
            params PublishedProviderFundingStreamStatus[] publishedProviderFundingStreamStatuses) =>
            _publishedFundingRepository.GetPublishedProviderStatusCounts(_specificationId,
                    string.Empty,
                    string.Empty,
                    Arg.Is<IEnumerable<string>>(_ => statuses.IsNullOrEmpty() || statuses.SequenceEqual(_)),
                    isIndicative,
                    monthYearOpened)
                .Returns(publishedProviderFundingStreamStatuses.Where(_ => statuses.IsNullOrEmpty() || statuses.Contains(_.Status)));

        private void GivenThePublishedProviderIdsForTheSpecificationId(IEnumerable<string> publishedProviderIds)
        {
            _publishedFundingRepository.GetPublishedProviderPublishedProviderIds(_specificationId)
                .Returns(publishedProviderIds);
        }

        private void GivenTheUnreleasedPublishedProviderVersions(string providerId, params PublishedProviderVersion[] publishedProviderVersions) =>
            _publishedFundingRepository.GetUnreleasedPublishedProviderVersions(_specificationId, providerId)
                .Returns(publishedProviderVersions);

        private void GivenTheReleasedDataAllocationHistory(string providerId, params ReleasedDataAllocationHistory[] releasedDataAllocationHistories) =>
            _releaseManagementRepository.GetPublishedProviderTransactionHistory(_specificationId, providerId)
            .Returns(releasedDataAllocationHistories);

        private async Task WhenThePublishedProvidersStatusAreQueried(bool? isIndicative = null,
            string[] statuses = null,
            string monthYearOpened = null)
        {
            _actionResult = await _service.GetProviderStatusCounts(_specificationId,
                string.Empty,
                string.Empty,
                statuses,
                isIndicative,
                monthYearOpened);
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

        private FundingConfiguration NewFundingConfiguration(Action<FundingConfigurationBuilder> setUp = null)
        {
            FundingConfigurationBuilder builder = new FundingConfigurationBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }

        private FundingConfigurationChannel NewFundingConfigurationChannel(Action<FundingConfigurationChannelBuilder> setUp = null)
        {
            FundingConfigurationChannelBuilder builder = new FundingConfigurationChannelBuilder();

            setUp?.Invoke(builder);

            return builder.Build();
        }
    }
}
