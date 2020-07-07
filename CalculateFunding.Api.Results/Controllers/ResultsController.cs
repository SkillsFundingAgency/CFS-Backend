using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Results.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Mvc;
using CalculationType = CalculateFunding.Models.Calcs.CalculationType;
using ProviderResult = CalculateFunding.Models.Calcs.ProviderResult;

namespace CalculateFunding.Api.Results.Controllers
{
    public class ResultsController : ControllerBase
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
        public async Task<IActionResult> RunGetProviderSpecifications([FromQuery] string providerId)
        {
            return await _resultsService.GetProviderSpecifications(providerId);
        }

        [Route("api/results/get-provider-results")]
        [HttpGet]
        [Produces(typeof(ProviderResult))]
        public async Task<IActionResult> RunGetProviderResults([FromQuery] string providerId, [FromQuery] string specificationId)
        {
            return await _resultsService.GetProviderResults(providerId, specificationId);
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
        public async Task<IActionResult> RunProviderResultsByCalculationTypeAdditional([FromRoute] string providerId,[FromRoute] string specificationId)
        {
            return await _resultsService.GetProviderResultByCalculationType(providerId, specificationId, CalculationType.Additional);
        }

        [Route("api/results/get-provider-source-datasets")]
        [HttpGet]
        [Produces(typeof(IEnumerable<ProviderSourceDataset>))]
        public async Task<IActionResult> RunGetProviderSourceDatasetsByProviderIdAndSpecificationId([FromQuery] string specificationId, [FromQuery] string providerId)
        {
            return await _resultsService.GetProviderSourceDatasetsByProviderIdAndSpecificationId(specificationId, providerId);
        }

        [Route("api/results/reindex-calc-provider-results")]
        [HttpGet]
        [ProducesResponseType(204)]
        public async Task<IActionResult> RunReIndexCalculationProviderResults()
        {
            return await _resultsService.ReIndexCalculationProviderResults();
        }

        [Route("api/results/calculation-provider-results-search")]
        [HttpPost]
        [Produces(typeof(CalculationProviderResultSearchResults))]
        public async Task<IActionResult> RunCalculationProviderResultsSearch([FromBody] SearchModel searchModel)
        {
            return await _providerCalculationResultsSearchService.SearchCalculationProviderResults(searchModel);
        }

        [Route("api/results/get-scoped-providerids")]
        [HttpGet]
        [Produces(typeof(IEnumerable<string>))]
        public async Task<IActionResult> RunGetScopedProviderIds([FromQuery] string specificationId)
        {
            return await _resultsService.GetScopedProviderIdsBySpecificationId(specificationId);
        }

        [Route("api/results/get-calculation-result-totals-for-specifications")]
        [HttpPost]
        [Produces(typeof(IEnumerable<FundingCalculationResultsTotals>))]
        public async Task<IActionResult> RunGetFundingCalculationResultsForSpecifications([FromBody] Models.Aggregations.SpecificationListModel specificationListModel)
        {
            return await _resultsService.GetFundingCalculationResultsForSpecifications(specificationListModel);
        }

        [Route("api/results/get-specification-provider-results")]
        [HttpGet]
        [Produces(typeof(IEnumerable<ProviderResult>))]
        public async Task<IActionResult> RunGetProviderResultsBySpecificationId([FromQuery] string specificationId, [FromQuery] string top)
        {
            return await _resultsService.GetProviderResultsBySpecificationId(specificationId, top);
        }

        [Route("api/results/provider-has-results")]
        [HttpGet]
        [Produces(typeof(bool))]
        public async Task<IActionResult> RunGetProviderHasResultsBySpecificationId([FromQuery] string specificationId)
        {
            return await _resultsService.ProviderHasResultsBySpecificationId(specificationId);
        }

        [Route("api/results/hasCalculationResults/{calculationId}")]
        [HttpGet]
        [Produces(typeof(bool))]
        public async Task<IActionResult> HasCalculationResults([FromRoute] string calculationId)
        {
            return await _resultsService.HasCalculationResults(calculationId);
        }

        [Route("api/results/reindex/calculation-results")]
        [HttpGet]
        [ProducesResponseType(204)]
        public async Task<IActionResult> ReIndexCalculationResults()
        {
            return await _providerCalculationResultsReIndexerService.ReIndexCalculationResults( Request.GetCorrelationId() ,Request.GetUser());
        }
    }
}