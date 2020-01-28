using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Policy.Controllers
{
    [ApiController]
    public class SchemaController : ControllerBase
    {
        private readonly IFundingSchemaService _fundingSchemaService;

        public SchemaController(IFundingSchemaService fundingSchemaService)
        {
            Guard.ArgumentNotNull(fundingSchemaService, nameof(fundingSchemaService));

            _fundingSchemaService = fundingSchemaService;
        }

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
            return await _fundingSchemaService.GetFundingSchemaByVersion(schemaVersion);
        }

        /// <summary>
        /// Saves (creates or updates) a funding schema.
        /// </summary>
        /// <returns>Saved schema</returns>
        [HttpPost("api/schemas")]
        [ProducesResponseType(201)]
        [Produces("application/schema+json")]
        public async Task<IActionResult> SaveFundingSchema()
        {
            string controllerName = string.Empty;

            if (this.ControllerContext.RouteData.Values.ContainsKey("controller"))
            {
                controllerName = (string)this.ControllerContext.RouteData.Values["controller"];
            }

            string schema = await ControllerContext.HttpContext.Request.GetRawBodyStringAsync();

            return await _fundingSchemaService.SaveFundingSchema(
                nameof(GetFundingSchemaByVersion),
                controllerName,
                schema);
        }
    }
}
