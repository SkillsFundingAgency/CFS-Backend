using System.Threading.Tasks;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Publishing.Controllers
{
    [ApiController]
    public class PublishingSpecificationController : ControllerBase
    {
        /// <summary>
        /// Check can choose specification for funding
        /// </summary>
        /// <param name="specificationId">The specification id</param>
        /// <param name="specificationPublishingService"></param>
        /// <returns></returns>
        [HttpGet("api/specifications/{specificationId}/funding/canChoose")]
        [ProducesResponseType(200)]
        public async Task<IActionResult> CanChooseForFunding(
            [FromRoute] string specificationId,
            [FromServices] ISpecificationPublishingService specificationPublishingService)
        {
            return await specificationPublishingService.CanChooseForFunding(specificationId);
        }

        /// <summary>
        /// Delete published funding for a specification
        /// </summary>
        /// <param name="specificationId">Specification Id</param>
        /// <param name="deleteSpecifications"></param>
        /// <returns></returns>
        [HttpDelete("api/specifications/{specificationId}")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> DeleteSpecification(
            [FromRoute] string specificationId,
            [FromServices] IDeleteSpecifications deleteSpecifications)
        {
            await deleteSpecifications.QueueDeleteSpecificationJob(specificationId,
                Request.GetUser(),
                Request.GetCorrelationId());

            return NoContent();
        }
    }
}