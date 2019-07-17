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
    }
}