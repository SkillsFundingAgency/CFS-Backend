using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Processing;
using CalculateFunding.Services.Publishing.Interfaces;
using CalculateFunding.Services.Publishing.Reporting.ChannelLevelPublishedGroup;
using CalculateFunding.Services.Publishing.Reporting.FundingLines;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Publishing.Reporting.PublishedProviderState
{
    public abstract class BaseChannelLevelPublishedGroupCsvGenerator : JobProcessingService, IPublishedProviderEstateCsvGenerator, IChannelLevelPublishedGroupCsvGenerator, IHealthChecker
    {
        private const string PublishedFundingReportContainerName = "publishingreports";

        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IBlobClient _blobClient;
        private readonly AsyncPolicy _blobClientPolicy;
        protected readonly AsyncPolicy _policiesPolicy;
        private readonly ICsvUtils _csvUtils;
        private readonly ILogger _logger;
        private readonly IFileSystemCacheSettings _fileSystemCacheSettings;
        private readonly IFundingLineCsvTransformServiceLocator _transformServiceLocator;

        protected abstract string JobDefinitionName { get; }

        protected BaseChannelLevelPublishedGroupCsvGenerator(
            IJobManagement jobManagement,
            IFileSystemAccess fileSystemAccess,
            IBlobClient blobClient,
            IPublishingResiliencePolicies policies,
            ICsvUtils csvUtils,
            ILogger logger,
            IFileSystemCacheSettings fileSystemCacheSettings,
            IChannelLevelPublishedGroupsCsvBatchProcessorServiceLocator channelLevelPublishedGroupsCsvBatchProcessorServiceLocator,
            IFundingLineCsvTransformServiceLocator transformServiceLocator) : base(jobManagement, logger)
        {
            Guard.ArgumentNotNull(fileSystemAccess, nameof(fileSystemAccess));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(policies?.BlobClient, nameof(policies.BlobClient));
            Guard.ArgumentNotNull(policies?.PoliciesApiClient, nameof(policies.PoliciesApiClient));
            Guard.ArgumentNotNull(csvUtils, nameof(csvUtils));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(fileSystemCacheSettings, nameof(fileSystemCacheSettings));

            _fileSystemAccess = fileSystemAccess;
            _blobClient = blobClient;
            _blobClientPolicy = policies.BlobClient;
            _policiesPolicy = policies.PoliciesApiClient;
            _csvUtils = csvUtils;
            _fileSystemCacheSettings = fileSystemCacheSettings;
            _logger = logger;
            _transformServiceLocator = transformServiceLocator;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) blobHealth = await _blobClient.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishedProviderVersionService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = blobHealth.Ok, DependencyName = _blobClient.GetType().GetFriendlyName(), Message = blobHealth.Message });

            return health;
        }

        public override async Task Process(Message message)
        {
            IFundingLineCsvTransform channelLevelPublishedGroupCsvTransformServiceLocator = _transformServiceLocator.GetService(FundingLineCsvGeneratorJobType.ChannelLevelPublishedGroup);
            bool processedResults = await GenerateCsv(message, channelLevelPublishedGroupCsvTransformServiceLocator);

            if (!processedResults)
            {
                _logger.Information("Did not create a new csv report as no providers matched");

                return;
            }
        }

        protected abstract Task<bool> GenerateCsv(Message message, IFundingLineCsvTransform publishedProviderCsvTransform);

        private void EnsureFileIsNew(string path)
        {
            if (_fileSystemAccess.Exists(path))
            {
                _fileSystemAccess.Delete(path);
            }
        }
    }
}
