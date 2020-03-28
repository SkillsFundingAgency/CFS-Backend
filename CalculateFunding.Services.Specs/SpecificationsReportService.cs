using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Mvc;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using System.Collections.Generic;
using CalculateFunding.Models.Specs;
using System.Linq;
using System;
using System.IO;
using Microsoft.Azure.Storage.Blob;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsReportService : ISpecificationsReportService
    {
        private readonly IBlobClient _blobClient;

        private const string CalcsResultsContainerName = "calcresults";
        private const string PublishedProviderVersionsContainerName = "publishedproviderversions";

        public SpecificationsReportService(IBlobClient blobClient)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));

            _blobClient = blobClient;
        }

        public IActionResult DownloadReport(string fileName, string type)
        {
            Guard.ArgumentNotNull(fileName, nameof(fileName));
            Guard.ArgumentNotNull(type, nameof(type));

            Enum.TryParse(type, true, out ReportType reportType);
            string containerName = GetContainerName(reportType);
            
            string blobUrl = _blobClient.GetBlobSasUrl(fileName, DateTimeOffset.Now.AddDays(1), SharedAccessBlobPermissions.Read, containerName);
            
            SpecificationsDownloadModel downloadModel = new SpecificationsDownloadModel { Url = blobUrl };
            return new OkObjectResult(downloadModel);
        }

        public IActionResult GetReportMetadata(string specificationId)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            var publishingMetadata = GetReportMetadata($"funding-lines-{specificationId}", PublishedProviderVersionsContainerName).ToList();
            var calcsMetadata = GetReportMetadata($"calculation-results-{specificationId}", CalcsResultsContainerName, ReportType.CalcResult).ToList();

            publishingMetadata.AddRange(calcsMetadata);

            return new OkObjectResult(publishingMetadata);
        }

        private IEnumerable<ReportMetadata> GetReportMetadata(string fileNamePrefix, string containerName, ReportType? reportType = null)
        {
            IEnumerable<IListBlobItem> listBlobItems = _blobClient.ListBlobs(fileNamePrefix, containerName, true, BlobListingDetails.Metadata);
            return GetReportMetadata(listBlobItems, reportType);
        }

        private IEnumerable<ReportMetadata> GetReportMetadata(
            IEnumerable<IListBlobItem> listBlobItems, 
            ReportType? reportType = null)
        {
            return listBlobItems.Select(b =>
            {
                (b as ICloudBlob).Metadata.TryGetValue("file_name", out string fileName);
                (b as ICloudBlob).Metadata.TryGetValue("job_type", out string jobType);
                Enum.TryParse(Path.GetExtension(b.Uri.AbsolutePath).Replace(".", string.Empty), true, out ReportFormat reportFormat);

                if (!reportType.HasValue)
                {
                    Enum.TryParse(jobType, true, out ReportType parsedReportType);
                    reportType = parsedReportType;
                }

                return new ReportMetadata
                {
                    Name = fileName,
                    BlobName = Path.GetFileName(b.Uri.AbsolutePath),
                    Type = reportType.Value.ToString(),
                    Identifier = (b as ICloudBlob).Metadata,
                    Category = GetReportCategory(reportType.Value).ToString(),
                    LastModified = (b as ICloudBlob).Properties.LastModified,
                    Format = reportFormat.ToString(),
            };
            }).OrderByDescending(_ => _.LastModified);
        }

        private ReportCategory GetReportCategory(ReportType reportType)
        {
            switch (reportType)
            {
                case ReportType.CalcResult:
                case ReportType.CurrentState:
                case ReportType.Released:
                case ReportType.CurrentProfileValues:
                case ReportType.CurrentOrganisationGroupValues:
                    return ReportCategory.Live;
                case ReportType.History:
                case ReportType.HistoryProfileValues:
                case ReportType.HistoryOrganisationGroupValues:
                case ReportType.HistoryPublishedProviderEstate:
                    return ReportCategory.History;
                case ReportType.Undefined:
                default:
                    return ReportCategory.Undefined;
            }
        }

        private string GetContainerName(ReportType reportType)
        {
            switch (reportType)
            {
                case ReportType.Undefined:
                case ReportType.CurrentState:
                case ReportType.Released:
                case ReportType.History:
                case ReportType.HistoryProfileValues:
                case ReportType.CurrentProfileValues:
                case ReportType.CurrentOrganisationGroupValues:
                case ReportType.HistoryOrganisationGroupValues:
                case ReportType.HistoryPublishedProviderEstate:
                    return PublishedProviderVersionsContainerName;
                case ReportType.CalcResult:
                    return CalcsResultsContainerName;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
