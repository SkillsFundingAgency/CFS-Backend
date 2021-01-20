using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Results
{
    public class ProviderResultsCsvGeneratorService : JobProcessingService, IProviderResultsCsvGeneratorService, IHealthChecker
    {
        public const int BatchSize = 100;
        
        private readonly ILogger _logger;
        private readonly IBlobClient _blobClient;
        private readonly ICalculationsApiClient _calculationsApiClient;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly ICalculationResultsRepository _resultsRepository;
        private readonly ICsvUtils _csvUtils;
        private readonly IProviderResultsToCsvRowsTransformation _resultsToCsvRowsTransformation;
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IFileSystemCacheSettings _fileSystemCacheSettings;
        private readonly IJobManagement _jobManagement;
        private readonly AsyncPolicy _blobClientPolicy;
        private readonly AsyncPolicy _calculationsApiClientPolicy;
        private readonly AsyncPolicy _specificationsApiClientPolicy;
        private readonly AsyncPolicy _resultsRepositoryPolicy;

        public ProviderResultsCsvGeneratorService(ILogger logger,
            IBlobClient blobClient,
            ICalculationsApiClient calculationsApiClient,
            ISpecificationsApiClient specificationsApiClient,
            ICalculationResultsRepository resultsRepository,
            IResultsResiliencePolicies policies,
            ICsvUtils csvUtils,
            IProviderResultsToCsvRowsTransformation resultsToCsvRowsTransformation,
            IFileSystemAccess fileSystemAccess,
            IFileSystemCacheSettings fileSystemCacheSettings,
            IJobManagement jobManagement) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(resultsToCsvRowsTransformation, nameof(resultsToCsvRowsTransformation));
            Guard.ArgumentNotNull(fileSystemAccess, nameof(fileSystemAccess));
            Guard.ArgumentNotNull(policies?.BlobClient, nameof(policies.BlobClient));
            Guard.ArgumentNotNull(policies?.CalculationsApiClient, nameof(policies.CalculationsApiClient));
            Guard.ArgumentNotNull(policies?.SpecificationsApiClient, nameof(policies.SpecificationsApiClient));
            Guard.ArgumentNotNull(policies?.ResultsRepository, nameof(policies.ResultsRepository));
            Guard.ArgumentNotNull(fileSystemCacheSettings, nameof(fileSystemCacheSettings));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));

            _logger = logger;
            _blobClient = blobClient;
            _calculationsApiClient = calculationsApiClient;
            _specificationsApiClient = specificationsApiClient;
            _resultsRepository = resultsRepository;
            _blobClientPolicy = policies.BlobClient;
            _calculationsApiClientPolicy = policies.CalculationsApiClient;
            _specificationsApiClientPolicy = policies.SpecificationsApiClient;
            _resultsRepositoryPolicy = policies.ResultsRepository;
            _csvUtils = csvUtils;
            _resultsToCsvRowsTransformation = resultsToCsvRowsTransformation;
            _fileSystemAccess = fileSystemAccess;
            _fileSystemCacheSettings = fileSystemCacheSettings;
            _jobManagement = jobManagement;
        }

        public override async Task Process(Message message)
        {
            string specificationId = message.GetUserProperty<string>("specification-id");
            string specificationName = message.GetUserProperty<string>("specification-name");

            if (specificationId == null)
            {
                string error = "Specification id missing";

                _logger.Error(error);

                throw new NonRetriableException(error);
            }

            IEnumerable<string> jobDefinitions = new List<string>
            {
                JobConstants.DefinitionNames.CreateInstructAllocationJob
            };

            IEnumerable<string> jobTypesRunning = await GetJobTypes(specificationId, jobDefinitions);

            if (!jobTypesRunning.IsNullOrEmpty())
            {
                string errorMessage = string.Join(Environment.NewLine, jobTypesRunning.Select(_ => $"{_} is still running"));
                _logger.Error(errorMessage);

                throw new NonRetriableException(errorMessage);
            }

            string temporaryFilePath = new CsvFilePath(_fileSystemCacheSettings.Path, specificationId);

            EnsureFileIsNew(temporaryFilePath);

            bool outputHeaders = true;

            ApiResponse<SpecificationSummary> specificationSummaryResponse  = await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

            if (specificationSummaryResponse?.Content == null)
            {
                string errorMessage = $"Specification: {specificationId} not found";
                _logger.Error(errorMessage);
                
                throw new NonRetriableException(errorMessage);
            }

            SpecificationSummary specificationSummary = specificationSummaryResponse.Content;

            IEnumerable<TemplateMappingItem> allMappings = new TemplateMappingItem[0];

            foreach (Reference reference in specificationSummary.FundingStreams)
            {
                ApiResponse<TemplateMapping> templateMapping = await _calculationsApiClientPolicy.ExecuteAsync(() => _calculationsApiClient.GetTemplateMapping(specificationId, reference.Id));

                if (templateMapping?.Content == null)
                {
                    continue;
                }

                allMappings = allMappings.Concat(templateMapping.Content.TemplateMappingItems);
            }

            if (allMappings.Any(_ => _.CalculationId == null))
            {
                string errorMessage = $"Specification: {specificationId} has missing calculations in template mapping";
                _logger.Error(errorMessage);

                throw new NonRetriableException(errorMessage);
            }

            await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.ProviderResultsBatchProcessing(specificationId,
                providerResults =>
                {
                    IEnumerable<ExpandoObject>csvRows = _resultsToCsvRowsTransformation.TransformProviderResultsIntoCsvRows(providerResults, allMappings.ToDictionary(_ => _.CalculationId));

                    string csv = _csvUtils.AsCsv(csvRows, outputHeaders);

                    _fileSystemAccess.Append(temporaryFilePath, csv)
                        .GetAwaiter()
                        .GetResult();

                    outputHeaders = false;
                    return Task.CompletedTask;
                }, BatchSize)
            );

            ICloudBlob blob = _blobClient.GetBlockBlobReference($"calculation-results-{specificationId}.csv");
            blob.Properties.ContentDisposition = $"attachment; filename={GetPrettyFileName(specificationName)}";

            await using Stream csvFileStream = _fileSystemAccess.OpenRead(temporaryFilePath);
            
            await _blobClientPolicy.ExecuteAsync(() => UploadBlob(blob, csvFileStream, GetMetadata(specificationId, specificationName)));
        }

        private async Task UploadBlob(ICloudBlob blob, Stream csvFileStream, IDictionary<string, string> metadata) 
        {
            await _blobClient.UploadAsync(blob, csvFileStream);
            await _blobClient.AddMetadataAsync(blob, metadata);
        }

        private void EnsureFileIsNew(string path)
        {
            if (_fileSystemAccess.Exists(path))
            {
                _fileSystemAccess.Delete(path);
            }
        }

        private IDictionary<string, string> GetMetadata(string specificationId, string specificationName)
        {
            return new Dictionary<string, string>
            {
                { "specification-id", specificationId },
                { "specification-name", specificationName },
                { "file-name", GetPrettyFileName(specificationName) },
                { "job-type", "CalcResult" }
            };
        }

        private string GetPrettyFileName(string specificationName)
        {
            return $"Calculation Results {specificationName} {DateTimeOffset.UtcNow:s}.csv";
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) blobHealth = await _blobClient.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderResultsCsvGeneratorService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = blobHealth.Ok, DependencyName = _blobClient.GetType().GetFriendlyName(), Message = blobHealth.Message });

            return health;
        }

        public async Task<IEnumerable<string>> GetJobTypes(string specificationId, IEnumerable<string> jobTypes)
        {
            Guard.ArgumentNotNull(jobTypes, nameof(jobTypes));

            IEnumerable<JobSummary> jobSummaries = await _jobManagement.GetLatestJobsForSpecification(specificationId, jobTypes);

            return jobSummaries.Where(_ => _ != null && _.RunningStatus == RunningStatus.InProgress).Select(_ => _.JobType);
        }

        private class CsvFilePath
        {
            private readonly string _root;
            private readonly string _specificationId;

            public CsvFilePath(string root, string specificationId)
            {
                _root = root;
                _specificationId = specificationId;
            }

            public static implicit operator string(CsvFilePath csvFilePath)
            {
                return Path.Combine(csvFilePath._root, $"calculation-results-{csvFilePath._specificationId}.csv");
            }
        }
    }
}