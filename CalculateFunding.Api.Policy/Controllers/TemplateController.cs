using System;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Policy.Controllers
{
    [ApiController]
    public class TemplateController : ControllerBase
    {
        private readonly IFundingTemplateService _fundingTemplateService;

        public TemplateController(IFundingTemplateService fundingTemplateService)
        {
            Guard.ArgumentNotNull(fundingTemplateService, nameof(fundingTemplateService));
            _fundingTemplateService = fundingTemplateService;
        }

        [HttpGet("api/templates/{fundingStreamId}/{templateVersion}")]
        [Produces("application/json")]
        public async Task<IActionResult> GetFundingTemplate([FromRoute]string fundingStreamId, [FromRoute]string templateVersion)
        {
            return await _fundingTemplateService.GetFundingTemplate(fundingStreamId, templateVersion);
        }

        /// <summary>
        /// Saves (creates or updates) a funding template based off a schema version for a funding stream.
        /// </summary>
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
        public async Task<IActionResult> SaveFundingTemplate()
        {
            string controllerName = string.Empty;

            if (this.ControllerContext.RouteData.Values.ContainsKey("controller"))
            {
                controllerName = (string)this.ControllerContext.RouteData.Values["controller"];
            }

            return await _fundingTemplateService.SaveFundingTemplate(
                nameof(GetFundingTemplate),
                controllerName,
                ControllerContext.HttpContext.Request);
        }
    }
}
