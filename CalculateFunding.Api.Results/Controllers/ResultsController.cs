using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Results.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Mvc;
using CalculationType = CalculateFunding.Models.Calcs.CalculationType;
using ProviderResult = CalculateFunding.Models.Calcs.ProviderResult;

namespace CalculateFunding.Api.Results.Controllers
{
    public class ResultsController : Controller
    {
        private readonly IResultsService _resultsService;
        private readonly IProviderCalculationResultsSearchService _providerCalculationResultsSearchService;
        private readonly IFeatureToggle _featureToggle;
        private readonly IProviderCalculationResultsReIndexerService _providerCalculationResultsReIndexerService;

        public ResultsController(
             IResultsService resultsService,
             IProviderCalculationResultsSearchService providerCalculationResultsSearchService,
             IFeatureToggle featureToggle,
             IProviderCalculationResultsReIndexerService providerCalculationResultsReIndexerService)
        {
            Guard.ArgumentNotNull(resultsService, nameof(resultsService));
            Guard.ArgumentNotNull(providerCalculationResultsSearchService, nameof(providerCalculationResultsSearchService));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(providerCalculationResultsReIndexerService, nameof(providerCalculationResultsReIndexerService));

            _resultsService = resultsService;
            _providerCalculationResultsSearchService = providerCalculationResultsSearchService;
            _featureToggle = featureToggle;
            _providerCalculationResultsReIndexerService = providerCalculationResultsReIndexerService;
        }

        [Route("api/results/get-provider-specs")]
        [HttpGet]
        [Produces(typeof(IEnumerable<string>))]
        public async Task<IActionResult> RunGetProviderSpecifications()
        {
            return await _resultsService.GetProviderSpecifications(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/get-provider-results")]
        [HttpGet]
        [Produces(typeof(ProviderResult))]
        public async Task<IActionResult> RunGetProviderResults()
        {
            return await _resultsService.GetProviderResults(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/specifications/{specificationId}/provider-result-by-calculationtype/{providerId}/template")]
        [HttpGet]
        [Produces(typeof(ProviderResult))]
        public async Task<IActionResult> RunProviderResultsByCalculationTypeTemplate([FromRoute] string providerId, [FromRoute] string specificationId)
        {
            return await _resultsService.GetProviderResultByCalculationType(providerId, specificationId, CalculationType.Template);
        }

        [Route("api/results/specifications/{specificationId}/provider-result-by-calculationtype/{providerId}/additional")]
        [HttpGet]
        [Produces(typeof(ProviderResult))]
        public async Task<IActionResult> RunProviderResultsByCalculationTypeAdditional([FromRoute] string providerId,[FromRoute]  string specificationId)
        {
            return await _resultsService.GetProviderResultByCalculationType(providerId, specificationId, CalculationType.Additional);
        }

        [Route("api/results/get-provider-source-datasets")]
        [HttpGet]
        [Produces(typeof(IEnumerable<ProviderSourceDataset>))]
        public async Task<IActionResult> RunGetProviderSourceDatasetsByProviderIdAndSpecificationId()
        {
            return await _resultsService.GetProviderSourceDatasetsByProviderIdAndSpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/reindex-calc-provider-results")]
        [HttpGet]
        public async Task<IActionResult> RunReIndexCalculationProviderResults()
        {
            return await _resultsService.ReIndexCalculationProviderResults();
        }

        [Route("api/results/calculation-provider-results-search")]
        [HttpPost]
        [Produces(typeof(CalculationProviderResultSearchResults))]
        public async Task<IActionResult> RunCalculationProviderResultsSearch()
        {
            return await _providerCalculationResultsSearchService.SearchCalculationProviderResults(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/get-scoped-providerids")]
        [HttpGet]
        [Produces(typeof(IEnumerable<string>))]
        public async Task<IActionResult> RunGetScopedProviderIds()
        {
            return await _resultsService.GetScopedProviderIdsBySpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/get-calculation-result-totals-for-specifications")]
        [HttpPost]
        [Produces(typeof(IEnumerable<FundingCalculationResultsTotals>))]
        public async Task<IActionResult> RunGetFundingCalculationResultsForSpecifications()
        {
            return await _resultsService.GetFundingCalculationResultsForSpecifications(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/get-specification-provider-results")]
        [HttpGet]
        [Produces(typeof(IEnumerable<ProviderResult>))]
        public async Task<IActionResult> RunGetProviderResultsBySpecificationId()
        {
            return await _resultsService.GetProviderResultsBySpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/hasCalculationResults/{calculationId}")]
        [HttpGet]
        [Produces(typeof(bool))]
        public async Task<IActionResult> HasCalculationResults(string calculationId)
        {
            return await _resultsService.HasCalculationResults(calculationId);
        }

        [Route("api/results/reindex/calculation-results")]
        [HttpGet]
        public async Task<IActionResult> ReIndexCalculationResults()
        {
            return await _providerCalculationResultsReIndexerService.ReIndexCalculationResults(ControllerContext.HttpContext.Request);
        }
    }
}