using System.Threading.Tasks;
using CalculateFunding.Services.Profiling.Models;
using CalculateFunding.Services.Profiling.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Profiling.Controllers
{
    [Authorize(Roles = "ExecuteApi")]
    [ApiController]
    public class ProfilingController : ControllerBase
    {
        private readonly ICalculateProfileService _calculateProfileService;

        public ProfilingController(ICalculateProfileService calculateProfileService)
        {
            _calculateProfileService = calculateProfileService;
        }

        /// <summary>
        /// Profile an allocation amount based on a profile pattern for funding stream and period
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [Route("api/profiling")]
        [HttpPost]
        [Produces(typeof(AllocationProfileResponse))]
        public async Task<IActionResult> Post([FromBody]ProfileRequest request)
        {
            if (!ModelState.IsValid)
            {
                return new BadRequestObjectResult(ModelState);
            }

            return await _calculateProfileService.ProcessProfileAllocationRequest(request);
        }
    }
}
