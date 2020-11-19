using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Providers.Models.Search;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.CalcEngine.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CalcEngine
{
    public class CalculationEnginePreviewService : ICalculationEnginePreviewService
    {
        private readonly ICalculationEngine _calculationEngine;
        private readonly IMapper _mapper;
        private readonly IProvidersApiClient _providersApiClient;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly IProviderSourceDatasetsRepository _providerSourceDatasetsRepository;
        private readonly ICalculationAggregationService _calculationAggregationService;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly ILogger _logger;

        private readonly AsyncPolicy _providersApiClientPolicy;
        private readonly AsyncPolicy _specificationsApiPolicy;
        private readonly AsyncPolicy _calculationsApiClientPolicy;

        public CalculationEnginePreviewService(
            ICalculationEngine calculationEngine,
            IProvidersApiClient providersApiClient,
            IMapper mapper,
            ICalculatorResiliencePolicies resiliencePolicies,
            ISpecificationsApiClient specificationsApiClient,
            IProviderSourceDatasetsRepository providerSourceDatasetsRepository,
            ICalculationAggregationService calculationAggregationService,
            ICalculationsRepository calculationsRepository,
            ILogger logger)
        {
            Guard.ArgumentNotNull(calculationEngine, nameof(calculationEngine));
            Guard.ArgumentNotNull(providersApiClient, nameof(providersApiClient));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(providerSourceDatasetsRepository, nameof(providerSourceDatasetsRepository));
            Guard.ArgumentNotNull(calculationAggregationService, nameof(calculationAggregationService));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(resiliencePolicies.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies.ProvidersApiClient, nameof(resiliencePolicies.ProvidersApiClient));
            Guard.ArgumentNotNull(resiliencePolicies.CalculationsApiClient, nameof(resiliencePolicies.CalculationsApiClient));

            _calculationEngine = calculationEngine;
            _providersApiClient = providersApiClient;
            _mapper = mapper;
            _specificationsApiClient = specificationsApiClient;
            _providerSourceDatasetsRepository = providerSourceDatasetsRepository;
            _calculationAggregationService = calculationAggregationService;
            _specificationsApiPolicy = resiliencePolicies.SpecificationsApiClient;
            _providersApiClientPolicy = resiliencePolicies.ProvidersApiClient;
            _calculationsApiClientPolicy = resiliencePolicies.CalculationsApiClient;
            _calculationsRepository = calculationsRepository;
            _logger = logger;
        }

        public async Task<IActionResult> PreviewCalculationResult(
            string specificationId, 
            string providerId,
            byte[] assemblyContent)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId));
            Guard.ArgumentNotNull(assemblyContent, nameof(assemblyContent));

            Assembly assembly = Assembly.Load(assemblyContent);
            IAllocationModel allocationModel = _calculationEngine.GenerateAllocationModel(assembly);

            SpecificationSummary specificationSummary = await GetSpecificationSummary(specificationId);

            ApiResponse<ProviderVersionSearchResult> providerVersionSearchResultApiResponse =
                await _providersApiClientPolicy.ExecuteAsync(() => _providersApiClient.GetProviderByIdFromProviderVersion(
                    specificationSummary.ProviderVersionId,
                    providerId));
            ProviderVersionSearchResult providerVersionSearchResult = providerVersionSearchResultApiResponse.Content;
            
            if(providerVersionSearchResult == null)
            {
                return new NotFoundResult();
            }
            
            ProviderSummary providerSummary = _mapper.Map<ProviderSummary>(providerVersionSearchResult);

            IEnumerable<CalculationSummaryModel> calculationSummaries
                = await GetCalculationSummaries(specificationId);

            Dictionary<string, Dictionary<string, ProviderSourceDataset>> providerSourceDatasets =
                await _providerSourceDatasetsRepository.GetProviderSourceDatasetsByProviderIdsAndRelationshipIds(
                specificationId,
                new[] { providerId },
                specificationSummary.DataDefinitionRelationshipIds);

            Dictionary<string, ProviderSourceDataset> providerSourceDataset = providerSourceDatasets[providerId];

            BuildAggregationRequest buildAggregationRequest = new BuildAggregationRequest
            {
                SpecificationId = specificationId,
                GenerateCalculationAggregationsOnly = true,
                BatchCount = 100
            };
            IEnumerable<CalculationAggregation> calculationAggregations =
                await _calculationAggregationService.BuildAggregations(buildAggregationRequest);

            ProviderResult providerResult = _calculationEngine.CalculateProviderResults(
                allocationModel,
                specificationId,
                calculationSummaries,
                providerSummary,
                providerSourceDataset,
                calculationAggregations
                );

            return new OkObjectResult(providerResult);
        }

        private async Task<SpecificationSummary> GetSpecificationSummary(string specificationId)
        {
            ApiResponse<SpecificationSummary> specificationQuery = 
                await _specificationsApiPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));
            if (specificationQuery == null || specificationQuery.StatusCode != HttpStatusCode.OK || specificationQuery.Content == null)
            {
                throw new InvalidOperationException("Specification summary is null");
            }

            return specificationQuery.Content;
        }

        private async Task<IEnumerable<CalculationSummaryModel>> GetCalculationSummaries(string specificationId)
        {
            IEnumerable<CalculationSummaryModel> calculations = await _calculationsApiClientPolicy.ExecuteAsync(() =>
                _calculationsRepository.GetCalculationSummariesForSpecification(specificationId));

            if (calculations == null)
            {
                _logger.Error($"Calculations lookup API returned null for specification id {specificationId}");

                throw new InvalidOperationException("Calculations lookup API returned null");
            }
            return calculations;
        }
    }
}
