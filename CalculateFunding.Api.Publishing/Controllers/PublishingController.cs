using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
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

        [HttpPost("api/publishedspecifications/{specificationId}")]
        public async Task<IActionResult> PublishSpecification([FromRoute] string specificationId)
        {
            return await _specificationPublishingService.CreatePublishJob(specificationId,
                GetUser(),
                GetCorrelationId());
        }

        [HttpPost("api/specifications/{specificationId}/publish")]
        public async Task<IActionResult> PublishProviderFunding([FromRoute] string specificationId)
        {
            return await _providerFundingPublishingService.PublishProviderFunding(specificationId,
                GetUser(),
                GetCorrelationId());
        }

        private Reference GetUser()
        {
            return Request.GetUser();
        }

        private string GetCorrelationId()
        {
            return Request.GetCorrelationId();
        }

        [Route("api/publishedspecifications/{specificationId}/approve")]
        [HttpPost]
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
                GetUser(),
                GetCorrelationId());
        }
    }
}