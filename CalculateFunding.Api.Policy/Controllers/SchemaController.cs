using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Policy.Controllers
{
    [ApiController]
    public class SchemaController : ControllerBase
    {
        /// <summary>
        /// Gets a schema version for funding which defines how a version of the funding model is defined.
        /// This schema is then used to define a template for a funding stream and for the output of funding once calculated.
        /// </summary>
        /// <param name="schemaVersion">Schema Version. Should always be a major and minor version eg 1.0 or 2.1</param>
        /// <returns>Returns a JSON Schema (https://json-schema.org/) definition</returns>
        [HttpGet("api/schemas/{schemaVersion}")]
        [Produces("application/schema+json")]
        public async Task<IActionResult> GetFundingSchemaByVersion([FromRoute]string schemaVersion)
        {
            return new OkObjectResult(String.Empty);
        }

        /// <summary>
        /// Saves (creates or updates) a funding schema.
        /// </summary>
        /// <param name="schema">The body should be a JSON Schema (https://json-schema.org/) definition which defines how a version of the funding model is defined.</param>
        /// <returns>Saved schema</returns>
        [HttpPost("api/schemas")]
        [ProducesResponseType(201)]
        [Produces("application/schema+json")]
        public async Task<IActionResult> SaveFundingSchema([FromBody]string schema)
        {
            // There may be a need to follow this guide to get the raw body content https://weblog.west-wind.com/posts/2017/sep/14/accepting-raw-request-body-content-in-aspnet-core-api-controllers

            return new CreatedAtActionResult(nameof(GetFundingSchemaByVersion), nameof(SchemaController), new { schemaVersion = 1.0 }, string.Empty);
        }
    }
}
