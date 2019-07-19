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

        public PublishingController(ISpecificationPublishingService specificationPublishingService,
            IProviderFundingPublishingService providerFundingPublishingService)
        {
            Guard.ArgumentNotNull(specificationPublishingService, nameof(specificationPublishingService));
            Guard.ArgumentNotNull(providerFundingPublishingService, nameof(providerFundingPublishingService));

            _specificationPublishingService = specificationPublishingService;
            _providerFundingPublishingService = providerFundingPublishingService;
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
                Request.GetUser(),
                Request.GetCorrelationId());
        }

        /// <summary>
        /// Approve specification
        /// </summary>
        /// <param name="specificationId">The specification id</param>
        /// <returns></returns>
        [HttpPost("api/specifications/{specificationId}/approve")]
        [ProducesResponseType(201)]
        public async Task<IActionResult> ApproveSpecification([FromRoute]string specificationId)
        {
            string controllerName = string.Empty;

            if (this.ControllerContext.RouteData.Values.ContainsKey("controller"))
            {
                controllerName = (string)this.ControllerContext.RouteData.Values["controller"];
            }

            return await _specificationPublishingService.ApproveSpecification(
                nameof(ApproveSpecification),
                controllerName,
                specificationId,
                ControllerContext.HttpContext.Request,
                Request.GetUser(),
                Request.GetCorrelationId());
        }
    }
}