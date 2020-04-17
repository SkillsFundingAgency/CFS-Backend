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
using ByteSizeLib;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsReportService : ISpecificationsReportService
    {
        private readonly IBlobClient _blobClient;

        private const string CalcsResultsContainerName = "calcresults";
        private const string PublishedProviderVersionsContainerName = "publishedproviderversions";

        private const string FundingLineReportFilePrefix = "funding-lines";
        private const string CalculationResultsReportFilePrefix = "calculation-results";

        public SpecificationsReportService(IBlobClient blobClient)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));

            _blobClient = blobClient;
        }

        public IActionResult GetReportMetadata(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            IEnumerable<SpecificationReport> specificationReports =
                GetReportMetadata($"{FundingLineReportFilePrefix}-{specificationId}", PublishedProviderVersionsContainerName)
                .Concat(
                    GetReportMetadata($"{CalculationResultsReportFilePrefix}-{specificationId}", CalcsResultsContainerName, JobType.CalcResult));

            return new OkObjectResult(specificationReports);
        }

        public async Task<IActionResult> DownloadReport(SpecificationReportIdentifier id)
        {
            Guard.ArgumentNotNull(id, nameof(id));

            ReportType reportType = GetReportType(id.JobType);
            string containerName = GetContainerName(reportType);
            string fileName = GenerateFileName(id);

            bool blobExists = await _blobClient.BlobExistsAsync(fileName, containerName);
            if (!blobExists)
            {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            string blobUrl = _blobClient.GetBlobSasUrl(fileName, DateTimeOffset.Now.AddDays(1), SharedAccessBlobPermissions.Read, containerName);
            
            SpecificationsDownloadModel downloadModel = new SpecificationsDownloadModel { Url = blobUrl };
            return new OkObjectResult(downloadModel);
        }

        private IEnumerable<SpecificationReport> GetReportMetadata(string fileNamePrefix, string containerName, JobType? reportType = null)
        {
            IEnumerable<IListBlobItem> listBlobItems = _blobClient.ListBlobs(fileNamePrefix, containerName, true, BlobListingDetails.Metadata).ToList();
            return GetReportMetadata(listBlobItems, reportType);
        }

        private IEnumerable<SpecificationReport> GetReportMetadata(
            IEnumerable<IListBlobItem> listBlobItems, 
            JobType? metadataJobType = null)
        {
            return listBlobItems.Select(b =>
            {
                ICloudBlob cloudBlob = (ICloudBlob)b;
                
                cloudBlob.Metadata.TryGetValue("file_name", out string fileName);
                ByteSize fileLength = ByteSize.FromBytes(cloudBlob.Properties.Length);
                string fileSuffix = Path.GetExtension(b.Uri.AbsolutePath).Replace(".", string.Empty);
                
                JobType jobType = metadataJobType.GetValueOrDefault();

                if (!metadataJobType.HasValue)
                {
                    cloudBlob.Metadata.TryGetValue("job_type", out string metadataJobTypeString);
                    bool reportTypeParseResult = Enum.TryParse(metadataJobTypeString, true, out jobType);

                    if (!reportTypeParseResult)
                    {
                        return null;
                    }
                }

                return new SpecificationReport
                {
                    Name = fileName,
                    Id = GetReportId(cloudBlob.Metadata, jobType),
                    Category = GetReportCategory(jobType).ToString(),
                    LastModified = cloudBlob.Properties.LastModified,
                    Format = fileSuffix.ToUpperInvariant(),
                    Size = $"{fileLength.LargestWholeNumberDecimalValue:0.#} {fileLength.LargestWholeNumberDecimalSymbol}"
                };
            }).Where(_ => _ != null).OrderByDescending(_ => _.LastModified);
        }

        private ReportCategory GetReportCategory(JobType jobType)
        {
            switch (jobType)
            {
                case JobType.CalcResult:
                case JobType.CurrentState:
                case JobType.Released:
                case JobType.CurrentProfileValues:
                case JobType.CurrentOrganisationGroupValues:
                    return ReportCategory.Live;
                case JobType.History:
                case JobType.HistoryProfileValues:
                case JobType.HistoryOrganisationGroupValues:
                case JobType.HistoryPublishedProviderEstate:
                    return ReportCategory.History;
                case JobType.Undefined:
                default:
                    return ReportCategory.Undefined;
            }
        }

        private ReportType GetReportType(JobType jobType)
        {
            switch (jobType)
            {
                case JobType.CurrentState:
                case JobType.Released:
                case JobType.History:
                case JobType.HistoryProfileValues:
                case JobType.CurrentProfileValues:
                case JobType.CurrentOrganisationGroupValues:
                case JobType.HistoryOrganisationGroupValues:
                case JobType.HistoryPublishedProviderEstate:
                    return ReportType.FundingLine;
                case JobType.CalcResult:
                    return ReportType.CalculationResult;
                case JobType.Undefined:
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private string GetContainerName(ReportType reportType)
        {
            return reportType switch
            {
                ReportType.FundingLine => PublishedProviderVersionsContainerName,
                ReportType.CalculationResult => CalcsResultsContainerName,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        private SpecificationReportIdentifier GetReportId(IDictionary<string, string> blobMetadata, JobType jobType)
        {
            blobMetadata.TryGetValue("specification_id", out string specificationId);
            blobMetadata.TryGetValue("funding_stream_id", out string fundingStreamId);
            blobMetadata.TryGetValue("funding_period_id", out string fundingPeriodId);
            blobMetadata.TryGetValue("funding_line_code", out string fundingLineCode);

            return blobMetadata != null ? new SpecificationReportIdentifier
            {
                JobType = jobType,
                FundingLineCode = fundingLineCode,
                FundingPeriodId = fundingPeriodId,
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId
            } : null;
        }

        private string GenerateFileName(SpecificationReportIdentifier id)
        {
            ReportType reportType = GetReportType(id.JobType);

            switch (reportType)
            {
                case ReportType.FundingLine:
                    string fundingLineCode = WithPrefixDelimiterOrEmpty(id.FundingLineCode);
                    string fundingStreamId = WithPrefixDelimiterOrEmpty(id.FundingStreamId);

                    switch (id.JobType)
                    {
                        case JobType.CurrentState:
                        case JobType.Released:
                        case JobType.History:
                        case JobType.HistoryProfileValues:
                        case JobType.CurrentProfileValues:
                        case JobType.CurrentOrganisationGroupValues:
                        case JobType.HistoryOrganisationGroupValues:
                            return $"{FundingLineReportFilePrefix}-{id.SpecificationId}-{id.JobType}{fundingLineCode}{fundingStreamId}.csv";
                        case JobType.HistoryPublishedProviderEstate:
                            return $"{FundingLineReportFilePrefix}-{id.SpecificationId}-{id.JobType}-{id.FundingPeriodId}.csv";
                        case JobType.CalcResult:
                        case JobType.Undefined:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                case ReportType.CalculationResult:
                    return $"{CalculationResultsReportFilePrefix}-{id.SpecificationId}.csv";
                default:
                    return null; 
            }
        }

        private string WithPrefixDelimiterOrEmpty(string literal) => literal.IsNullOrWhitespace() ? string.Empty : $"-{literal}";
    }
}
