using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Common.ApiClient.Policies.Models.FundingConfig;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Datasets.Converter;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Processing;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using static CalculateFunding.Services.Core.NonRetriableException;

namespace CalculateFunding.Services.Datasets.Converter
{
    public class ConverterWizardActivityCsvGenerationGeneratorService : JobProcessingService, IConverterWizardActivityCsvGenerationGeneratorService
    {
        public const int BatchSize = 100;

        private readonly ILogger _logger;
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IFileSystemCacheSettings _fileSystemCacheSettings;
        private readonly ICsvUtils _csvUtils;
        private readonly IConverterWizardActivityToCsvRowsTransformation _converterWizardActivityToCsvRowsTransformation;
        private readonly IConverterEligibleProviderService _converterEligibleProviderService;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly AsyncPolicy _specificationsApiClientPolicy;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly AsyncPolicy _policiesApiClientPolicy;
        private readonly IDefinitionSpecificationRelationshipService _definitionSpecificationRelationshipService;
        private readonly IConverterDataMergeLogger _converterDataMergeLogger;
        private readonly IConverterActivityReportRepository _converterActivityReportRepository;

        public ConverterWizardActivityCsvGenerationGeneratorService(
            IFileSystemAccess fileSystemAccess,
            IFileSystemCacheSettings fileSystemCacheSettings,
            ICsvUtils csvUtils,
            IConverterWizardActivityToCsvRowsTransformation converterWizardActivityToCsvRowsTransformation,
            IConverterEligibleProviderService converterEligibleProviderService,
            ISpecificationsApiClient specificationsApiClient,
            IPoliciesApiClient policiesApiClient,
            IConverterActivityReportRepository converterActivityReportRepository,
            IDatasetsResiliencePolicies datasetsResiliencePolicies,
            IDefinitionSpecificationRelationshipService definitionSpecificationRelationshipService,
            IConverterDataMergeLogger converterDataMergeLogger,
            IJobManagement jobManagement,
            ILogger logger) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(fileSystemAccess, nameof(fileSystemAccess));
            Guard.ArgumentNotNull(fileSystemCacheSettings, nameof(fileSystemCacheSettings));
            Guard.ArgumentNotNull(converterWizardActivityToCsvRowsTransformation, nameof(converterWizardActivityToCsvRowsTransformation));
            Guard.ArgumentNotNull(converterEligibleProviderService, nameof(converterEligibleProviderService));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(converterActivityReportRepository, nameof(converterActivityReportRepository));
            Guard.ArgumentNotNull(datasetsResiliencePolicies.SpecificationsApiClient, nameof(datasetsResiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(datasetsResiliencePolicies.PoliciesApiClient, nameof(datasetsResiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(definitionSpecificationRelationshipService, nameof(definitionSpecificationRelationshipService));
            Guard.ArgumentNotNull(converterDataMergeLogger, nameof(converterDataMergeLogger));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(csvUtils, nameof(csvUtils));

            _logger = logger;
            _fileSystemAccess = fileSystemAccess;
            _fileSystemCacheSettings = fileSystemCacheSettings;
            _converterWizardActivityToCsvRowsTransformation = converterWizardActivityToCsvRowsTransformation;
            _converterEligibleProviderService = converterEligibleProviderService;
            _specificationsApiClient = specificationsApiClient;
            _policiesApiClient = policiesApiClient;
            _converterActivityReportRepository = converterActivityReportRepository;
            _specificationsApiClientPolicy = datasetsResiliencePolicies.SpecificationsApiClient;
            _policiesApiClientPolicy = datasetsResiliencePolicies.PoliciesApiClient;
            _definitionSpecificationRelationshipService = definitionSpecificationRelationshipService;
            _converterDataMergeLogger = converterDataMergeLogger;
            _csvUtils = csvUtils;
        }

        public override async Task Process(Message message)
        {
            string specificationId = message.GetUserProperty<string>("specification-id");
            string parentJobId = message.GetUserProperty<string>("parentJobId");

            if (specificationId == null)
            {
                string error = "Specification id missing";

                _logger.Error(error);

                throw new NonRetriableException(error);
            }

            SpecificationSummary specificationSummary =
                (await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId)))?.Content;

            EnsureIsNotNull(specificationSummary, $"Did not locate s specification summary for id {specificationId}");

            FundingConfiguration fundingConfiguration = await GetFundingConfiguration(specificationSummary.FundingPeriod?.Id, specificationSummary.FundingStreams.First().Id);
            
            EnsureConvertersAreEnabledForFundingConfiguration(fundingConfiguration);

            IEnumerable<ProviderConverterDetail> providerConverters = await _converterEligibleProviderService.GetConvertersForProviderVersion(specificationSummary.ProviderVersionId, fundingConfiguration);
            
            if (providerConverters.IsNullOrEmpty()) return;

            IEnumerable<DatasetSpecificationRelationshipViewModel> converterEnabledDatasets = (await _definitionSpecificationRelationshipService.GetRelationshipsBySpecificationId(specificationId)).Where(_ => _.ConverterEnabled);

            IEnumerable<ConverterDataMergeLog> outcomeLogs = await _converterDataMergeLogger.GetLogs(parentJobId);

            // filter datasets based on logs
            string temporaryFilePath = new CsvFilePath(_fileSystemCacheSettings.Path, specificationId);

            EnsureFileIsNew(temporaryFilePath);

            bool outputHeader = true;

            foreach(IEnumerable<ProviderConverterDetail> providerConvertersBatch in providerConverters.Partition(BatchSize))
            {
                IEnumerable<ExpandoObject> csvRows = _converterWizardActivityToCsvRowsTransformation.TransformConvertWizardActivityIntoCsvRows(providerConvertersBatch, outcomeLogs, converterEnabledDatasets);

                string csv = _csvUtils.AsCsv(csvRows, outputHeader);

                await _fileSystemAccess.Append(temporaryFilePath, csv);

                outputHeader = false;
            }

            await using Stream csvFileStream = _fileSystemAccess.OpenRead(temporaryFilePath);

            await _converterActivityReportRepository.UploadReport(new CsvFileName(specificationId),
                GetPrettyFileName(specificationSummary.Name),
                csvFileStream, 
                GetMetadata(specificationId, specificationSummary.Name));
        }

        private IDictionary<string, string> GetMetadata(string specificationId, string specificationName)
        {
            return new Dictionary<string, string>
            {
                { "specification-id", specificationId },
                { "specification-name", specificationName },
                { "file-name", GetPrettyFileName(specificationName) },
                { "job-type", "ConverterWizardActivityCsvGenerationGenerator" }
            };
        }

        private void EnsureFileIsNew(string path)
        {
            if (_fileSystemAccess.Exists(path))
            {
                _fileSystemAccess.Delete(path);
            }
        }

        private static void EnsureConvertersAreEnabledForFundingConfiguration(FundingConfiguration fundingConfiguration)
        {
            Ensure(fundingConfiguration.EnableConverterDataMerge,
                $"Converter data merge not enabled for funding stream {fundingConfiguration.FundingStreamId} and funding period {fundingConfiguration.FundingPeriodId}");
        }

        private async Task<FundingConfiguration> GetFundingConfiguration(string fundingPeriodId,
            string fundingStreamId)
        {
            FundingConfiguration fundingConfiguration = (await _policiesApiClientPolicy.ExecuteAsync(() =>
                _policiesApiClient.GetFundingConfiguration(fundingStreamId,
                    fundingPeriodId)))?.Content;

            EnsureIsNotNull(fundingConfiguration, $"Did not locate funding configuration for {fundingStreamId} {fundingPeriodId}");

            return fundingConfiguration;
        }

        private string GetPrettyFileName(string specificationName)
        {
            return $"Converter wizard activity report {specificationName} {DateTimeOffset.UtcNow:s}.csv";
        }

        private class CsvFileName
        {
            private readonly string _specificationId;

            public CsvFileName(string specificationId)
            {
                _specificationId = specificationId;
            }

            public static implicit operator string(CsvFileName csvFileName)
            {
                return $"converter-wizard-activity-{csvFileName._specificationId}.csv";
            }
        }

        private class CsvFilePath
        {
            private readonly string _root;
            private readonly string _filename;

            public CsvFilePath(string root, string specificationId)
            {
                _root = root;
                _filename = new CsvFileName(specificationId);
            }

            public static implicit operator string(CsvFilePath csvFilePath)
            {
                return Path.Combine(csvFilePath._root, csvFilePath._filename);
            }
        }
    }
}
