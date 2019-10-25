using System.Threading.Tasks;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Results.Controllers
{
    public class ResultsController : Controller
    {
        private readonly IResultsService _resultsService;
        private readonly ICalculationProviderResultsSearchService _calculationProviderResultsSearchService;
        private readonly IProviderCalculationResultsSearchService _providerCalculationResultsSearchService;
        private readonly IFeatureToggle _featureToggle;
        private readonly IProviderCalculationResultsReIndexerService _providerCalculationResultsReIndexerService;

        public ResultsController(
             IResultsService resultsService,
             ICalculationProviderResultsSearchService calculationProviderResultsSearchService,
             IProviderCalculationResultsSearchService providerCalculationResultsSearchService,
             IFeatureToggle featureToggle,
             IProviderCalculationResultsReIndexerService providerCalculationResultsReIndexerService)
        {
            Guard.ArgumentNotNull(resultsService, nameof(resultsService));
            Guard.ArgumentNotNull(calculationProviderResultsSearchService, nameof(calculationProviderResultsSearchService));
            Guard.ArgumentNotNull(providerCalculationResultsSearchService, nameof(providerCalculationResultsSearchService));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(providerCalculationResultsReIndexerService, nameof(providerCalculationResultsReIndexerService));

            _calculationProviderResultsSearchService = calculationProviderResultsSearchService;
            _resultsService = resultsService;
            _providerCalculationResultsSearchService = providerCalculationResultsSearchService;
            _featureToggle = featureToggle;
            _providerCalculationResultsReIndexerService = providerCalculationResultsReIndexerService;
        }

        [Route("api/results/get-provider-specs")]
        [HttpGet]
        public async Task<IActionResult> RunGetProviderSpecifications()
        {
            return await _resultsService.GetProviderSpecifications(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/get-provider-results")]
        [HttpGet]
        public async Task<IActionResult> RunGetProviderResults()
        {
            return await _resultsService.GetProviderResults(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/specifications/{specificationId}/provider-result-by-calculationtype/{providerId}/template")]
        [HttpGet]
        public async Task<IActionResult> RunProviderResultsByCalculationTypeTemplate(string providerId, string specificationId)
        {
            return await _resultsService.GetProviderResultByCalculationType(providerId, specificationId, CalculationType.Template);
        }

        [Route("api/results/specifications/{specificationId}/provider-result-by-calculationtype/{providerId}/additional")]
        [HttpGet]
        public async Task<IActionResult> RunProviderResultsByCalculationTypeAdditional(string providerId, string specificationId)
        {
            return await _resultsService.GetProviderResultByCalculationType(providerId, specificationId, CalculationType.Additional);
        }

        [Route("api/results/get-provider-source-datasets")]
        [HttpGet]
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
        public async Task<IActionResult> RunCalculationProviderResultsSearch()
        {
            if (_featureToggle.IsNewProviderCalculationResultsIndexEnabled())
            {
                return await _providerCalculationResultsSearchService.SearchCalculationProviderResults(ControllerContext.HttpContext.Request);
            }
            else
            {
                return await _calculationProviderResultsSearchService.SearchCalculationProviderResults(ControllerContext.HttpContext.Request);
            }
        }

        [Route("api/results/get-scoped-providerids")]
        [HttpGet]
        public async Task<IActionResult> RunGetScopedProviderIds()
        {
            return await _resultsService.GetScopedProviderIdsBySpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/get-calculation-result-totals-for-specifications")]
        [HttpPost]
        public async Task<IActionResult> RunGetFundingCalculationResultsForSpecifications()
        {
            return await _resultsService.GetFundingCalculationResultsForSpecifications(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/get-specification-provider-results")]
        [HttpGet]
        public async Task<IActionResult> RunGetProviderResultsBySpecificationId()
        {
            return await _resultsService.GetProviderResultsBySpecificationId(ControllerContext.HttpContext.Request);
        }
        
        [Route("api/results/hasCalculationResults/{calculationId}")]
        [HttpGet]
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