using System.Collections.Generic;
using System.Net.Mime;
using System.Threading.Tasks;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Core.Extensions;
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

        /// <summary>
        /// Gets source file and metadata of a template
        /// </summary>
        /// <param name="fundingStreamId">Funding stream ID</param>
        /// <param name="fundingPeriodId">Funding Period ID</param>
        /// <param name="templateVersion">Template Version</param>
        /// <returns></returns>
        [HttpGet("api/templates/{fundingStreamId}/{fundingPeriodId}/{templateVersion}")]
        [Produces(typeof(FundingTemplateContents))]
        public async Task<IActionResult> GetFundingTemplate([FromRoute]string fundingStreamId, [FromRoute]string fundingPeriodId, [FromRoute]string templateVersion)
        {
            return await _fundingTemplateService.GetFundingTemplate(fundingStreamId, fundingPeriodId, templateVersion);
        }

        /// <summary>
        /// Gets source file of a template, the original file uploaded
        /// </summary>
        /// <param name="fundingStreamId">Funding stream ID</param>
        /// <param name="fundingPeriodId">Funding Perdiod ID</param>
        /// <param name="templateVersion">Template Version</param>
        /// <returns></returns>
        [HttpGet("api/templates/{fundingStreamId}/{fundingPeriodId}/{templateVersion}/sourcefile")]
        [Produces("application/json")]
        public async Task<IActionResult> GetFundingTemplateSourceFile([FromRoute]string fundingStreamId, [FromRoute]string fundingPeriodId, [FromRoute]string templateVersion)
        {
            return await _fundingTemplateService.GetFundingTemplateSourceFile(fundingStreamId, fundingPeriodId, templateVersion);
        }

        /// <summary>
        /// Gets contents for a template in the common metadata output
        /// </summary>
        /// <param name="fundingStreamId">Funding stream ID</param>
        /// <param name="fundingPeriodId">Funding Period ID</param>
        /// <param name="templateVersion">Template Version</param>
        /// <returns></returns>
        [HttpGet("api/templates/{fundingStreamId}/{fundingPeriodId}/{templateVersion}/metadata")]
        [ProducesResponseType(200, Type = typeof(TemplateMetadataContents))]
        public async Task<IActionResult> GetFundingTemplateContents([FromRoute]string fundingStreamId, [FromRoute]string fundingPeriodId, [FromRoute]string templateVersion)
        {
            return await _fundingTemplateService.GetFundingTemplateContents(fundingStreamId, fundingPeriodId, templateVersion);
        }

        /// <summary>
        /// Saves (creates or updates) a funding template based off a schema version for a funding stream and funding period.
        /// </summary>
        /// <returns></returns>
        [HttpPost("api/templates/{fundingStreamId}/{fundingPeriodId}/{templateVersion}")]
        [ProducesResponseType(201)]
        [Produces("application/json")]
        public async Task<IActionResult> SaveFundingTemplate([FromRoute]string fundingStreamId, [FromRoute]string fundingPeriodId, [FromRoute]string templateVersion)
        {
            string controllerName = string.Empty;

            if (this.ControllerContext.RouteData.Values.ContainsKey("controller"))
            {
                controllerName = (string)this.ControllerContext.RouteData.Values["controller"];
            }

            string template = await ControllerContext.HttpContext.Request.GetRawBodyStringAsync();

            return await _fundingTemplateService.SaveFundingTemplate(
                nameof(GetFundingTemplate),
                controllerName,
                template,
                fundingStreamId,
                fundingPeriodId,
                templateVersion);
        }

        /// <summary>
        /// Gets source file and metadata of a template
        /// </summary>
        /// <param name="fundingStreamId">Funding stream ID</param>
        /// <param name="fundingPeriodId">Funding Period ID</param>        
        /// <returns></returns>
        [HttpGet("api/templates/{fundingStreamId}/{fundingPeriodId}")]
        [Produces(typeof(IEnumerable<PublishedFundingTemplate>))]
        public async Task<IActionResult> GetFundingTemplates([FromRoute]string fundingStreamId, [FromRoute]string fundingPeriodId)
        {
            return await _fundingTemplateService.GetFundingTemplates(fundingStreamId, fundingPeriodId);
        }
    }
}
