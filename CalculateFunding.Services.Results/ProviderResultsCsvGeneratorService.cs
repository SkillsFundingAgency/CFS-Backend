using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching.FileSystem;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Results
{
    public class ProviderResultsCsvGeneratorService : IProviderResultsCsvGeneratorService
    {
        public const int BatchSize = 100;
        
        private readonly ILogger _logger;
        private readonly IBlobClient _blobClient;
        private readonly ICalculationResultsRepository _resultsRepository;
        private readonly ICsvUtils _csvUtils;
        private readonly IProverResultsToCsvRowsTransformation _resultsToCsvRowsTransformation;
        private readonly IFileSystemAccess _fileSystemAccess;
        private readonly IFileSystemCacheSettings _fileSystemCacheSettings;
        private readonly Policy _blobClientPolicy;
        private readonly Policy _resultsRepositoryPolicy;

        public ProviderResultsCsvGeneratorService(ILogger logger,
            IBlobClient blobClient,
            ICalculationResultsRepository resultsRepository,
            IResultsResiliencePolicies policies,
            ICsvUtils csvUtils,
            IProverResultsToCsvRowsTransformation resultsToCsvRowsTransformation,
            IFileSystemAccess fileSystemAccess,
            IFileSystemCacheSettings fileSystemCacheSettings)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(resultsToCsvRowsTransformation, nameof(resultsToCsvRowsTransformation));
            Guard.ArgumentNotNull(fileSystemAccess, nameof(fileSystemAccess));
            Guard.ArgumentNotNull(policies?.BlobClient, nameof(policies.BlobClient));
            Guard.ArgumentNotNull(policies?.ResultsRepository, nameof(policies.ResultsRepository));
            Guard.ArgumentNotNull(fileSystemCacheSettings, nameof(fileSystemCacheSettings));

            _logger = logger;
            _blobClient = blobClient;
            _resultsRepository = resultsRepository;
            _blobClientPolicy = policies.BlobClient;
            _resultsRepositoryPolicy = policies.ResultsRepository;
            _csvUtils = csvUtils;
            _resultsToCsvRowsTransformation = resultsToCsvRowsTransformation;
            _fileSystemAccess = fileSystemAccess;
            _fileSystemCacheSettings = fileSystemCacheSettings;
        }

        public async Task Run(Message message)
        {
            string specificationId = message.GetUserProperty<string>("specification-id");

            if (specificationId == null)
            {
                string error = "Specification id missing";

                _logger.Error(error);

                throw new NonRetriableException(error);
            }

            string temporaryFilePath = new CsvFilePath(_fileSystemCacheSettings.Path, specificationId);

            EnsureFileIsNew(temporaryFilePath);

            bool outputHeaders = true;
            
            await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.ProviderResultsBatchProcessing(specificationId,
                providerResults =>
                {
                    IEnumerable<ExpandoObject>csvRows = _resultsToCsvRowsTransformation.TransformProviderResultsIntoCsvRows(providerResults);

                    StreamWriter streamWriter = _csvUtils.AsCsvStream(csvRows, outputHeaders);

                    _fileSystemAccess.Append(temporaryFilePath, streamWriter.BaseStream)
                        .GetAwaiter()
                        .GetResult();
                    
                    _csvUtils.ReturnStreamWriter(streamWriter);

                    outputHeaders = false;
                    return Task.CompletedTask;
                }, BatchSize)
            );

            ICloudBlob blob = _blobClient.GetBlockBlobReference($"calculation-results-{specificationId}");

            using (Stream csvFileStream = _fileSystemAccess.OpenRead(temporaryFilePath))
            {
                await _blobClientPolicy.ExecuteAsync(() => _blobClient.UploadAsync(blob, csvFileStream));
            }
        }

        private void EnsureFileIsNew(string path)
        {
            if (_fileSystemAccess.Exists(path))
            {
                _fileSystemAccess.Delete(path);
            }
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