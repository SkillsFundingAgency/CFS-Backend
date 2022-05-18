using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ByteSizeLib;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specs.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;

namespace CalculateFunding.Services.Specs
{
    public class SpecificationsReportService : ISpecificationsReportService, IHealthChecker
    {
        private readonly IBlobClient _blobClient;

        private const string CalcsResultsContainerName = "calcresults";
        private const string PublishedProviderVersionsContainerName = "publishingreports";

        private const string FundingLineReportFilePrefix = "funding-lines";
        private const string CalculationResultsReportFilePrefix = "calculation-results";

        public SpecificationsReportService(IBlobClient blobClient)
        {
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));

            _blobClient = blobClient;
        }

        public IActionResult GetReportMetadata(string specificationId, string targetFundingPeriodId = null)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            IEnumerable<SpecificationReport> specificationReports =
                GetReportMetadata($"{FundingLineReportFilePrefix}-{specificationId}", PublishedProviderVersionsContainerName, targetFundingPeriodId: targetFundingPeriodId)
                .Concat(
                    GetReportMetadata($"{CalculationResultsReportFilePrefix}-{specificationId}", CalcsResultsContainerName, JobType.CalcResult, targetFundingPeriodId: targetFundingPeriodId));

            return new OkObjectResult(specificationReports);
        }

        public async Task<IActionResult> DownloadReport(string reportId)
        {
            Guard.ArgumentNotNull(reportId, nameof(reportId));

            SpecificationReportIdentifier specificationReportIdentifier = DecodeReportId(reportId);

            ReportType reportType = GetReportType(specificationReportIdentifier.JobType);
            string containerName = GetContainerName(reportType);
            string blobName = GenerateFileName(specificationReportIdentifier);

            ICloudBlob cloudBlob;
            try
            {
                cloudBlob = await _blobClient.GetBlobReferenceFromServerAsync(blobName, containerName);
            }
            catch (StorageException)
            {
                return new StatusCodeResult((int)HttpStatusCode.NotFound);
            }

            cloudBlob.Metadata.TryGetValue("file_name", out string fileName);

            string blobUrl = _blobClient.GetBlobSasUrl(blobName, DateTimeOffset.Now.AddDays(1), SharedAccessBlobPermissions.Read, containerName);

            SpecificationsDownloadModel downloadModel = new SpecificationsDownloadModel { Url = blobUrl, FileName = fileName };
            return new OkObjectResult(downloadModel);
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) = await _blobClient.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(SpecificationsReportService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = Ok, DependencyName = _blobClient.GetType().GetFriendlyName(), Message = Message });

            return health;
        }

        private IEnumerable<SpecificationReport> GetReportMetadata(string fileNamePrefix, string containerName, JobType? reportType = null, string targetFundingPeriodId = null)
        {
            IEnumerable<IListBlobItem> listBlobItems = _blobClient.ListBlobs(fileNamePrefix, containerName, true, BlobListingDetails.Metadata).ToList();
            return GetReportMetadata(listBlobItems, reportType, targetFundingPeriodId);
        }

        private IEnumerable<SpecificationReport> GetReportMetadata(
            IEnumerable<IListBlobItem> listBlobItems,
            JobType? metadataJobType = null,
            string targetFundingPeriodId = null)
        {
            return listBlobItems.Select(b =>
            {
                ICloudBlob cloudBlob = (ICloudBlob)b;

                cloudBlob.Metadata.TryGetValue("file_name", out string fileName);
                cloudBlob.Metadata.TryGetValue("funding_period_id", out string fundingPeriodId);
                ByteSize fileLength = ByteSize.FromBytes(cloudBlob.Properties.Length);
                string fileSuffix = Path.GetExtension(b.Uri.AbsolutePath).Replace(".", string.Empty);

                JobType jobType = metadataJobType.GetValueOrDefault();

                if (!string.IsNullOrWhiteSpace(targetFundingPeriodId) && !string.IsNullOrWhiteSpace(fundingPeriodId) && fundingPeriodId != targetFundingPeriodId)
                {
                    return null;
                }

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
                    ReportType = jobType,
                    SpecificationReportIdentifier = EncodeReportId(GetReportId(cloudBlob.Metadata)),
                    Category = GetReportCategory(jobType).ToString(),
                    Grouping = GetReportGrouping(jobType),
                    GroupingLevel = GetReportGroupingLevel(jobType),
                    LastModified = cloudBlob.Properties.LastModified,
                    Format = fileSuffix.ToUpperInvariant(),
                    Size = $"{fileLength.LargestWholeNumberDecimalValue:0.#} {fileLength.LargestWholeNumberDecimalSymbol}",
                };
            }).Where(_ => _ != null).OrderByDescending(_ => _.LastModified);
        }

        private ReportCategory GetReportCategory(JobType jobType) =>
            jobType switch
            {
                { } type when type != JobType.CalcResult &&
                              (type == JobType.Released ||
                              type == JobType.History ||
                              type == JobType.HistoryProfileValues ||
                              type == JobType.CurrentProfileValues ||
                              type == JobType.CurrentState ||
                              type == JobType.CurrentOrganisationGroupValues ||
                              type == JobType.HistoryOrganisationGroupValues ||
                              type == JobType.HistoryPublishedProviderEstate ||
                              type == JobType.PublishedGroups ||
                              type == JobType.PublishedProviderStateSummary) => ReportCategory.History,
                JobType.CalcResult => ReportCategory.Live,
                _ => ReportCategory.Undefined
            };

        private ReportGroupingLevel GetReportGroupingLevel(JobType jobType) =>
            jobType switch
            {
                { } type when type == JobType.CalcResult ||
                              type == JobType.History ||
                              type == JobType.HistoryOrganisationGroupValues ||
                              type == JobType.HistoryProfileValues ||
                              type == JobType.HistoryPublishedProviderEstate
                               => ReportGroupingLevel.All,
                { } type when type == JobType.CurrentOrganisationGroupValues ||
                              type == JobType.CurrentProfileValues ||
                              type == JobType.CurrentState ||
                              type == JobType.PublishedProviderStateSummary
                               => ReportGroupingLevel.Current,
                { } type when type == JobType.Released ||
                                type == JobType.PublishedGroups
                               => ReportGroupingLevel.Released,
                _ => ReportGroupingLevel.Undefined
            };

        private ReportGrouping GetReportGrouping(JobType jobType) =>
            jobType switch
            {
                { } type when type == JobType.CurrentOrganisationGroupValues ||
                              type == JobType.PublishedGroups ||
                              type == JobType.HistoryOrganisationGroupValues ||
                              type == JobType.PublishedGroups => ReportGrouping.Group,
                { } type when type == JobType.CurrentProfileValues ||
                              type == JobType.HistoryProfileValues
                               => ReportGrouping.Profiling,
                { } type when type == JobType.CurrentState ||
                              type == JobType.History ||
                              type == JobType.HistoryPublishedProviderEstate ||
                              type == JobType.Released ||
                              type == JobType.PublishedProviderStateSummary
                               => ReportGrouping.Provider,
                JobType.CalcResult => ReportGrouping.Live,
                _ => ReportGrouping.Undefined
            };

        private ReportType GetReportType(JobType jobType) =>
            jobType switch
            {
                { } type when type != JobType.CalcResult &&
                              (type == JobType.Released ||
                              type == JobType.History ||
                              type == JobType.HistoryProfileValues ||
                              type == JobType.CurrentProfileValues ||
                              type == JobType.CurrentState ||
                              type == JobType.CurrentOrganisationGroupValues ||
                              type == JobType.HistoryOrganisationGroupValues ||
                              type == JobType.HistoryPublishedProviderEstate ||
                              type == JobType.PublishedGroups ||
                              type == JobType.PublishedProviderStateSummary) => ReportType.FundingLine,
                JobType.CalcResult => ReportType.CalculationResult,
                _ => throw new ArgumentOutOfRangeException()
            };

        private string GetContainerName(ReportType reportType) =>
            reportType switch
            {
                ReportType.FundingLine => PublishedProviderVersionsContainerName,
                ReportType.CalculationResult => CalcsResultsContainerName,
                _ => throw new ArgumentOutOfRangeException(),
            };

        private string EncodeReportId(SpecificationReportIdentifier specificationReportIdentifier)
        {
            byte[] idBytes = specificationReportIdentifier.AsJsonBytes();
            return Convert.ToBase64String(idBytes);
        }

        private SpecificationReportIdentifier DecodeReportId(string encodedReportId)
        {
            byte[] base64EncodedBytes = Convert.FromBase64String(encodedReportId);
            string decodedText = Encoding.UTF8.GetString(base64EncodedBytes);
            return decodedText.AsPoco<SpecificationReportIdentifier>();
        }

        private SpecificationReportIdentifier GetReportId(IDictionary<string, string> blobMetadata)
        {
            blobMetadata.TryGetValue("specification_id", out string specificationId);
            blobMetadata.TryGetValue("funding_stream_id", out string fundingStreamId);
            blobMetadata.TryGetValue("funding_period_id", out string fundingPeriodId);
            blobMetadata.TryGetValue("funding_line_code", out string fundingLineCode);
            blobMetadata.TryGetValue("job_type", out string jobTypeString);

            Enum.TryParse(jobTypeString, out JobType jobType);

            return new SpecificationReportIdentifier
            {
                JobType = jobType,
                FundingLineCode = fundingLineCode,
                FundingPeriodId = fundingPeriodId,
                FundingStreamId = fundingStreamId,
                SpecificationId = specificationId
            };
        }

        private string GenerateFileName(SpecificationReportIdentifier id)
        {
            ReportType reportType = GetReportType(id.JobType);

            switch (reportType)
            {
                case ReportType.FundingLine:
                    string fundingLineCode = WithPrefixDelimiterOrEmpty(id.FundingLineCode.ToASCII());
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
                        case JobType.PublishedGroups:
                            return $"{FundingLineReportFilePrefix}-{id.SpecificationId}-{id.JobType}{fundingLineCode}{fundingStreamId}.csv";
                        case JobType.HistoryPublishedProviderEstate:
                        case JobType.PublishedProviderStateSummary:
                            return $"{FundingLineReportFilePrefix}-{id.SpecificationId}-{id.JobType}-{id.FundingPeriodId}.csv";
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
