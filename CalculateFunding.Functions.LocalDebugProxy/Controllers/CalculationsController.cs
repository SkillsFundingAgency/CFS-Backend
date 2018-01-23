using CalculateFunding.Functions.Calcs.Http;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace CalculateFunding.Functions.LocalDebugProxy.Controllers
{
    public class CalculationsController : Controller
    {
        [Route("api/calcs/calculations-search")]
        [HttpPost]
        public Task<IActionResult> RunSpecificationsByYear()
        {
            return Calculations.RunCalculationsSearch(ControllerContext.HttpContext.Request, null);
        }
    }
}
