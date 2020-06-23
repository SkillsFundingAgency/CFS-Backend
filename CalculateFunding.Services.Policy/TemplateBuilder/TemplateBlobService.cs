using System;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Policy.Models;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Policy.TemplateBuilder
{
    public class TemplateBlobService : ITemplateBlobService
    {
        private readonly AsyncPolicy _fundingTemplateRepositoryPolicy;
        private readonly IFundingTemplateRepository _fundingTemplateRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly AsyncPolicy _cacheProviderPolicy;
        private readonly ILogger _logger;

        public TemplateBlobService(
            IPolicyResiliencePolicies policyResiliencePolicies,
            IFundingTemplateRepository fundingTemplateRepository,
            ICacheProvider cacheProvider,
            ILogger logger)
        {
            
            _fundingTemplateRepository = fundingTemplateRepository;
            _fundingTemplateRepositoryPolicy = policyResiliencePolicies.FundingTemplateRepository;
            _cacheProvider = cacheProvider;
            _cacheProviderPolicy = policyResiliencePolicies.CacheProvider;
            _logger = logger;
        }

        public async Task<CommandResult> PublishTemplate(Template template)
        {
            string templateVersion = $"{template.Current.MajorVersion}.{template.Current.MinorVersion}";
            string blobName = $"{template.FundingStream.Id}/{template.FundingPeriod.Id}/{templateVersion}.json";
            try
            {
                byte[] templateFileBytes = Encoding.UTF8.GetBytes(template.Current.TemplateJson);

                try
                {
                    await _fundingTemplateRepositoryPolicy.ExecuteAsync(() => _fundingTemplateRepository.SaveFundingTemplateVersion(blobName, templateFileBytes));
                }
                catch (Exception ex)
                {
                    throw new NonRetriableException($"Failed to save funding template version: '{blobName}'", ex);
                }

                return await ClearTemplateBlobCache(template, templateVersion);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to save funding template '{blobName}' to blob storage");

                return CommandResult.Fail("Error occurred uploading funding template to blob storage: " + ex.Message);
            }
        }

        private async Task<CommandResult> ClearTemplateBlobCache(Template template, string templateVersion)
        {
            string cacheKey =
                $"{CacheKeys.FundingTemplatePrefix}{template.FundingStream.Id}-{template.FundingPeriod.Id}-{templateVersion}"
                    .ToLowerInvariant();
            Task task1 = _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<string>(cacheKey));
            Task task2 = _cacheProvider.RemoveAsync<FundingTemplateContents>(
                $"{CacheKeys.FundingTemplateContents}{template.FundingStream.Id}:{template.FundingPeriod.Id}:{templateVersion}"
                    .ToLowerInvariant());
            Task task3 = _cacheProvider.RemoveAsync<TemplateMetadataContents>(
                $"{CacheKeys.FundingTemplateContentMetadata}{template.FundingStream.Id}:{template.FundingPeriod.Id}:{templateVersion}"
                    .ToLowerInvariant());
            await task1;
            await task2;
            await task3;
            return CommandResult.Success();
        }
    }
}