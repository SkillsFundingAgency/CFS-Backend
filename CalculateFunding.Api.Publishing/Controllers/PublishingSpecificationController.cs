using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class PublishingSpecificationController : ControllerBase
    {
        private readonly ISpecificationPublishingService _specificationPublishingService;
        private readonly IDeleteSpecifications _deleteSpecifications;

        public PublishingSpecificationController(
            ISpecificationPublishingService specificationPublishingService,
            IDeleteSpecifications deleteSpecifications)
        {
            Guard.ArgumentNotNull(specificationPublishingService, nameof(specificationPublishingService));
            Guard.ArgumentNotNull(deleteSpecifications, nameof(deleteSpecifications));

            _specificationPublishingService = specificationPublishingService;
            _deleteSpecifications = deleteSpecifications;
        }

        /// <summary>
        /// Check can choose specification for funding
        /// </summary>
        /// <param name="specificationId">The specification id</param>
        /// <returns></returns>
        [HttpGet("api/specifications/{specificationId}/funding/canChoose")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> CanChooseForFunding(
            [FromRoute] string specificationId)
        {
            return await _specificationPublishingService.CanChooseForFunding(specificationId);
        }

        /// <summary>
        /// Delete published funding for a specification
        /// </summary>
        /// <param name="specificationId">Specification Id</param>
        /// <returns></returns>
        [HttpDelete("api/specifications/{specificationId}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> DeleteSpecification(
            [FromRoute] string specificationId)
        {
            await _deleteSpecifications.QueueDeleteSpecificationJob(specificationId,
                Request.GetUser(),
                Request.GetCorrelationId());

            return NoContent();
        }
    }
}