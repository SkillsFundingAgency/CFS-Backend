using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.AzureStorage;
using CalculateFunding.Services.Providers.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Providers
{
    public class ProviderFundingVersionService: IProviderFundingVersionService
    {
        private const int CACHE_DURATION = 7;
        private readonly ICacheProvider _cacheProvider;
        private readonly IBlobClient _blobClient;
        private readonly Policy _blobClientPolicy;
        private readonly ILogger _logger;          
       

        public ProviderFundingVersionService(ICacheProvider cacheProvider,
            IBlobClient blobClient,
            ILogger logger,
            IProvidersResiliencePolicies resiliencePolicies
            )
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(blobClient, nameof(blobClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));

            _cacheProvider = cacheProvider;
            _blobClient = blobClient;
            _logger = logger;
            _blobClientPolicy = resiliencePolicies.BlobRepositoryPolicy;

        }

        public async Task<IActionResult> GetProviderFundingVersions(string providerFundingVersion)
        {
            if (string.IsNullOrWhiteSpace(providerFundingVersion))
            {
                return new BadRequestObjectResult("Null or empty id provided.");
            }

            string blobName = $"{providerFundingVersion}.json";

            bool exists = await _blobClientPolicy.ExecuteAsync(() => _blobClient.BlobExistsAsync(blobName));

            if (!exists)
            {
                _logger.Error($"Blob '{blobName}' does not exist.");

                return new NotFoundResult();
            }

            string template = string.Empty;

            try
            {
                ICloudBlob blob = await _blobClientPolicy.ExecuteAsync(() => _blobClient.GetBlobReferenceFromServerAsync(blobName));

                using (Stream blobStream = await _blobClientPolicy.ExecuteAsync(() => _blobClient.DownloadToStreamAsync(blob)))
                {
                    using (StreamReader streamReader = new StreamReader(blobStream))
                    {
                        template = await streamReader.ReadToEndAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                string errorMessage = $"Failed to fetch blob '{blobName}' from azure storage";

                _logger.Error(ex, errorMessage);

                return new InternalServerErrorResult(errorMessage);
            }

            return new OkObjectResult(template);
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) blobHealth = await _blobClient.IsHealthOk();
            (bool Ok, string Message) cacheHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ProviderFundingVersionService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = blobHealth.Ok, DependencyName = _blobClient.GetType().GetFriendlyName(), Message = blobHealth.Message });
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheHealth.Message });

            return health;
        }
    }
}
