using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Datasets.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using System;

namespace CalculateFunding.Services.Datasets
{
    public class ReportService : IReportService
    {
        private const string BlobContainerName = "converterwizardreports";

        private readonly IBlobClient _blobClient;

        public ReportService(IBlobClient blobClient)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));

            _blobClient = blobClient;
        }

        public IActionResult GetReportMetadata(string specificationId)
        {
            string blobName = $"{specificationId}/converterwizardreport.csv";

            string blobUrl = _blobClient.GetBlobSasUrl(blobName, DateTimeOffset.Now.AddDays(1), SharedAccessBlobPermissions.Read, BlobContainerName);

            return new OkObjectResult(new DatasetDownloadModel { Url = blobUrl });
        }
    }
}
