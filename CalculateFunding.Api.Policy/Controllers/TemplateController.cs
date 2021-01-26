using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy;
using CalculateFunding.Services.Core.AspNet.OperationFilters;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Swashbuckle.AspNetCore.Annotations;

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
        public async Task<ActionResult<FundingTemplateContents>> GetFundingTemplate([FromRoute] string fundingStreamId, [FromRoute] string fundingPeriodId, [FromRoute] string templateVersion)
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
        public async Task<ActionResult<string>> GetFundingTemplateSourceFile([FromRoute] string fundingStreamId, [FromRoute] string fundingPeriodId, [FromRoute] string templateVersion)
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
        public async Task<ActionResult<TemplateMetadataContents>> GetFundingTemplateContents([FromRoute] string fundingStreamId, [FromRoute] string fundingPeriodId, [FromRoute] string templateVersion)
        {
            return await _fundingTemplateService.GetFundingTemplateContents(fundingStreamId, fundingPeriodId, templateVersion);
        }


        private const string SaveFundingTemplateDescription = @"Saves (creates or updates) a funding template based off a schema version for a funding stream and funding period.

The template contents should be provided in the HTTP body as per the template schema.
The template is validated against the schema and associated rules.";

        /// <summary>
        /// Saves (creates or updates) a funding template
        /// </summary>
        /// <returns></returns>
        [HttpPost("api/templates/{fundingStreamId}/{fundingPeriodId}/{templateVersion}")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400, Type = typeof(ModelStateDictionary))]
        [Produces("application/json")]
        [JsonBodyContents]
        [SwaggerOperation(Summary = "Saves (creates or updates) a funding template", Description = SaveFundingTemplateDescription)]
        public async Task<IActionResult> SaveFundingTemplate([FromRoute] string fundingStreamId, [FromRoute] string fundingPeriodId, [FromRoute] string templateVersion)
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
        public async Task<ActionResult<IEnumerable<PublishedFundingTemplate>>> GetFundingTemplates([FromRoute] string fundingStreamId, [FromRoute] string fundingPeriodId)
        {
            return await _fundingTemplateService.GetFundingTemplates(fundingStreamId, fundingPeriodId);
        }

        /// <summary>
        /// Gets distinct fundingline and calculation contents for a template in the common metadata output
        /// </summary>
        /// <param name="fundingStreamId">Funding stream ID</param>
        /// <param name="fundingPeriodId">Funding Period ID</param>
        /// <param name="templateVersion">Template Version</param>
        /// <returns></returns>
        [HttpGet("api/templates/{fundingStreamId}/{fundingPeriodId}/{templateVersion}/metadata/distinct")]
        [ProducesResponseType(200, Type = typeof(TemplateMetadataDistinctContents))]
        public async Task<ActionResult<TemplateMetadataDistinctContents>> GetDistinctTemplateMetadataContents([FromRoute] string fundingStreamId, [FromRoute] string fundingPeriodId, [FromRoute] string templateVersion)
        {
            return await _fundingTemplateService.GetDistinctFundingTemplateMetadataContents(fundingStreamId, fundingPeriodId, templateVersion);
        }

        /// <summary>
        /// Gets distinct fundinglines contents for a template in the common metadata output
        /// </summary>
        /// <param name="fundingStreamId">Funding stream ID</param>
        /// <param name="fundingPeriodId">Funding Period ID</param>
        /// <param name="templateVersion">Template Version</param>
        /// <returns></returns>
        [HttpGet("api/templates/{fundingStreamId}/{fundingPeriodId}/{templateVersion}/metadata/distinct/funding-lines")]
        [ProducesResponseType(200, Type = typeof(TemplateMetadataDistinctFundingLinesContents))]
        public async Task<ActionResult<TemplateMetadataDistinctFundingLinesContents>> GetDistinctTemplateMetadataFundingLinesContents([FromRoute] string fundingStreamId, [FromRoute] string fundingPeriodId, [FromRoute] string templateVersion)
        {
            return await _fundingTemplateService.GetDistinctFundingTemplateMetadataFundingLinesContents(fundingStreamId, fundingPeriodId, templateVersion);
        }

        /// <summary>
        /// Gets distinct calculations contents for a template in the common metadata output
        /// </summary>
        /// <param name="fundingStreamId">Funding stream ID</param>
        /// <param name="fundingPeriodId">Funding Period ID</param>
        /// <param name="templateVersion">Template Version</param>
        /// <returns></returns>
        [HttpGet("api/templates/{fundingStreamId}/{fundingPeriodId}/{templateVersion}/metadata/distinct/calculations")]
        [ProducesResponseType(200, Type = typeof(TemplateMetadataDistinctCalculationsContents))]
        public async Task<ActionResult<TemplateMetadataDistinctCalculationsContents>> GetDistinctTemplateMetadataCalculationsContents([FromRoute] string fundingStreamId, [FromRoute] string fundingPeriodId, [FromRoute] string templateVersion)
        {
            return await _fundingTemplateService.GetDistinctFundingTemplateMetadataCalculationsContents(fundingStreamId, fundingPeriodId, templateVersion);
        }

        /// <summary>
        /// Gets cash calculations and payment fundings for a template in the common metadata output
        /// </summary>
        /// <param name="fundingStreamId">Funding stream ID</param>
        /// <param name="fundingPeriodId">Funding Period ID</param>
        /// <param name="templateVersion">Template Version</param>
        /// <returns></returns>
        [HttpGet("api/templates/{fundingStreamId}/{fundingPeriodId}/{templateVersion}/metadata/cash-calculations")]
        [ProducesResponseType(200, Type = typeof(TemplateMetadataFundingLineCashCalculationsContents))]
        public async Task<ActionResult<TemplateMetadataFundingLineCashCalculationsContents>> GetCashCalcsForFundingLines([FromRoute] string fundingStreamId, [FromRoute] string fundingPeriodId, [FromRoute] string templateVersion)
        {
            return await _fundingTemplateService.GetCashCalcsForTemplateVersion(fundingStreamId, fundingPeriodId, templateVersion);
        }
    }
}
