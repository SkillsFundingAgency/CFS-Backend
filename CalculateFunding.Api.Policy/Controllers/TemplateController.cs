using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Policy.Controllers
{
    [ApiController]
    public class TemplateController : ControllerBase
    {
        [HttpGet("api/templates/{fundingStreamId}/{templateVersion}")]
        [Produces("application/json")]
        public async Task<IActionResult> GetFundingTemplate([FromRoute]string fundingStreamId, [FromRoute]string templateVersion)
        {
            return new OkObjectResult(String.Empty);
        }

        /// <summary>
        /// Saves (creates or updates) a funding template based off a schema version for a funding stream.
        /// </summary>
        /// <param name="templateJson">Template JSON in the format of the schema.
        /// There is an assumption that the following json will be populated to get the schema version and funding stream in the body content:
        /// {
        ///      "schemaVersion: "1.0"
        ///      "fundingStream: {
        ///            "code": "PSG"
        ///      }
        /// }
        /// </param>
        /// <returns></returns>
        [HttpPost("api/templates")]
        [ProducesResponseType(201)]
        [Produces("application/json")]
        public async Task<IActionResult> SaveFundingTemplate([FromBody] string templateJson)
        {


            // There may be a need to follow this guide to get the raw body content https://weblog.west-wind.com/posts/2017/sep/14/accepting-raw-request-body-content-in-aspnet-core-api-controllers

            return new CreatedAtActionResult(nameof(GetFundingTemplate), nameof(TemplateController), new { fundingStreamId = "PSG", templateVersion = "1.0" }, string.Empty);
        }
    }
}
