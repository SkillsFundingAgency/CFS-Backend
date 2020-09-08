using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Caching;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationFundingLineQueryService : ICalculationFundingLineQueryService
    {
        private readonly IPoliciesApiClient _templates;
        private readonly AsyncPolicy _policyResilience;
        private readonly ICalculationsRepository _calculations;
        private readonly AsyncPolicy _calculationsResilience;
        private readonly ICacheProvider _cache;
        private readonly AsyncPolicy _cacheResilience;
        private readonly ISpecificationsApiClient _specifications;
        private readonly AsyncPolicy _specificationsResilience;
        
        public CalculationFundingLineQueryService(IPoliciesApiClient templates,
            ICalculationsRepository calculations,
            ICacheProvider cache,
            ISpecificationsApiClient specifications,
            ICalcsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(templates, nameof(templates));
            Guard.ArgumentNotNull(calculations, nameof(calculations));
            Guard.ArgumentNotNull(cache, nameof(cache));
            Guard.ArgumentNotNull(specifications, nameof(specifications));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies.CalculationsRepository, nameof(resiliencePolicies.CalculationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies.CacheProviderPolicy, nameof(resiliencePolicies.CacheProviderPolicy));
            Guard.ArgumentNotNull(resiliencePolicies.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            
            _templates = templates;
            _calculations = calculations;
            _cache = cache;
            _specifications = specifications;
            _policyResilience = resiliencePolicies.PoliciesApiClient;
            _calculationsResilience = resiliencePolicies.CalculationsRepository;
            _cacheResilience = resiliencePolicies.CacheProviderPolicy;
            _specificationsResilience = resiliencePolicies.SpecificationsApiClient;
        }

        public async Task<IActionResult> GetCalculationFundingLines(string calculationId)
        {
            Guard.IsNullOrWhiteSpace(calculationId, nameof(calculationId));

            Calculation calculation = await GetCalculation(calculationId);

            if (calculation == null)
            {
                return new NotFoundResult();
            }

            string specificationId = calculation.SpecificationId;

            string cacheKey = GetCacheKey(calculationId, specificationId);

            if (await CacheContains(cacheKey))
            {
                return new OkObjectResult(await GetCachedCalculationFundingLines(cacheKey));
            }
            
            SpecificationSummary specificationSummary = await GetSpecificationSummary(specificationId);

            Guard.ArgumentNotNull(specificationSummary, nameof(specificationSummary));

            string fundingStreamId = calculation.FundingStreamId;

            if (specificationSummary.TemplateIds == null || !specificationSummary.TemplateIds.TryGetValue(fundingStreamId, out string templateVersion))
            {
                throw new ArgumentOutOfRangeException(nameof(fundingStreamId), 
                    $"Specification {specificationId} does not contain a template version for the funding stream {fundingStreamId}");
            }

            TemplateMetadataContents template = await GetTemplate(fundingStreamId,
                specificationSummary.FundingPeriod?.Id,
                templateVersion);
            
            Guard.ArgumentNotNull(template, nameof(template));

            TemplateMapping templateMapping = await GetTemplateMapping(fundingStreamId,
                specificationId);
            
            Guard.ArgumentNotNull(templateMapping, nameof(templateMapping));

            uint? templateId = templateMapping.TemplateMappingItems?.SingleOrDefault(_ => _.CalculationId == calculationId)?.TemplateId;

            Guard.Ensure(templateId.HasValue, $"Did not locate a template mapping item for CalculationId {calculationId}");

            IEnumerable<CalculationFundingLine> calculationFundingLines = GetCalculationFundingLines(template, templateId.GetValueOrDefault());

            await CacheCalculationFundingLines(cacheKey, calculationFundingLines.ToArray());
            
            return new OkObjectResult(calculationFundingLines);
        }

        private async Task<bool> CacheContains(string cacheKey)
            => await _cacheResilience.ExecuteAsync(() => _cache.KeyExists<CalculationFundingLine[]>(cacheKey));

        private async Task<IEnumerable<CalculationFundingLine>> GetCachedCalculationFundingLines(string cacheKey)
            => await _cacheResilience.ExecuteAsync(() => _cache.GetAsync<CalculationFundingLine[]>(cacheKey));

        private async Task CacheCalculationFundingLines(string cacheKey,
            CalculationFundingLine[] calculationFundingLines)
            => await _cacheResilience.ExecuteAsync(() => _cache.SetAsync(cacheKey, calculationFundingLines));

        private static string GetCacheKey(string calculationId,
            string specificationId)
            => $"{CacheKeys.CalculationFundingLines}{specificationId}:{calculationId}";
        
        private async Task<TemplateMapping> GetTemplateMapping(string fundingStreamId,
            string specificationId)
            => await _calculationsResilience.ExecuteAsync(() => _calculations.GetTemplateMapping(specificationId,
                fundingStreamId));

        private async Task<TemplateMetadataContents> GetTemplate(string fundingStreamId,
            string fundingPeriodId,
            string templateVersion)
            => (await _policyResilience.ExecuteAsync(() => _templates.GetFundingTemplateContents(fundingStreamId, 
                fundingPeriodId, 
                templateVersion)))?.Content;

        private async Task<SpecificationSummary> GetSpecificationSummary(string specificationId)
            => (await _specificationsResilience.ExecuteAsync(() => _specifications.GetSpecificationSummaryById(specificationId)))?.Content;

        private async Task<Calculation> GetCalculation(string calculationId)
            => await _calculationsResilience.ExecuteAsync(() => _calculations.GetCalculationById(calculationId));
        
        public IEnumerable<CalculationFundingLine> GetCalculationFundingLines(TemplateMetadataContents template,
            uint templateId)
            => template.RootFundingLines?
                .Where(_ => ContainsCalculation(_, templateId))
                .Select(_ => new CalculationFundingLine
                {
                    TemplateId = _.TemplateLineId,
                    Name = _.Name
                })
                .ToArray();

        private static bool ContainsCalculation(TemplateFundingLine fundingLine,
            uint templateId)
        {
            foreach (TemplateCalculation calculation in fundingLine.Calculations ?? ArraySegment<TemplateCalculation>.Empty)
            {
                if (ContainsCalculation(calculation, templateId))
                {
                    return true;
                }   
            }

            foreach (TemplateFundingLine childFundingLine in fundingLine.FundingLines ?? ArraySegment<TemplateFundingLine>.Empty)
            {
                if (ContainsCalculation(childFundingLine, templateId))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool ContainsCalculation(TemplateCalculation calculation,
            uint templateId)
        {
            if (calculation.TemplateCalculationId == templateId)
            {
                return true;
            }

            foreach (TemplateCalculation childCalculation in calculation.Calculations ?? ArraySegment<TemplateCalculation>.Empty)
            {
                if (ContainsCalculation(childCalculation, templateId))
                {
                    return true;
                }        
            }

            return false;
        }
    }
}