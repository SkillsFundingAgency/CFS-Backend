using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Services.Calcs.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Calcs.Controllers
{
    [ApiController]
    public class ObsoleteItemController : ControllerBase
    {
        private readonly IObsoleteItemService _obsoleteItemService;

        public ObsoleteItemController(IObsoleteItemService obsoleteItemService)
        {
            Guard.ArgumentNotNull(obsoleteItemService, nameof(obsoleteItemService));
            _obsoleteItemService = obsoleteItemService;
        }

        /// <summary>
        /// Get obsolete items for specification
        /// </summary>
        /// <param name="specificationId">Specification Id</param>
        /// <returns></returns>
        [HttpGet("api/obsoleteitems/specifications/{specificationId}")]
        [ProducesResponseType(typeof(IEnumerable<ObsoleteItem>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetObsoleteItemsForSpecification([FromRoute]string specificationId)
        {
            return await _obsoleteItemService.GetObsoleteItemsForSpecification(specificationId);
        }

        /// <summary>
        /// Get obsolete items for calculation
        /// </summary>
        /// <param name="calculationId">Calculation Id</param>
        /// <returns></returns>
        [HttpGet("api/obsoleteitems/calculations/{calculationId}")]
        [ProducesResponseType(typeof(IEnumerable<ObsoleteItem>), (int)HttpStatusCode.OK)]
        [ProducesResponseType((int)HttpStatusCode.NotFound)]
        public async Task<IActionResult> GetObsoleteItemsForCalculation([FromRoute]string calculationId)
        {
            return await _obsoleteItemService.GetObsoleteItemsForCalculation(calculationId);
        }

        /// <summary>
        /// Creates new obsolete item
        /// </summary>
        /// <param name="obsoleteItem"></param>
        /// <returns></returns>
        [HttpPost("api/obsoleteitems")]
        [ProducesResponseType((int) HttpStatusCode.Created)]
        public async Task<IActionResult> CreateObsoleteItem([FromBody] ObsoleteItem obsoleteItem)
        {
            return await _obsoleteItemService.CreateObsoleteItem(obsoleteItem);
        }

        /// <summary>
        /// Delete calculation from existing obsolete item.
        /// If the last calculation is being removed from the obsolete item, then the whole obsolete item will be deleted
        /// </summary>
        /// <param name="obsoleteItemId">Obsolete Item Id</param>
        /// <param name="calculationId">Calculation Id</param>
        /// <returns></returns>
        [HttpDelete("api/obsoleteitems/{obsoleteItemId}/{calculationId}")]
        [ProducesResponseType((int)HttpStatusCode.OK)]
        public async Task<IActionResult> RemoveObsoleteItem([FromRoute] string obsoleteItemId, [FromRoute] string calculationId)
        {
            return await _obsoleteItemService.RemoveObsoleteItem(obsoleteItemId, calculationId);
        }

        /// <summary>
        /// Add referenced calculation to existing obsolete item
        /// </summary>
        /// <param name="obsoleteItemId">Obsolete Item Id</param>
        /// <param name="calculationId">calculation Id</param>
        /// <returns></returns>
        [HttpPatch("api/obsoleteitems/{obsoleteItemId}/{calculationId}")]
        [ProducesResponseType((int)HttpStatusCode.NoContent)]
        public async Task<IActionResult> AddCalculationToObsoleteItem([FromRoute] string obsoleteItemId, [FromRoute] string calculationId)
        {
            return await _obsoleteItemService.AddCalculationToObsoleteItem(obsoleteItemId, calculationId);
        }
    }
}
