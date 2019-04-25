using CalculateFunding.Api.Providers.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Providers.Controllers
{
    [ApiController]
    public class MasterProviderController : ControllerBase
    {
        /// <summary>
        /// Get the master provider list, containing a list of all the providers in the system with their current information
        /// </summary>
        /// <returns></returns>
        [HttpGet("api/providers/master")]
        [ProducesResponseType(200, Type = typeof(MasterProviderDatasetResultViewModel))]
        public IActionResult GetMasterProviders()
        {
            return Ok(new MasterProviderDatasetResultViewModel());
        }

        /// <summary>
        /// Set a specific version (uploaded via ProviderByVersion) to be the master provider list
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        [HttpPut("api/providers/master")]
        [ProducesResponseType(201)]
        public IActionResult SetMasterProviderVersion(SetMasterProviderViewModel configuration)
        {
            return NoContent();
        }
    }
}
