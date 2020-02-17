using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class PublishingController : ControllerBase
    {
        private readonly ISpecificationPublishingService _specificationPublishingService;
        private readonly IProviderFundingPublishingService _providerFundingPublishingService;
        private readonly IPublishedProviderFundingService _publishedProviderFundingService;
        private readonly IPublishedSearchService _publishedSearchService;
        private readonly IPublishedProviderVersionService _publishedProviderVersionService;
        private readonly IPublishedProviderStatusService _publishedProviderStatusService;
        private readonly IDeleteSpecifications _deleteSpecifications;

        public PublishingController(ISpecificationPublishingService specificationPublishingService,
            IProviderFundingPublishingService providerFundingPublishingService,
            IPublishedProviderFundingService publishedProviderFundingService,
            IPublishedSearchService publishedSearchService,
            IPublishedProviderVersionService publishedProviderVersionService,
            IPublishedProviderStatusService publishedProviderStatusService, 
            IDeleteSpecifications deleteSpecifications)
        {
            Guard.ArgumentNotNull(specificationPublishingService, nameof(specificationPublishingService));
            Guard.ArgumentNotNull(providerFundingPublishingService, nameof(providerFundingPublishingService));
            Guard.ArgumentNotNull(publishedProviderFundingService, nameof(publishedProviderFundingService));
            Guard.ArgumentNotNull(publishedSearchService, nameof(publishedSearchService));
            Guard.ArgumentNotNull(publishedProviderVersionService, nameof(publishedProviderVersionService));
            Guard.ArgumentNotNull(publishedProviderStatusService, nameof(publishedProviderStatusService));
            Guard.ArgumentNotNull(deleteSpecifications, nameof(deleteSpecifications));

            _specificationPublishingService = specificationPublishingService;
            _providerFundingPublishingService = providerFundingPublishingService;
            _publishedProviderFundingService = publishedProviderFundingService;
            _publishedSearchService = publishedSearchService;
            _publishedProviderVersionService = publishedProviderVersionService;
            _publishedProviderStatusService = publishedProviderStatusService;
            _deleteSpecifications = deleteSpecifications;
        }
        
        [HttpDelete("api/specifications/{specificationId}")]
        [ApiExplorerSettings(IgnoreApi = true)]
        [ProducesResponseType(204)]
        public async Task<IActionResult> DeleteSpecification([FromRoute] string specificationId)
        {
            await _specificationPublishingService.CreateRefreshFundingJob(specificationId,
                Request.GetUser(),
                Request.GetCorrelationId());

            return NoContent();
        }

        /// <summary>
        /// Refresh funding for a specification
        /// </summary>
        /// <returns></returns>
        [HttpPost("api/specifications/{specificationId}/refresh")]
        [ProducesResponseType(200, Type = typeof(JobCreationResponse))]
        public async Task<IActionResult> RefreshFundingForSpecification([FromRoute] string specificationId)
        {
            return await _specificationPublishingService.CreateRefreshFundingJob(specificationId,
                Request.GetUser(),
                Request.GetCorrelationId());
        }

        /// <summary>
        /// Approve funding for a specification
        /// </summary>
        /// <param name="specificationId">The specification id</param>
        /// <returns></returns>
        [HttpPost("api/specifications/{specificationId}/approve")]
        [ProducesResponseType(200, Type = typeof(JobCreationResponse))]
        public async Task<IActionResult> ApproveSpecification([FromRoute] string specificationId)
        {
            var controllerName = string.Empty;

            if (ControllerContext.RouteData.Values.ContainsKey("controller"))
                controllerName = (string)ControllerContext.RouteData.Values["controller"];

            return await _specificationPublishingService.ApproveSpecification(
                nameof(ApproveSpecification),
                controllerName,
                specificationId,
                Request.GetUser(),
                Request.GetCorrelationId());
        }

        /// <summary>
        /// Publish provider funding
        /// </summary>
        /// <param name="specificationId">The specification id</param>
        /// <returns></returns>
        [HttpPost("api/specifications/{specificationId}/publish")]
        [ProducesResponseType(200, Type = typeof(JobCreationResponse))]
        public async Task<IActionResult> PublishProviderFunding([FromRoute] string specificationId)
        {
            return await _providerFundingPublishingService.PublishProviderFunding(specificationId,
                GetUser(),
                GetCorrelationId());
        }

        [HttpGet("api/specifications/{specificationId}/publishedproviders")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> PublishedProviders([FromRoute] string specificationId)
        {
            return await _publishedProviderFundingService
                .GetLatestPublishedProvidersForSpecificationId(specificationId);
        }

        /// <summary>
        /// Check can choose specification for funding
        /// </summary>
        /// <param name="specificationId">The specification id</param>
        /// <returns></returns>
        [HttpGet("api/specifications/{specificationId}/funding/canChoose")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> CanChooseForFunding([FromRoute] string specificationId)
        {
            return await _specificationPublishingService.CanChooseForFunding(specificationId);
        }

        [Route("api/publishedprovider/publishedprovider-search")]
        [HttpPost]
        [ProducesResponseType(200, Type = typeof(PublishedSearchResults))]
        public async Task<IActionResult> SearchPublishedProvider([FromBody] SearchModel searchModel)
        {
            return await _publishedSearchService.SearchPublishedProviders(searchModel);
        }

        [HttpGet("api/publishedprovider/reindex")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> ReIndex()
        {
            return await _publishedProviderVersionService.ReIndex(GetUser(),
                GetCorrelationId());
        }

        /// <summary>
        /// Get count of published provider by state
        /// </summary>
        /// <param name="specificationId"></param>
        /// <param name="providerType"></param>
        /// <param name="localAuthority"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        [HttpGet("api/specifications/{specificationId}/publishedproviders/publishingstatus")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<ProviderFundingStreamStatusResponse>))]
        public async Task<IActionResult> GetProviderStatusCounts([FromRoute] string specificationId, [FromQuery] string providerType, [FromQuery] string localAuthority, [FromQuery] string status)
        {
            return await _publishedProviderStatusService
                .GetProviderStatusCounts(specificationId, providerType, localAuthority, status);
        }

        [HttpGet("api/publishedproviders/{fundingStreamId}/{fundingPeriodId}/localauthorities")]
        [ProducesResponseType(200, Type = typeof(IEnumerable<string>))]
        public async Task<IActionResult> SearchPublishedProviderLocalAuthorities([FromQuery] string searchText, [FromRoute] string fundingStreamId, [FromRoute] string fundingPeriodId)
        {
            return await _publishedSearchService.SearchPublishedProviderLocalAuthorities(searchText, fundingStreamId, fundingPeriodId);
        }

        private Reference GetUser()
        {
            return Request.GetUser();
        }

        private string GetCorrelationId()
        {
            return Request.GetCorrelationId();
        }
    }
}