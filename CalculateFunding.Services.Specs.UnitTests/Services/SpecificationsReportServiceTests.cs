using System;
using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Common.Storage;
using CalculateFunding.Models.Specs;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
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
        private const string specificationId = "test-spec-1";

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
            string fundingLineBlobName = "funding-line-file";
            string fundingLineFileExtension = "csv";
            string fundingLineFileName = "Funding Lines";

            string calcResultsBlobName = "calc-result-file";
            string calcResultsFileExtension = "xlsx";

            IDictionary<string, string> fundingLineFileMetadata = new Dictionary<string, string>
            {
                {"job_type", ReportType.Released.ToString()},
                {"file_name", fundingLineFileName}
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
                    Arg.Any<string>(),
                    Arg.Any<bool>(),
                    Arg.Any<BlobListingDetails>())
                .Returns(fundingLineListBlobItems);

            _blobClient
                .ListBlobs(
                    $"calculation-results-{specificationId}",
                    Arg.Any<string>(),
                    Arg.Any<bool>(),
                    Arg.Any<BlobListingDetails>())
                .Returns(calcResultsListBlobItems);

            IActionResult result = _service.GetReportMetadata(specificationId);

            _blobClient
                .Received(1)
                .ListBlobs($"funding-lines-{specificationId}", "publishedproviderversions", true, BlobListingDetails.Metadata);

            _blobClient
                .Received(1)
                .ListBlobs($"calculation-results-{specificationId}", "calcresults", true, BlobListingDetails.Metadata);

            result
                .Should()
                .BeOfType<OkObjectResult>();

            IEnumerable<ReportMetadata> reportMetadata = ((OkObjectResult) result).Value as IEnumerable<ReportMetadata>;

            reportMetadata
                .Should()
                .NotBeNull();

            reportMetadata
                .Count()
                .Should()
                .Be(2);

            ReportMetadata metadata = reportMetadata
                .ElementAt(0);

            metadata
                .Should()
                .BeEquivalentTo(new ReportMetadata
                    {
                        Name = fundingLineFileName,
                        BlobName = "funding-line-file.csv",
                        Type = "Released",
                        Category = "Live",
                        Format = "csv"
                    },
                    opt => opt.Excluding(_ => _.Identifier));

            metadata
                .Identifier["job_type"]
                .Should()
                .Be("Released");
        }

        [TestMethod]
        public void DownloadReport_NullFileNamePassed_ThrowsException()
        {
            Func<IActionResult> invocation = () => _service.DownloadReport(null, ReportType.CalcResult);

            invocation
                .Should()
                .Throw<ArgumentNullException>()
                .Which
                .ParamName
                .Should()
                .Be("fileName");
        }

        [TestMethod]
        public void DownloadReport_GivenFileNameAndType_ReturnsDownloadUrl()
        {
            string fileName = "test.csv";
            string sasUrl = "http://www.test.com/test.csv";

            _blobClient
                .GetBlobSasUrl(Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<SharedAccessBlobPermissions>(), Arg.Any<string>())
                .Returns(sasUrl);

            IActionResult result = _service.DownloadReport(fileName, ReportType.CurrentProfileValues);

            result
                .Should()
                .BeOfType<OkObjectResult>()
                .Which
                .Value
                .Should()
                .BeOfType<SpecificationsDownloadModel>();

            SpecificationsDownloadModel downloadModel = (result as OkObjectResult).Value as SpecificationsDownloadModel;

            downloadModel
                .Url
                .Should()
                .Be(sasUrl);

            _blobClient
                .Received(1)
                .GetBlobSasUrl(fileName, Arg.Any<DateTimeOffset>(), SharedAccessBlobPermissions.Read, "publishedproviderversions");
        }

        private Uri BuildUri(string fileName, string extension)
        {
            return new Uri($"http://www.test.com/{fileName}.{extension}");
        }
    }
}