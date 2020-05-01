﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Storage;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace CalculateFunding.Services.Specs.UnitTests.Services
{
    [TestClass]
    public class SpecificationsReportServiceTests
    {
        private SpecificationsReportService _service;
        private IBlobClient _blobClient;

        private const string PublishedProviderVersionsContainerName = "publishingreports";

        [TestInitialize]
        public void SetUp()
        {
            _blobClient = Substitute.For<IBlobClient>();

            _service = new SpecificationsReportService(_blobClient);
        }

        [TestMethod]
        public void GetReportMetadata_NullSpecificationIdPassed_ThrowsException()
        {
            Func<IActionResult> invocation = () => _service.GetReportMetadata(null);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("specificationId");
        }

        [TestMethod]
        public void GetReportMetadata_GivenSpecificationIdWithBlobs_ReturnsReportMetadata()
        {
            string fundingLineBlobName = NewRandomString();
            string fundingLineFileExtension = "csv";
            string fundingLineFileName = "Funding Lines";

            string calcResultsBlobName = NewRandomString();
            string calcResultsFileExtension = "xlsx";

            JobType jobType = JobType.Released;
            string fundingLineCode = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();

            IDictionary<string, string> fundingLineFileMetadata = new Dictionary<string, string>
            {
                {"job_type", jobType.ToString()},
                {"file_name", fundingLineFileName},
                {"funding_line_code", fundingLineCode},
                {"funding_period_id", fundingPeriodId},
                {"funding_stream_id", fundingStreamId},
                {"specification_id", specificationId}
            };

            SpecificationReportIdentifier id = new SpecificationReportIdentifier
            {
                JobType = jobType,
                FundingLineCode = fundingLineCode,
                FundingPeriodId = fundingPeriodId,
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId
            };

            BlobProperties blobProperties = new BlobProperties();

            ICloudBlob fundingLineCloudBlob = Substitute.For<ICloudBlob>();

            fundingLineCloudBlob
                .Uri
                .Returns(BuildUri(fundingLineBlobName, fundingLineFileExtension));

            fundingLineCloudBlob
                .Metadata
                .Returns(fundingLineFileMetadata);

            fundingLineCloudBlob
                .Properties
                .Returns(blobProperties);

            IEnumerable<IListBlobItem> fundingLineListBlobItems = new List<IListBlobItem>
            {
                fundingLineCloudBlob
            };

            ICloudBlob calcResultsCloudBlob = Substitute.For<ICloudBlob>();

            calcResultsCloudBlob
                .Uri
                .Returns(BuildUri(calcResultsBlobName, calcResultsFileExtension));

            calcResultsCloudBlob
                .Properties
                .Returns(blobProperties);

            IEnumerable<IListBlobItem> calcResultsListBlobItems = new List<IListBlobItem>
            {
                calcResultsCloudBlob
            };

            _blobClient
                .ListBlobs(
                    $"funding-lines-{specificationId}",
                    PublishedProviderVersionsContainerName,
                    true,
                    BlobListingDetails.Metadata)
                .Returns(fundingLineListBlobItems);

            _blobClient
                .ListBlobs(
                    $"calculation-results-{specificationId}",
                    "calcresults",
                    true,
                    BlobListingDetails.Metadata)
                .Returns(calcResultsListBlobItems);

            IActionResult result = _service.GetReportMetadata(specificationId);

            result
                .Should()
                .BeOfType<OkObjectResult>();

            IEnumerable<SpecificationReport> reportMetadata = ((OkObjectResult) result).Value as IEnumerable<SpecificationReport>;

            reportMetadata
                .Should()
                .NotBeNull();

            reportMetadata
                .Count()
                .Should()
                .Be(2);

            SpecificationReport metadata = reportMetadata
                .ElementAt(0);

            metadata
                .Should()
                .BeEquivalentTo(new SpecificationReport
                {
                        Name = fundingLineFileName,
                        Category = "Live",
                        Format = "CSV",
                        Size = "-1 B",
                        SpecificationReportIdentifier = EncodeSpecificationReportIdentifier(id)
                });
        }

        [TestMethod]
        public void DownloadReport_NullFileNamePassed_ThrowsException()
        {
            string reportId = null;
            Func<Task<IActionResult>> invocation = async () => await _service.DownloadReport(reportId);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be(nameof(reportId));
        }

        [TestMethod]
        public async Task DownloadReport_GivenNotExistingIdSent_ReturnsNotFound()
        {
            JobType jobType = JobType.Released;
            string fundingLineCode = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();

            SpecificationReportIdentifier id = new SpecificationReportIdentifier
            {
                JobType = jobType,
                FundingLineCode = fundingLineCode,
                FundingPeriodId = fundingPeriodId,
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId
            };

            string expectedBlobName = $"funding-lines-{specificationId}-{jobType}-{fundingLineCode}-{fundingStreamId}.csv";

            _blobClient
                .When(_ => _.GetBlobReferenceFromServerAsync(expectedBlobName, PublishedProviderVersionsContainerName))
                .Do(_ => { throw new StorageException(); });

            IActionResult result = await _service.DownloadReport(EncodeSpecificationReportIdentifier(id));

            result
                .Should()
                .BeOfType<StatusCodeResult>()
                .Which
                .StatusCode
                .Should()
                .Be((int)HttpStatusCode.NotFound);
        }

        [TestMethod]
        public async Task DownloadReport_GivenFileNameAndType_ReturnsDownloadUrl()
        {
            string sasUrl = "http://www.test.com/test.csv";

            JobType jobType = JobType.Released;
            string fundingLineCode = NewRandomString();
            string fundingPeriodId = NewRandomString();
            string fundingStreamId = NewRandomString();
            string specificationId = NewRandomString();

            SpecificationReportIdentifier id = new SpecificationReportIdentifier
            {
                JobType = jobType,
                FundingLineCode = fundingLineCode,
                FundingPeriodId = fundingPeriodId,
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId
            };

            string expectedBlobName = $"funding-lines-{specificationId}-{jobType}-{fundingLineCode}-{fundingStreamId}.csv";

            _blobClient
                .GetBlobSasUrl(expectedBlobName, Arg.Any<DateTimeOffset>(), SharedAccessBlobPermissions.Read, PublishedProviderVersionsContainerName)
                .Returns(sasUrl);

            string fundingLineFileName = "Funding Lines";

            IDictionary<string, string> fundingLineFileMetadata = new Dictionary<string, string>
            {
                {"file_name", fundingLineFileName},
            };

            ICloudBlob fundingLineCloudBlob = Substitute.For<ICloudBlob>();

            fundingLineCloudBlob
                .Metadata
                .Returns(fundingLineFileMetadata);

            _blobClient
                .GetBlobReferenceFromServerAsync(expectedBlobName, PublishedProviderVersionsContainerName)
                .Returns(Task.FromResult(fundingLineCloudBlob));

            IActionResult result = await _service.DownloadReport(EncodeSpecificationReportIdentifier(id));

            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<SpecificationsDownloadModel>();

            SpecificationsDownloadModel downloadModel = ((OkObjectResult)result).Value as SpecificationsDownloadModel;

            downloadModel
                .Url
                .Should()
                .Be(sasUrl);

            downloadModel
                .FileName
                .Should()
                .Be(fundingLineFileName);
        }

        private Uri BuildUri(string fileName, string extension)
        {
            return new Uri($"http://www.test.com/{fileName}.{extension}");
        }

        private static string NewRandomString() => new RandomString();

        private static string EncodeSpecificationReportIdentifier(SpecificationReportIdentifier specificationReportIdentifier)
        {
            string idJson = JsonExtensions.AsJson(specificationReportIdentifier);
            byte[] idBytes = Encoding.UTF8.GetBytes(idJson);
            return Convert.ToBase64String(idBytes);
        }

        private static SpecificationReportIdentifier DecodeSpecificationReportIdentifier(string encodedSpecificationReportIdentifier)
        {
            byte[] base64EncodedBytes = Convert.FromBase64String(encodedSpecificationReportIdentifier);
            string decodedText = Encoding.UTF8.GetString(base64EncodedBytes);
            return JsonExtensions.AsPoco<SpecificationReportIdentifier>(decodedText);
        }
    }
}