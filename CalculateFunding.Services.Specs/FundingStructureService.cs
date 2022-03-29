using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Calcs;
using CalculateFunding.Common.ApiClient.Calcs.Models;
using CalculateFunding.Common.ApiClient.Graph;
using CalculateFunding.Common.ApiClient.Graph.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Specifications;
using CalculateFunding.Models.Specifications.ViewModels;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Specifications.Interfaces;
using CalculateFunding.Services.Specs.Interfaces;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Calculation = CalculateFunding.Common.ApiClient.Graph.Models.Calculation;
using FundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;

namespace CalculateFunding.Services.Specifications
{
    public class FundingStructureService : IFundingStructureService
    {
        private readonly ISpecificationsService _specificationsService;
        private readonly Common.ApiClient.Policies.IPoliciesApiClient _policiesApiClient;
        private readonly ICalculationsApiClient _calculationsApiClient;
        private readonly IGraphApiClient _graphApiClient;
        private readonly ICacheProvider _cacheProvider;
        private readonly IValidator<UpdateFundingStructureLastModifiedRequest> _validator;

        private readonly AsyncPolicy _calculationsResilience;
        private readonly AsyncPolicy _policiesResilience;
        private readonly AsyncPolicy _cacheResilience;

        public FundingStructureService(
            ICacheProvider cacheProvider,
            ISpecificationsService specificationsService,
            ICalculationsApiClient calculationsApiClient,
            IGraphApiClient graphApiClient,
            Common.ApiClient.Policies.IPoliciesApiClient policiesApiClient,
            IValidator<UpdateFundingStructureLastModifiedRequest> validator,
            ISpecificationsResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(specificationsService, nameof(specificationsService));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(calculationsApiClient, nameof(calculationsApiClient));
            Guard.ArgumentNotNull(graphApiClient, nameof(graphApiClient));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(validator, nameof(validator));

            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.CacheProvider, nameof(resiliencePolicies.CacheProvider));
            Guard.ArgumentNotNull(resiliencePolicies?.CalcsApiClient, nameof(resiliencePolicies.CalcsApiClient));

            _specificationsService = specificationsService;
            _policiesApiClient = policiesApiClient;
            _calculationsApiClient = calculationsApiClient;
            _graphApiClient = graphApiClient;
            _cacheProvider = cacheProvider;
            _validator = validator;

            _policiesResilience = resiliencePolicies.PoliciesApiClient;
            _cacheResilience = resiliencePolicies.CacheProvider;
            _calculationsResilience = resiliencePolicies.CalcsApiClient;
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
            IActionResult specificationSummaryResult = await _specificationsService.GetSpecificationSummaryById(specificationId);

            if (!(specificationSummaryResult is OkObjectResult))
            {
                return specificationSummaryResult;
            }

            SpecificationSummary specificationSummary = (specificationSummaryResult as OkObjectResult).Value
                as SpecificationSummary;

            string templateVersion = specificationSummary.TemplateIds.ContainsKey(fundingStreamId)
                ? specificationSummary.TemplateIds[fundingStreamId]
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

            IEnumerable<TemplateMappingItem> missingCalculations = templateMappingResponse.Content.TemplateMappingItems?.Where(_ => string.IsNullOrWhiteSpace(_.CalculationId));

            if (missingCalculations.AnyWithNullCheck())
            {
                return new InternalServerErrorResult(
                    $"Template mappings missing for calculations '{string.Join(',', missingCalculations.Select(_ => _.Name))}' possibly due to duplicate additional calculations for specification '{specificationId}' and funding stream '{fundingStreamId}'");
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
            List<string> calculationIdsWithError,
            int level = 0) =>
                fundingStructures.AddRange(fundingLines.Select(fundingLine =>
                    RecursivelyAddFundingLines(
                        fundingLine.FundingLines,
                        templateMappingItems,
                        calculationMetadata,
                        level,
                        fundingLine,
                        calculationIdsWithError)));

        private static FundingStructureItem RecursivelyAddFundingLines(IEnumerable<FundingLine> fundingLines,
            List<TemplateMappingItem> templateMappingItems,
            List<CalculationMetadata> calculationMetadata,
            int level,
            FundingLine fundingLine,
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
                        calculationIdsWithError));
                }
            }

            string status =
                innerFundingStructureItems.FirstOrDefault(f => f.CalculationPublishStatus == "Error") != null
                    ? "Error"
                    : null;

            // Add FundingStructureItem
            FundingStructureItem fundingStructureItem = MapToFundingStructureItem(
                level,
                fundingLine.Name,
                fundingLine.FundingLineCode,
                fundingLine.TemplateLineId,
                FundingStructureType.FundingLine,
                null,
                null,
                status,
                innerFundingStructureItems.Any() ? innerFundingStructureItems : null);

            return fundingStructureItem;
        }

        private static FundingStructureItem RecursivelyMapCalculationsToFundingStructureItem(TemplateCalculation calculation,
            int level,
            List<TemplateMappingItem> templateMappingItems,
            List<CalculationMetadata> calculationMetadata,
            List<string> calculationIdsWithError)
        {
            level++;

            string calculationType = calculation.Type.ToString();
            List<FundingStructureItem> innerFundingStructureItems = null;

            string calculationId = GetCalculationId(calculation, templateMappingItems);

            string calculationPublishStatus = GetCalculationPublishStatus(calculationMetadata, calculationIdsWithError, calculationId);


            if (calculation.Calculations != null && calculation.Calculations.Any())
            {
                innerFundingStructureItems = calculation.Calculations.Select(innerCalculation =>
                        RecursivelyMapCalculationsToFundingStructureItem(
                            innerCalculation,
                            level,
                            templateMappingItems,
                            calculationMetadata,
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
                null,
                calculation.TemplateCalculationId,
                FundingStructureType.Calculation,
                calculationType,
                calculationId,
                calculationPublishStatus,
                innerFundingStructureItems);
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
            string fundingLineCode,
            uint templateId,
            FundingStructureType type,
            string calculationType = null,
            string calculationId = null,
            string calculationPublishStatus = null,
            List<FundingStructureItem> fundingStructureItems = null) =>
            new FundingStructureItem
            {
                Level = level,
                Name = name,
                FundingLineCode = fundingLineCode,
                TemplateId = templateId,
                CalculationId = calculationId,
                CalculationPublishStatus = calculationPublishStatus,
                Type = type,
                CalculationType = calculationType,
                FundingStructureItems = fundingStructureItems
            };

        private static string GetCalculationId(
            TemplateCalculation calculation,
            IEnumerable<TemplateMappingItem> templateMappingItems) =>
            templateMappingItems
                .FirstOrDefault(_ => _.TemplateId == calculation.TemplateCalculationId)?
                .CalculationId;
    }
}
