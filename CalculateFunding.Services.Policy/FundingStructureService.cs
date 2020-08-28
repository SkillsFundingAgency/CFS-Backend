using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Results.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.Search;
using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Models.Policy.FundingPolicy.ViewModels;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Polly;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Policy
{
    public class FundingStructureService : IFundingStructureService
    {
        private readonly IValidator<UpdateFundingStructureLastModifiedRequest> _validator;
        private readonly IFundingTemplateService _fundingTemplateService;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly ICalculationsApiClient _calculationsApiClient;
        private readonly IResultsApiClient _resultsApiClient;
        private readonly AsyncPolicy _cacheResilience;
        private readonly AsyncPolicy _specificationsResilience;
        private readonly AsyncPolicy _calculationsResilience;
        private readonly AsyncPolicy _resultsResilience;

        public FundingStructureService(ICacheProvider cacheProvider,
            ISpecificationsApiClient specificationsApiClient,
            ICalculationsApiClient calculationsApiClient,
            IResultsApiClient resultsApiClient,
            IFundingTemplateService fundingTemplateService,
            IValidator<UpdateFundingStructureLastModifiedRequest> validator,
            IPolicyResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(validator, nameof(validator));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies?.CacheProvider, nameof(resiliencePolicies.CacheProvider));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsApiClient, nameof(resiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.ResultsApiClient, nameof(resiliencePolicies.ResultsApiClient));

            _cacheProvider = cacheProvider;
            _specificationsApiClient = specificationsApiClient;
            _calculationsApiClient = calculationsApiClient;
            _resultsApiClient = resultsApiClient;
            _fundingTemplateService = fundingTemplateService;
            _validator = validator;
            _cacheResilience = resiliencePolicies.CacheProvider;
            _specificationsResilience = resiliencePolicies.SpecificationsApiClient;
            _calculationsResilience = resiliencePolicies.CalculationsApiClient;
            _resultsResilience = resiliencePolicies.ResultsApiClient;
        }
        
        public async Task<IActionResult> UpdateFundingStructureLastModified(UpdateFundingStructureLastModifiedRequest request)
        {
            Guard.ArgumentNotNull(request, nameof(request));
            
            // ReSharper disable once MethodHasAsyncOverload
            ValidationResult validationResult = _validator.Validate(request);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }
            
            string cacheKey = GetCacheKeyFundingStructure(request.SpecificationId,
                request.FundingStreamId,
                request.FundingPeriodId);

            await _cacheResilience.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, request.LastModified));

            return new OkResult();
        }

        public async Task<DateTimeOffset> GetFundingStructureTimeStamp(string fundingStreamId,
            string fundingPeriodId,
            string specificationId)
        {
            
            string cacheKey = GetCacheKeyFundingStructure(specificationId, fundingStreamId, fundingPeriodId);

            DateTimeOffset? timestamp = await _cacheResilience.ExecuteAsync(() => _cacheProvider.GetAsync<DateTimeOffset?>(cacheKey));

            return timestamp.GetValueOrDefault();
        }

        private static string GetCacheKeyFundingStructure(string specificationId,
            string fundingStreamId,
            string fundingPeriodId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));
            Guard.IsNullOrWhiteSpace(fundingPeriodId, nameof(fundingPeriodId));

            return $"{CacheKeys.FundingLineStructureTimestamp}{specificationId}:{fundingStreamId}:{fundingPeriodId}";
        }

        //this was lifted pretty much as is from the FE
        public async Task<IActionResult> GetFundingStructure(string fundingStreamId,
            string fundingPeriodId,
            string specificationId)
        {
            ApiResponse<SpecificationSummary> specificationSummaryApiResponse =
		        await _specificationsResilience.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));
	        IActionResult specificationSummaryApiResponseErrorResult =
		        specificationSummaryApiResponse.IsSuccessOrReturnFailureResult("GetSpecificationSummaryById");
	        if (specificationSummaryApiResponseErrorResult != null)
	        {
		        return specificationSummaryApiResponseErrorResult;
	        }

	        string templateVersion = specificationSummaryApiResponse.Content.TemplateIds.ContainsKey(fundingStreamId)
		        ? specificationSummaryApiResponse.Content.TemplateIds[fundingStreamId]
		        : null;
	        if (templateVersion == null)
		        return new InternalServerErrorResult(
			        $"Specification contains no matching template version for funding stream '{fundingStreamId}'");

            TemplateMetadataContents fundingTemplateContents =
                (await _fundingTemplateService.GetFundingTemplateContents(fundingStreamId, fundingPeriodId, templateVersion) as OkObjectResult)
                .Value as TemplateMetadataContents;
            
	        if (fundingTemplateContents == null)
	        {
		        return new InternalServerErrorResult($"Unable to locate funding template contents for {fundingStreamId} {fundingPeriodId} {templateVersion}");
	        }

	        ApiResponse<TemplateMapping> templateMappingResponse =
		        await _calculationsResilience.ExecuteAsync(() => _calculationsApiClient.GetTemplateMapping(specificationId, fundingStreamId));
	        IActionResult templateMappingResponseErrorResult =
		        templateMappingResponse.IsSuccessOrReturnFailureResult("GetTemplateMapping");
	        if (templateMappingResponseErrorResult != null)
	        {
		        return templateMappingResponseErrorResult;
	        }

	        ApiResponse<IEnumerable<CalculationMetadata>> calculationMetadata =
		        await _calculationsResilience.ExecuteAsync(() => _calculationsApiClient.GetCalculationMetadataForSpecification(specificationId));
	        IActionResult calculationMetadataErrorResult =
		        calculationMetadata.IsSuccessOrReturnFailureResult("calculationMetadata");
	        if (calculationMetadataErrorResult != null)
	        {
		        return calculationMetadataErrorResult;
	        }

	        List<FundingStructureItem> fundingStructures = new List<FundingStructureItem>();
	        RecursivelyAddFundingLineToFundingStructure(
		        fundingStructures,
                fundingTemplateContents.RootFundingLines,
		        templateMappingResponse.Content.TemplateMappingItems.ToList(),
		        calculationMetadata.Content.ToList(),
		        null,
                null);

            FundingStructure fundingStructure = new FundingStructure
            {
                Items = fundingStructures,
                LastModified = await GetFundingStructureTimeStamp(fundingStreamId,
                    fundingPeriodId,
                    specificationId)
            };
            
            return new OkObjectResult(fundingStructure);
        }

        //this was lifted pretty much as is from the FE
        public async Task<IActionResult> GetFundingStructureWithCalculationResults(string fundingStreamId,
            string fundingPeriodId,
            string specificationId,
            string providerId = null) 
        {
            ApiResponse<SpecificationSummary> specificationSummaryApiResponse =
                await _specificationsResilience.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));
            IActionResult specificationSummaryApiResponseErrorResult =
                specificationSummaryApiResponse.IsSuccessOrReturnFailureResult("GetSpecificationSummaryById");
            if (specificationSummaryApiResponseErrorResult != null)
            {
                return specificationSummaryApiResponseErrorResult;
            }

            string templateVersion = specificationSummaryApiResponse.Content.TemplateIds.ContainsKey(fundingStreamId)
                ? specificationSummaryApiResponse.Content.TemplateIds[fundingStreamId]
                : null;
            if (templateVersion == null)
                return new InternalServerErrorResult(
                    $"Specification contains no matching template version for funding stream '{fundingStreamId}'");

            TemplateMetadataContents fundingTemplateContents =
                (await _fundingTemplateService.GetFundingTemplateContents(fundingStreamId, fundingPeriodId, templateVersion) as OkObjectResult)
                .Value as TemplateMetadataContents;
            
            if (fundingTemplateContents == null)
            {
                return new InternalServerErrorResult($"Unable to locate funding template contents for {fundingStreamId} {fundingPeriodId} {templateVersion}");
            }

            ApiResponse<TemplateMapping> templateMappingResponse =
                await _calculationsResilience.ExecuteAsync(() => _calculationsApiClient.GetTemplateMapping(specificationId, fundingStreamId));
            IActionResult templateMappingResponseErrorResult =
                templateMappingResponse.IsSuccessOrReturnFailureResult("GetTemplateMapping");
            if (templateMappingResponseErrorResult != null)
            {
                return templateMappingResponseErrorResult;
            }

            ApiResponse<IEnumerable<CalculationMetadata>> calculationMetadata =
                await _calculationsResilience.ExecuteAsync(() => _calculationsApiClient.GetCalculationMetadataForSpecification(specificationId));
            IActionResult calculationMetadataErrorResult =
                calculationMetadata.IsSuccessOrReturnFailureResult("calculationMetadata");
            if (calculationMetadataErrorResult != null)
            {
                return calculationMetadataErrorResult;
            }

            ApiResponse<CalculationProviderResultSearchResults> calculationProviderResultsResponse = null;
            ApiResponse<ProviderResultResponse> providerResultResponse = null;

            if (providerId.IsNullOrWhitespace())
            {
                calculationProviderResultsResponse =
                    await _resultsResilience.ExecuteAsync(() => _resultsApiClient.SearchCalculationProviderResults(new SearchModel
                    {
                        PageNumber = 1,
                        Top = 10000,
                        SearchTerm = "",
                        IncludeFacets = false,
                        Filters = new Dictionary<string, string[]>
                        {
                            {
                                "specificationId", new[]
                                {
                                    specificationId
                                }
                            }
                        }
                    }));
                IActionResult calculationProviderResultsErrorResult =
                    calculationProviderResultsResponse.IsSuccessOrReturnFailureResult("SearchCalculationProviderResults");
                if (calculationProviderResultsErrorResult != null)
                {
                    return calculationProviderResultsErrorResult;
                }
            }
            else
            {
                providerResultResponse =
                    await _resultsResilience.ExecuteAsync(() => _resultsApiClient.GetProviderResults(providerId, specificationId));
                    
                IActionResult providerResultResponseErrorResult = providerResultResponse.IsSuccessOrReturnFailureResult("GetProviderResults");
                if (providerResultResponseErrorResult != null)
                {
                    return providerResultResponseErrorResult;
                }
            }

            List<FundingStructureItem> fundingStructures = new List<FundingStructureItem>();
            RecursivelyAddFundingLineToFundingStructure(
                fundingStructures,
                fundingTemplateContents.RootFundingLines,
                templateMappingResponse.Content.TemplateMappingItems.ToList(),
                calculationMetadata.Content.ToList(),
                providerResultResponse?.Content,
                calculationProviderResultsResponse?.Content);

            FundingStructure fundingStructure = new FundingStructure
            {
                Items = fundingStructures,
                LastModified = await GetFundingStructureTimeStamp(fundingStreamId,
                    fundingPeriodId,
                    specificationId)
            };
            
            return new OkObjectResult(fundingStructure);
        }

        private static FundingStructureItem RecursivelyAddFundingLines(IEnumerable<FundingLine> fundingLines,
            List<TemplateMappingItem> templateMappingItems,
            List<CalculationMetadata> calculationMetadata,
            int level,
            FundingLine fundingLine,
            ProviderResultResponse providerResult,
            CalculationProviderResultSearchResults calculationProviderResultSearchResults)
        {
            level++;

            List<FundingStructureItem> innerFundingStructureItems = new List<FundingStructureItem>();

            // If funding line has calculations, recursively add them to list of inner FundingStructureItems
            if (fundingLine.Calculations != null && fundingLine.Calculations.Any())
            {
                foreach (TemplateCalculation calculation in fundingLine.Calculations)
                {
                    innerFundingStructureItems.Add(
                        RecursivelyMapCalculationsToFundingStructureItem(
                            calculation,
                            level,
                            templateMappingItems,
                            calculationMetadata,
                            providerResult,
                            calculationProviderResultSearchResults));
                }
            }

            // If funding line has more funding lines, recursively add them to list of inner FundingStructureItems
            if (fundingLine.FundingLines != null && fundingLine.FundingLines.Any())
            {
                foreach (FundingLine line in fundingLines)
                {
                    innerFundingStructureItems.Add(RecursivelyAddFundingLines(
                        line.FundingLines,
                        templateMappingItems,
                        calculationMetadata,
                        level,
                        line,
                        providerResult,
                        calculationProviderResultSearchResults));
                }
            }

            FundingLineResult fundingLineResult = providerResult?.FundingLineResults?.FirstOrDefault(_ => _.FundingLine.Id == fundingLine.TemplateLineId.ToString());
            string calculationValue = null;
            if (fundingLineResult != null)
            {
                CalculationValueFormat calculationValueTypeViewModel = CalculationValueFormat.Number;
                calculationValue = fundingLineResult.Value.AsFormatCalculationType(calculationValueTypeViewModel);
            }

            // Add FundingStructureItem
            FundingStructureItem fundingStructureItem = MapToFundingStructureItem(
                level,
                fundingLine.Name,
                FundingStructureType.FundingLine,
                null,
                null,
                null,
                innerFundingStructureItems.Any() ? innerFundingStructureItems : null,
                calculationValue);

            return fundingStructureItem;
        }

        private static void RecursivelyAddFundingLineToFundingStructure(List<FundingStructureItem> fundingStructures,
            IEnumerable<FundingLine> fundingLines,
            List<TemplateMappingItem> templateMappingItems,
            List<CalculationMetadata> calculationMetadata,
            ProviderResultResponse providerResult,
            CalculationProviderResultSearchResults calculationProviderResultSearchResults,
            int level = 0) =>
            fundingStructures.AddRange(fundingLines.Select(fundingLine =>
                RecursivelyAddFundingLines(
                    fundingLine.FundingLines,
                    templateMappingItems,
                    calculationMetadata,
                    level,
                    fundingLine,
                    providerResult,
                    calculationProviderResultSearchResults)));


        private static FundingStructureItem MapToFundingStructureItem(int level,
            string name,
            FundingStructureType type,
            string calculationType = null,
            string calculationId = null,
            string calculationPublishStatus = null,
            List<FundingStructureItem> fundingStructureItems = null,
            string value = null,
            DateTimeOffset? lastUpdatedDate = null) =>
            new FundingStructureItem(
                level,
                name,
                calculationId,
                calculationPublishStatus,
                type,
                calculationType,
                fundingStructureItems,
                value,
                lastUpdatedDate);

        private static FundingStructureItem RecursivelyMapCalculationsToFundingStructureItem(TemplateCalculation calculation,
            int level,
            List<TemplateMappingItem> templateMappingItems,
            List<CalculationMetadata> calculationMetadata,
            ProviderResultResponse providerResult,
            CalculationProviderResultSearchResults calculationProviderResultSearchResults)
        {
            level++;

            List<FundingStructureItem> innerFundingStructureItems = null;

            string calculationId = GetCalculationId(calculation, templateMappingItems);
            string calculationPublishStatus = calculationMetadata.FirstOrDefault(_ => _.CalculationId == calculationId)?
                .PublishStatus.ToString();
            CalculationResultResponse calculationResult = providerResult?.CalculationResults.FirstOrDefault(_ => _.Calculation.Id == calculationId);

            string calculationType = null;
            string calculationValue = null;

            if (calculationResult != null)
            {
                calculationType = calculationResult.CalculationValueType.ToString();
                CalculationValueFormat calculationValueTypeViewModel = calculationType.AsEnum<CalculationValueFormat>();
                calculationValue = calculationResult.Value.AsFormatCalculationType(calculationValueTypeViewModel);
            }

            DateTimeOffset? lastUpdatedDate = calculationProviderResultSearchResults?
                .Results?
                .FirstOrDefault(c => c.Id == calculationId)?
                .LastUpdatedDate;

            if (calculation.Calculations != null && calculation.Calculations.Any())
            {
                innerFundingStructureItems = calculation.Calculations.Select(innerCalculation =>
                        RecursivelyMapCalculationsToFundingStructureItem(
                            innerCalculation,
                            level,
                            templateMappingItems,
                            calculationMetadata,
                            providerResult,
                            calculationProviderResultSearchResults))
                    .ToList();
            }

            return MapToFundingStructureItem(
                level,
                calculation.Name,
                FundingStructureType.Calculation,
                calculationType,
                calculationId,
                calculationPublishStatus,
                innerFundingStructureItems,
                calculationValue,
                lastUpdatedDate);
        }

        private static string GetCalculationId(
            TemplateCalculation calculation,
            IEnumerable<TemplateMappingItem> templateMappingItems) =>
            templateMappingItems
                .FirstOrDefault(_ => _.TemplateId == calculation.TemplateCalculationId)?
                .CalculationId;
    }
}