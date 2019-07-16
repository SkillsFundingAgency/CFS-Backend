using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CalculateFunding.Api.Publishing.Controllers
{
    public class PublishingController : Controller
    {
        private readonly ISpecificationPublishingService _specificationPublishingService;

        public PublishingController(ISpecificationPublishingService specificationPublishingService)
        {
            Guard.ArgumentNotNull(specificationPublishingService, nameof(specificationPublishingService));

            _specificationPublishingService = specificationPublishingService;
        }

        [HttpPost("api/publishedspecifications/{specificationId}")]
        public async Task<IActionResult> PublishSpecification([FromRoute] string specificationId)
        {
            return await _specificationPublishingService.CreatePublishJob(specificationId,
                Request.GetUser(),
                Request.GetCorrelationId());
        }
    }
}