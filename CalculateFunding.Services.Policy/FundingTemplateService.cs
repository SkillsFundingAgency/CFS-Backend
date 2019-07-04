using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Storage;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Policy
{
    public class FundingTemplateService : IFundingTemplateService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly IFundingTemplateRepository _fundingTemplateRepository;
        private readonly Polly.Policy _fundingTemplateRepositoryPolicy;
        private readonly IFundingTemplateValidationService _fundingTemplateValidationService;
        private readonly ICacheProvider _cacheProvider;
        private readonly Polly.Policy _cacheProviderPolicy;

        public FundingTemplateService(
            ILogger logger, 
            IFundingTemplateRepository fundingTemplateRepository,
            IPolicyResilliencePolicies policyResilliencePolicies,
            IFundingTemplateValidationService fundingTemplateValidationService,
            ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(fundingTemplateRepository, nameof(fundingTemplateRepository));
            Guard.ArgumentNotNull(policyResilliencePolicies, nameof(policyResilliencePolicies));
            Guard.ArgumentNotNull(fundingTemplateValidationService, nameof(fundingTemplateValidationService));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));

            _logger = logger;
            _fundingTemplateRepository = fundingTemplateRepository;
            _fundingTemplateRepositoryPolicy = policyResilliencePolicies.FundingTemplateRepository;
            _fundingTemplateValidationService = fundingTemplateValidationService;
            _cacheProvider = cacheProvider;
            _cacheProviderPolicy = policyResilliencePolicies.CacheProvider;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            (bool Ok, string Message) fundingTemplateRepoHealth = await ((BlobClient)_fundingTemplateRepository).IsHealthOk();
            ServiceHealth fundingTemplateValidationServiceHealth = await ((IHealthChecker)_fundingTemplateValidationService).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(FundingSchemaService)
            };

            health.Dependencies.Add(new DependencyHealth { HealthOk = fundingTemplateRepoHealth.Ok, DependencyName = fundingTemplateRepoHealth.GetType().GetFriendlyName(), Message = fundingTemplateRepoHealth.Message });
            health.Dependencies.AddRange(fundingTemplateValidationServiceHealth.Dependencies);

            return health;
        }

        public async Task<IActionResult> SaveFundingTemplate(string actionName, string controllerName, HttpRequest request)
        {
            Guard.IsNullOrWhiteSpace(actionName, nameof(actionName));
            Guard.IsNullOrWhiteSpace(controllerName, nameof(controllerName));
            Guard.ArgumentNotNull(request, nameof(request));

            string template = await request.GetRawBodyStringAsync();

            //Already checked for null above when getting body
            if (template.Trim() == string.Empty)
            {
                return new BadRequestObjectResult("Null or empty funding template was provided.");
            }

            FundingTemplateValidationResult validationResult = await _fundingTemplateValidationService.ValidateFundingTemplate(template);

            if (!validationResult.IsValid)
            {
                return new BadRequestObjectResult(validationResult.ValidationState);
            }

            string blobName = $"{validationResult.FundingStreamId}/{validationResult.Version}.json";

            try
            {
                byte[] templateFileBytes = Encoding.UTF8.GetBytes(template);

                await SaveFundingTemplateVersion(blobName, templateFileBytes);

                string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{validationResult.FundingStreamId}-{validationResult.Version}";

                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, template));

                return new CreatedAtActionResult(actionName, controllerName, new { fundingStreamId = validationResult.FundingStreamId, templateVersion = validationResult.Version }, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to save funding template '{blobName}' to blob storage");

                return new InternalServerErrorResult("Error occurred uploading funding template");
            }
        }

        public async Task<IActionResult> GetFundingTemplate(string fundingStreamId, string templateVersion)
        {
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(templateVersion, nameof(templateVersion));

            string cacheKey = $"{CacheKeys.FundingTemplatePrefix}{fundingStreamId}-{templateVersion}";

            string template = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<string>(cacheKey));

            if (!string.IsNullOrWhiteSpace(template))
            {
                return new OkObjectResult(template);
            }

            string blobName = $"{fundingStreamId}/{templateVersion}.json";

            try
            {
                bool versionExists = await CheckIfFundingTemplateVersionExists(blobName);

                if (!versionExists)
                {
                    return new NotFoundResult();
                }

                template = await GetFundingTemplateVersion(blobName);

                if (string.IsNullOrWhiteSpace(template))
                {
                    _logger.Error($"Empty template returned from blob storage for blob name '{blobName}'");
                    return new InternalServerErrorResult($"Failed to retreive blob contents for funding stream id '{fundingStreamId}' funding template version '{templateVersion}'");
                }

                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, template));

                return new OkObjectResult(template);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to fetch funding template '{blobName}' from blob storage");

                return new InternalServerErrorResult($"Error occurred fetching funding template for funding stream id '{fundingStreamId}' and version '{templateVersion}'");
            }
        }

        private async Task SaveFundingTemplateVersion(string blobName, byte[] templateBytes)
        {
            try
            {
                await _fundingTemplateRepositoryPolicy.ExecuteAsync(() => _fundingTemplateRepository.SaveFundingTemplateVersion(blobName, templateBytes));
            }
            catch (Exception ex)
            {
                throw new NonRetriableException($"Failed to save funding template version: '{blobName}'", ex);
            }
        }

        private async Task<bool> CheckIfFundingTemplateVersionExists(string blobName)
        {
            try
            {
                return await _fundingTemplateRepositoryPolicy.ExecuteAsync(() => _fundingTemplateRepository.TemplateVersionExists(blobName));
            }
            catch (Exception ex)
            {
                _logger.Error($"Failed to check if funding template version: '{blobName}' exists");

                throw new NonRetriableException($"Failed to check if funding template version: '{blobName}' exists", ex);
            }
        }

        private async Task<string> GetFundingTemplateVersion(string blobName)
        {
            try
            {
                return await _fundingTemplateRepositoryPolicy.ExecuteAsync(() => _fundingTemplateRepository.GetFundingTemplateVersion(blobName));
            }
            catch (Exception ex)
            {
                throw new NonRetriableException($"Failed to get funding template version: '{blobName}' from blob storage", ex);
            }
        }
    }
}
