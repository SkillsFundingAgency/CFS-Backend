using System;
using System.Threading.Tasks;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Datasets.Interfaces;
using CalculateFunding.Services.Results;
using CalculateFunding.Services.Results.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{
	public class ResultsController : BaseController
	{
		private readonly IResultsService _resultsService;
		private readonly IResultsSearchService _resultsSearchService;

		public ResultsController(IServiceProvider serviceProvider,
			 IResultsService resultsService, IResultsSearchService resultsSearchService)
			: base(serviceProvider)
		{
			Guard.ArgumentNotNull(resultsSearchService, nameof(resultsSearchService));

			_resultsService = resultsService;
			_resultsSearchService = resultsSearchService;
		}

		[Route("api/results/providers-search")]
		[HttpPost]
		public Task<IActionResult> RunProvidersSearch()
		{
			SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

			return _resultsSearchService.SearchProviders(ControllerContext.HttpContext.Request);
		}

		[Route("api/results/get-provider-specs")]
		[HttpGet]
		public Task<IActionResult> RunGetProviderSpecifications()
		{
			SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

			return _resultsService.GetProviderSpecifications(ControllerContext.HttpContext.Request);
		}


		[Route("api/results/get-provider-results")]
		[HttpGet]
		public Task<IActionResult> RunGetProviderResults()
		{
			SetUserAndCorrelationId(ControllerContext.HttpContext.Request);

			return _resultsService.GetProviderResults(ControllerContext.HttpContext.Request);
		}
	}
}