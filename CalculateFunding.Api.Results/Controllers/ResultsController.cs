using System.Threading.Tasks;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Results.Controllers
{
    public class ResultsController : Controller
	{
		private readonly IResultsService _resultsService;
		private readonly IResultsSearchService _resultsSearchService;
        private readonly ICalculationProviderResultsSearchService _calculationProviderResultsSearchService;
        private readonly IPublishedResultsService _publishedResultsService;
        private readonly IProviderCalculationResultsSearchService _providerCalculationResultsSearchService;
        private readonly IFeatureToggle _featureToggle;

        public ResultsController(
			 IResultsService resultsService,
             IResultsSearchService resultsSearchService,
             ICalculationProviderResultsSearchService calculationProviderResultsSearchService,
             IPublishedResultsService publishedResultsService,
             IProviderCalculationResultsSearchService providerCalculationResultsSearchService,
             IFeatureToggle featureToggle)
		{
			Guard.ArgumentNotNull(resultsSearchService, nameof(resultsSearchService));
            Guard.ArgumentNotNull(resultsService, nameof(resultsService));
            Guard.ArgumentNotNull(calculationProviderResultsSearchService, nameof(calculationProviderResultsSearchService));
            Guard.ArgumentNotNull(publishedResultsService, nameof(publishedResultsService));
            Guard.ArgumentNotNull(providerCalculationResultsSearchService, nameof(providerCalculationResultsSearchService));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

            _resultsSearchService = resultsSearchService; 
            _calculationProviderResultsSearchService = calculationProviderResultsSearchService;
            _publishedResultsService = publishedResultsService;
            _resultsService = resultsService;
            _providerCalculationResultsSearchService = providerCalculationResultsSearchService;
            _featureToggle = featureToggle;
        }

		[Route("api/results/providers-search")]
		[HttpPost]
		public async Task<IActionResult> RunProvidersSearch()
		{
            return await _resultsSearchService.SearchProviders(ControllerContext.HttpContext.Request);
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

        [Route("api/results/get-provider")]
        [HttpGet]
        public async Task<IActionResult> RunGetProvider()
        {
            return await _resultsService.GetProviderById(ControllerContext.HttpContext.Request);
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

        [Route("api/results/get-published-provider-results-for-specification")]
        [HttpGet]
        public async Task<IActionResult> RunGetPublishedProviderResultsForSpecification()
        {
            return await _publishedResultsService.GetPublishedProviderResultsBySpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/get-published-provider-results-for-funding-stream")]
        [HttpGet]
        public async Task<IActionResult> RunGetPublishedProviderResultsByFundingPeriodAndSpecificationAndFundingStream()
        {
            return await _publishedResultsService.GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/get-confirmation-details-for-approve-publish-provider-results")]
        [HttpPost]
        public async Task<IActionResult> RunGetConfirmationDetailsForApprovePublishProviderResults()
        {
            return await _publishedResultsService.GetConfirmationDetailsForApprovePublishProviderResults(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/update-published-allocationline-results-status")]
        [HttpPost]
        public async Task<IActionResult> RunUpdatePublishedAllocationLineResultsStatus()
        {
            return await _publishedResultsService.UpdatePublishedAllocationLineResultsStatus(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/get-specification-provider-results")]
        [HttpGet]
        public async Task<IActionResult> RunGetProviderResultsBySpecificationId()
        {
            return await _resultsService.GetProviderResultsBySpecificationId(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/import-providers")]
        [HttpPost]
        public async Task<IActionResult> RunImportProviders()
        {
			return await _resultsService.ImportProviders(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/remove-current-providers")]
        [HttpPost]
        public async Task<IActionResult> RunRemoveCurrentProviders()
        {
            return await _resultsService.RemoveCurrentProviders();
        }

        [Route("api/results/reindex/allocation-feeds")]
        [HttpGet]
        public async Task<IActionResult> ReIndexAllocationFeeds()
        {
            return await _publishedResultsService.ReIndexAllocationNotificationFeeds();
        }

        [Route("api/results/hasCalculationResults/{calculationId}")]
        [HttpGet]
        public async Task<IActionResult> HasCalculationResults(string calculationId)
        {
            return await _resultsService.HasCalculationResults(calculationId);
        }

        [Route("api/results/migrate-feed-index-id")]
        [HttpPost]
        public async Task<IActionResult> RunMigratefeedIndexId()
        {
            return await _publishedResultsService.MigrateFeedIndexId(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/migrate-version-numbers")]
        [HttpPost]
        public async Task<IActionResult> RunMigrateVersionNumbers()
        {
            return await _publishedResultsService.MigrateVersionNumbers(ControllerContext.HttpContext.Request);
        }

        [Route("api/results/migrate-calculation-results")]
        [HttpPost]
        public async Task<IActionResult> MigratePublishedCalculationResults()
        {
            return await _publishedResultsService.MigratePublishedCalculationResults(ControllerContext.HttpContext.Request);
        }
    }
}