using System.Threading.Tasks;
using CalculateFunding.Api.External.Swagger.OperationFilters;
using CalculateFunding.Api.External.V2.Interfaces;
using CalculateFunding.Api.External.V2.Models;
using CalculateFunding.Api.External.V2.Models.Examples;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Examples;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace CalculateFunding.Api.External.V2.Controllers
{
    [Authorize(Roles = Constants.ExecuteApiRole)]
    [ApiVersion("2.0")]
    [Route("api/v{version:apiVersion}/providers/")]
    public class ProviderResultsController : Controller
    {
        private readonly IProviderResultsService _providerResultsService;

        public ProviderResultsController(IProviderResultsService providerResultsService)
        {
            _providerResultsService = providerResultsService;
        }

        /// <summary>
        /// Returns a summary of funding stream totals for a given provider in a given period
        /// </summary>
        /// <param name="ukprn">The UKPRN identifying the provider</param>
        /// <param name="startYear">The required period start year i.e. 2018</param>
        /// <param name="endYear">The required period end year i.e. 2019</param>
        /// <param name="allocationLineIds">Comma seperated list of allocation line ids</param>
        /// <returns>The funding streams totals for all funding streams relevant for the provider in the period specified</returns>
        [HttpGet]
        [Route("ukprn/{ukprn}/startYear/{startYear}/endYear/{endYear}/allocationLines/{allocationLineIds}/summary")]
        [ProducesResponseType(typeof(ProviderResultSummary), 200)]
        [Produces(typeof(ProviderResultSummary))]
        [SwaggerResponseExample(200, typeof(ProviderResultSummaryExamples))]
        [SwaggerOperation("getProviderResultSummaryForAllocations")]
        [SwaggerOperationFilter(typeof(OperationFilter<ProviderResultSummary>))]
        [ProducesResponseType(typeof(ProviderResultSummary), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public Task<IActionResult> SummaryForAllocationLines(string ukprn, int startYear, int endYear, string allocationLineIds)
        {
            return _providerResultsService.GetProviderResultsForAllocations(ukprn, startYear, endYear, allocationLineIds, Request);
        }

        /// <summary>
        /// Returns a summary of funding stream totals for a given provider in a given period
        /// </summary>
        /// <param name="ukprn">The UKPRN identifying the provider</param>
        /// <param name="startYear">The required period start year i.e. 2018</param>
        /// <param name="endYear">The required period end year i.e. 2019</param>
        /// <param name="fundingStreamIds">Comma seperated list of funding stream ids</param>
        /// <returns>The funding streams totals for all funding streams relevant for the provider in the period specified</returns>
        [HttpGet]
        [Route("ukprn/{ukprn}/startYear/{startYear}/endYear/{endYear}/fundingStreams/{fundingStreamIds}/summary")]
        [ProducesResponseType(typeof(ProviderResultSummary), 200)]
        [Produces(typeof(ProviderResultSummary))]
        [SwaggerResponseExample(200, typeof(ProviderResultSummaryExamples))]
        [SwaggerOperation("getProviderResultSummaryForFundingStreams")]
        [SwaggerOperationFilter(typeof(OperationFilter<ProviderResultSummary>))]
        [ProducesResponseType(typeof(ProviderResultSummary), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public Task<IActionResult> SummaryForFundingStreams(string ukprn, int startYear, int endYear, string fundingStreamIds)
        {
            return _providerResultsService.GetProviderResultsForFundingStreams(ukprn, startYear, endYear, fundingStreamIds, Request);
        }

        /// <summary>
        /// Returns a summary of funding stream totals for a given la code in a given period
        /// </summary>
        /// <param name="laCode">The LACode identifying the providers to return</param>
        /// <param name="startYear">The required period start year i.e. 2018</param>
        /// <param name="endYear">The required period end year i.e. 2019</param>
        ///  <param name="allocationLineIds">Comma seperated list of allocation line ids</param>
        /// <returns>The funding streams totals for all funding streams relevant for the LACode in the period specified</returns>
        [HttpGet]
        [Route("laCode/{laCode}/startYear/{startYear}/endYear/{endYear}/allocationLines/{allocationLineIds}/summary")]
        [ProducesResponseType(typeof(LocalAuthorityResultsSummary), 200)]
        [Produces(typeof(LocalAuthorityResultsSummary))]
        [SwaggerResponseExample(200, typeof(LocalAuthorityResultSummaryExamples))]
        [SwaggerOperation("getProviderResultSummaryForLACodeAndAllocationLines")]
        [SwaggerOperationFilter(typeof(OperationFilter<LocalAuthorityResultsSummary>))]
        [ProducesResponseType(typeof(LocalAuthorityResultsSummary), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(404)]
        [ProducesResponseType(500)]
        public Task<IActionResult> SummaryForLocalAuthorityAllocationLines(string laCode, int startYear, int endYear, string allocationLineIds)
        {
            return _providerResultsService.GetLocalAuthorityProvidersResultsForAllocations(laCode, startYear, endYear, allocationLineIds, Request);
        }
    }
}
