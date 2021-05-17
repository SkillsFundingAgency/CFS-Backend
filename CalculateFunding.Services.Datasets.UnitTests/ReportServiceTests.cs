using CalculateFunding.Common.Storage;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Tests.Common.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;

namespace CalculateFunding.Services.Datasets
{
    [TestClass]
    public class ReportServiceTests
    {
        private const string BlobContainerName = "converterwizardreports";

        private Mock<IBlobClient> _blobClient;
        private ReportService _reportService;

        [TestInitialize]
        public void Initialize()
        {
            _blobClient = new Mock<IBlobClient>();
            _reportService = new ReportService(_blobClient.Object);
        }

        [TestMethod]
        public void GetReportMetadata_ShouldReturnDownloadReportUrl_WhenCalledWithSpecificationId()
        {
            // Arrange
            string specificationId = NewRandomString();
            string blobSasUrl = NewRandomString();
            string blobName = $"{specificationId}/converterwizardreport.csv";

            _blobClient
                .Setup(_ => _.GetBlobSasUrl(blobName, It.IsAny<DateTimeOffset>(), SharedAccessBlobPermissions.Read, BlobContainerName))
                .Returns(blobSasUrl);

            // Act
            IActionResult actionResult = _reportService.GetReportMetadata(specificationId);

            // Assert
            actionResult
                .Should()
                .BeOfType<OkObjectResult>()
                .Subject
                .Value
                .Should()
                .BeOfType<DatasetDownloadModel>()
                .Subject
                .Url
                .Should()
                .Be(blobSasUrl);
        }

        private string NewRandomString() => new RandomString();
    }
}
