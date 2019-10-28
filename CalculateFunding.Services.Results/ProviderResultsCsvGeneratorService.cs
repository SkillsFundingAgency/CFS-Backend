using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
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
        private readonly ILogger _logger;
        private readonly IBlobClient _blobClient;
        private readonly ICalculationResultsRepository _resultsRepository;
        private readonly ICsvUtils _csvUtils;
        private readonly IProverResultsToCsvRowsTransformation _resultsToCsvRowsTransformation;
        private readonly Policy _blobClientPolicy;
        private readonly Policy _resultsRepositoryPolicy;

        public ProviderResultsCsvGeneratorService(ILogger logger, 
            IBlobClient blobClient, 
            ICalculationResultsRepository resultsRepository,
            IResultsResiliencePolicies policies,
            ICsvUtils csvUtils, 
            IProverResultsToCsvRowsTransformation resultsToCsvRowsTransformation)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(resultsToCsvRowsTransformation, nameof(resultsToCsvRowsTransformation));
            Guard.ArgumentNotNull(policies?.BlobClient, nameof(policies.BlobClient));
            Guard.ArgumentNotNull(policies?.ResultsRepository, nameof(policies.ResultsRepository));
            
            _logger = logger;
            _blobClient = blobClient;
            _resultsRepository = resultsRepository;
            _blobClientPolicy = policies.BlobClient;
            _resultsRepositoryPolicy = policies.ResultsRepository;
            _csvUtils = csvUtils;
            _resultsToCsvRowsTransformation = resultsToCsvRowsTransformation;
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
            
            List<dynamic> providerResultRows = new List<dynamic>();

            await _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.ProviderResultsBatchProcessing(specificationId, 
                providerResults =>
                {
                    providerResultRows.AddRange( _resultsToCsvRowsTransformation.TransformProviderResultsIntoCsvRows(providerResults));
                    
                    return Task.CompletedTask;
                })
            );

            ICloudBlob blob = _blobClient.GetBlockBlobReference($"calculation-results-{specificationId}");
             
            string csv = _csvUtils.AsCsv(providerResultRows);

            await _blobClientPolicy.ExecuteAsync(() => _blobClient.UploadAsync(blob, csv));
        }
    }
}