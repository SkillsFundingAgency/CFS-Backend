using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class PublishingController : Controller
    {
        private readonly ISpecificationPublishingService _specificationPublishingService;
        private readonly IProviderFundingPublishingService _providerFundingPublishingService;
        private readonly IPublishedProviderFundingService _publishedProviderFundingService;

        public PublishingController(ISpecificationPublishingService specificationPublishingService,
            IProviderFundingPublishingService providerFundingPublishingService,
            IPublishedProviderFundingService publishedProviderFundingService)
        {
            Guard.ArgumentNotNull(specificationPublishingService, nameof(specificationPublishingService));
            Guard.ArgumentNotNull(providerFundingPublishingService, nameof(providerFundingPublishingService));
            Guard.ArgumentNotNull(publishedProviderFundingService, nameof(publishedProviderFundingService));

            _specificationPublishingService = specificationPublishingService;
            _providerFundingPublishingService = providerFundingPublishingService;
            _publishedProviderFundingService = publishedProviderFundingService;
        }

        /// <summary>
        /// Publish specification
        /// </summary>
        /// <returns></returns>
        [HttpPost("api/publishedspecifications/{specificationId}")]
        [ProducesResponseType(201)]
        public async Task<IActionResult> PublishSpecification([FromRoute] string specificationId)
        {
            return await _specificationPublishingService.CreatePublishJob(specificationId,
                Request.GetUser(),
                Request.GetCorrelationId());
        }

        /// <summary>
        /// Publish provider funding
        /// </summary>
        /// <param name="specificationId">The specification id</param>
        /// <returns></returns>
        [HttpPost("api/specifications/{specificationId}/publish")]
        [ProducesResponseType(201)]
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
        /// Approve specification
        /// </summary>
        /// <param name="specificationId">The specification id</param>
        /// <returns></returns>
        [HttpPost("api/specifications/{specificationId}/approve")]
        [ProducesResponseType(201)]
        public async Task<IActionResult> ApproveSpecification([FromRoute] string specificationId)
        {
            var controllerName = string.Empty;

            if (ControllerContext.RouteData.Values.ContainsKey("controller"))
                controllerName = (string) ControllerContext.RouteData.Values["controller"];

            return await _specificationPublishingService.ApproveSpecification(
                nameof(ApproveSpecification),
                controllerName,
                specificationId,
                ControllerContext.HttpContext.Request,
                Request.GetUser(),
                Request.GetCorrelationId());
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