using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Common.ApiClient.Graph.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.TemplateMetadata.Enums;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Result;
using CalculateFunding.Models.Result.ViewModels;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Results.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Polly;
using CalcModels = CalculateFunding.Models.Calcs;
using Calculation = CalculateFunding.Common.ApiClient.Graph.Models.Calculation;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Results
{
    public class FundingStructureService : IFundingStructureService
    {
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly Common.ApiClient.Policies.IPoliciesApiClient _policiesApiClient;
        private readonly ICalculationsApiClient _calculationsApiClient;
        private readonly IGraphApiClient _graphApiClient;
        private readonly IProviderCalculationResultsSearchService _providerCalculationResultsSearchService;
        private readonly IResultsService _resultsService;
        private readonly ICacheProvider _cacheProvider;
        private readonly IValidator<UpdateFundingStructureLastModifiedRequest> _validator;

        private readonly AsyncPolicy _calculationsResilience;
        private readonly AsyncPolicy _specificationsResilience;
        private readonly AsyncPolicy _policiesResilience;
        private readonly AsyncPolicy _cacheResilience;

        public FundingStructureService(
            ICacheProvider cacheProvider,
            ISpecificationsApiClient specificationsApiClient,
            ICalculationsApiClient calculationsApiClient,
            IGraphApiClient graphApiClient,
            IProviderCalculationResultsSearchService providerCalculationResultsSearchService,
            IResultsService resultsService,
            Common.ApiClient.Policies.IPoliciesApiClient policiesApiClient,
            IValidator<UpdateFundingStructureLastModifiedRequest> validator,
            IResultsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(graphApiClient, nameof(graphApiClient));
            Guard.ArgumentNotNull(providerCalculationResultsSearchService, nameof(providerCalculationResultsSearchService));
            Guard.ArgumentNotNull(resultsService, nameof(resultsService));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(validator, nameof(validator));

            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsApiClient, nameof(resiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.CacheProvider, nameof(resiliencePolicies.CacheProvider));

            _specificationsApiClient = specificationsApiClient;
            _policiesApiClient = policiesApiClient;
            _calculationsApiClient = calculationsApiClient;
            _graphApiClient = graphApiClient;
            _providerCalculationResultsSearchService = providerCalculationResultsSearchService;
            _resultsService = resultsService;
            _cacheProvider = cacheProvider;
            _validator = validator;

            _specificationsResilience = resiliencePolicies.SpecificationsApiClient;
            _policiesResilience = resiliencePolicies.PoliciesApiClient;
            _calculationsResilience = resiliencePolicies.CalculationsApiClient;
            _cacheResilience = resiliencePolicies.CacheProvider;
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

            DateTimeOffset timestamp = await _cacheResilience.ExecuteAsync(() => _cacheProvider.GetAsync<DateTimeOffset>(cacheKey));

            return timestamp;
        }

        public async Task<IActionResult> GetFundingStructure(string fundingStreamId, string fundingPeriodId, string specificationId)
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

            ApiResponse<TemplateMetadataContents> templateMetadataContentsApiResponse =
                await _policiesResilience.ExecuteAsync(() => _policiesApiClient.GetFundingTemplateContents(fundingStreamId, fundingPeriodId, templateVersion));
            IActionResult templateMetadataContentsApiResponseErrorResult =
                templateMetadataContentsApiResponse.IsSuccessOrReturnFailureResult("GetFundingTemplateContents");
            if (templateMetadataContentsApiResponseErrorResult != null)
            {
                return templateMetadataContentsApiResponseErrorResult;
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

            List<string> calculationIdsWithError = new List<string>();

            ApiResponse<IEnumerable<Entity<Calculation>>> getCircularDependenciesApiResponse =
                await _graphApiClient.GetCircularDependencies(specificationId);
            IActionResult circularDependenciesApiErrorResult =
                getCircularDependenciesApiResponse.IsSuccessOrReturnFailureResult("GetCircularDependencies");
            if (circularDependenciesApiErrorResult == null)
            {
                calculationIdsWithError =
                    getCircularDependenciesApiResponse.Content.Select(calcs => calcs.Node.CalculationId).ToList();
            }

            List<FundingStructureItem> fundingStructures = new List<FundingStructureItem>();
            RecursivelyAddFundingLineToFundingStructure(
                fundingStructures,
                templateMetadataContentsApiResponse.Content.RootFundingLines,
                templateMappingResponse.Content.TemplateMappingItems.ToList(),
                calculationMetadata.Content.ToList(),
                null,
                null,
                calculationIdsWithError);

            FundingStructure fundingStructure = new FundingStructure
            {
                Items = fundingStructures,
                LastModified = await GetFundingStructureTimeStamp(fundingStreamId,
                    fundingPeriodId,
                    specificationId)
            };

            return new OkObjectResult(fundingStructure);
        }

        public async Task<IActionResult> GetFundingStructureWithCalculationResults(
            string fundingStreamId,
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

            ApiResponse<TemplateMetadataContents> templateMetadataContentsApiResponse =
                await _policiesResilience.ExecuteAsync(() => _policiesApiClient.GetFundingTemplateContents(fundingStreamId, fundingPeriodId, templateVersion));
            IActionResult templateMetadataContentsApiResponseErrorResult =
                templateMetadataContentsApiResponse.IsSuccessOrReturnFailureResult("GetFundingTemplateContents");
            if (templateMetadataContentsApiResponseErrorResult != null)
            {
                return templateMetadataContentsApiResponseErrorResult;
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

            CalcModels.ProviderResultResponse providerResultResponse = null;
            CalculationProviderResultSearchResults calculationProviderResultSearchResults = null;

            if (providerId.IsNullOrWhitespace())
            {
                IActionResult calculationProviderResultsResponseResult =
                    await _providerCalculationResultsSearchService.SearchCalculationProviderResults(new SearchModel
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
                    });

                if (!(calculationProviderResultsResponseResult is OkObjectResult))
                {
                    return calculationProviderResultsResponseResult;
                }

                calculationProviderResultSearchResults = (calculationProviderResultsResponseResult as OkObjectResult).Value
                    as CalculationProviderResultSearchResults;
            }
            else
            {
                IActionResult providerResultResponseResult = await _resultsService.GetProviderResults(providerId, specificationId);

                if (!(providerResultResponseResult is OkObjectResult))
                {
                    return providerResultResponseResult;
                }

                providerResultResponse = (providerResultResponseResult as OkObjectResult).Value
                    as CalcModels.ProviderResultResponse;
            }

            List<string> calculationIdsWithError = new List<string>();

            ApiResponse<IEnumerable<Entity<Calculation>>> getCircularDependenciesApiResponse =
                await _graphApiClient.GetCircularDependencies(specificationId);
            IActionResult circularDependenciesApiErrorResult =
                getCircularDependenciesApiResponse.IsSuccessOrReturnFailureResult("GetCircularDependencies");
            if (circularDependenciesApiErrorResult == null)
            {
                calculationIdsWithError =
                    getCircularDependenciesApiResponse.Content.Select(calcs => calcs.Node.CalculationId).ToList();
            }

            List<FundingStructureItem> fundingStructures = new List<FundingStructureItem>();
            RecursivelyAddFundingLineToFundingStructure(
                fundingStructures,
                templateMetadataContentsApiResponse.Content.RootFundingLines,
                templateMappingResponse.Content.TemplateMappingItems.ToList(),
                calculationMetadata.Content.ToList(),
                providerResultResponse,
                calculationProviderResultSearchResults,
                calculationIdsWithError);

            FundingStructure fundingStructure = new FundingStructure
            {
                Items = fundingStructures,
                LastModified = await GetFundingStructureTimeStamp(fundingStreamId,
                    fundingPeriodId,
                    specificationId)
            };

            return new OkObjectResult(fundingStructure);
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

        private static void RecursivelyAddFundingLineToFundingStructure(List<FundingStructureItem> fundingStructures,
            IEnumerable<FundingLine> fundingLines,
            List<TemplateMappingItem> templateMappingItems,
            List<CalculationMetadata> calculationMetadata,
            CalcModels.ProviderResultResponse providerResult,
            CalculationProviderResultSearchResults calculationProviderResultSearchResults,
            List<string> calculationIdsWithError,
            int level = 0) =>
                fundingStructures.AddRange(fundingLines.Select(fundingLine =>
                    RecursivelyAddFundingLines(
                        fundingLine.FundingLines,
                        templateMappingItems,
                        calculationMetadata,
                        level,
                        fundingLine,
                        providerResult,
                        calculationProviderResultSearchResults,
                        calculationIdsWithError)));

        private static FundingStructureItem RecursivelyAddFundingLines(IEnumerable<FundingLine> fundingLines,
            List<TemplateMappingItem> templateMappingItems,
            List<CalculationMetadata> calculationMetadata,
            int level,
            FundingLine fundingLine,
            CalcModels.ProviderResultResponse providerResult,
            CalculationProviderResultSearchResults calculationProviderResultSearchResults,
            List<string> calculationIdsWithError)
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
                            calculationProviderResultSearchResults,
                            calculationIdsWithError));
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
                        calculationProviderResultSearchResults,
                        calculationIdsWithError));
                }
            }

            CalcModels.FundingLineResult fundingLineResult =
                providerResult?.FundingLineResults?.FirstOrDefault(_ => _.FundingLine.Id == fundingLine.TemplateLineId.ToString());
            string calculationValue = null;
            if (fundingLineResult != null)
            {
                CalculationValueFormat calculationValueTypeViewModel = CalculationValueFormat.Number;
                calculationValue = fundingLineResult.Value.AsFormatCalculationType(calculationValueTypeViewModel);
            }


            string status =
                innerFundingStructureItems.FirstOrDefault(f => f.CalculationPublishStatus == "Error") != null
                    ? "Error"
                    : null;

            // Add FundingStructureItem
            FundingStructureItem fundingStructureItem = MapToFundingStructureItem(
                level,
                fundingLine.Name,
                FundingStructureType.FundingLine,
                null,
                null,
                status,
                innerFundingStructureItems.Any() ? innerFundingStructureItems : null,
                calculationValue);

            return fundingStructureItem;
        }

        private static FundingStructureItem RecursivelyMapCalculationsToFundingStructureItem(TemplateCalculation calculation,
            int level,
            List<TemplateMappingItem> templateMappingItems,
            List<CalculationMetadata> calculationMetadata,
            CalcModels.ProviderResultResponse providerResult,
            CalculationProviderResultSearchResults calculationProviderResultSearchResults,
            List<string> calculationIdsWithError)
        {
            level++;

            string calculationType = null;
            string calculationValue = null;
            List<FundingStructureItem> innerFundingStructureItems = null;

            string calculationId = GetCalculationId(calculation, templateMappingItems);

            string calculationPublishStatus = GetCalculationPublishStatus(calculationMetadata, calculationIdsWithError, calculationId);

            CalcModels.CalculationResultResponse calculationResult
                = providerResult?.CalculationResults.FirstOrDefault(_ => _.Calculation.Id == calculationId);

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
                            calculationProviderResultSearchResults,
                            calculationIdsWithError))
                    .ToList();
            }

            calculationPublishStatus =
                innerFundingStructureItems?.FirstOrDefault(f => f.CalculationPublishStatus == "Error") != null
                    ? "Error"
                    : calculationPublishStatus;

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

        private static string GetCalculationPublishStatus(List<CalculationMetadata> calculationMetadata, List<string> calculationIdsWithError,
            string calculationId) =>
                calculationIdsWithError?.FirstOrDefault(calcErrorId => calcErrorId == calculationId) != null
                    ? "Error"
                    : calculationMetadata
                        .FirstOrDefault(_ => _.CalculationId == calculationId)?
                        .PublishStatus.ToString();

        private static FundingStructureItem MapToFundingStructureItem(int level,
            string name,
            FundingStructureType type,
            string calculationType = null,
            string calculationId = null,
            string calculationPublishStatus = null,
            List<FundingStructureItem> fundingStructureItems = null,
            string value = null,
            DateTimeOffset? lastUpdatedDate = null) =>
            new FundingStructureItem
            {
                Level = level,
                Name = name,
                CalculationId = calculationId,
                CalculationPublishStatus = calculationPublishStatus,
                Type = type,
                CalculationType = calculationType,
                FundingStructureItems = fundingStructureItems,
                Value = value,
                LastUpdatedDate = lastUpdatedDate
            };

        private static string GetCalculationId(
            TemplateCalculation calculation,
            IEnumerable<TemplateMappingItem> templateMappingItems) =>
            templateMappingItems
                .FirstOrDefault(_ => _.TemplateId == calculation.TemplateCalculationId)?
                .CalculationId;
    }
}
