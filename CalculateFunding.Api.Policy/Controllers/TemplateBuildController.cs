using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.Search;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Repositories.Common.Search.Results;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Models;
using CalculateFunding.Services.Policy.TemplateBuilder;
using CalculateFunding.Services.Policy.Validators;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Policy.Controllers
{
    [ApiController]
    public class TemplateBuildController : ControllerBase
    {
        private readonly ITemplateBuilderService _templateBuilderService;
        private readonly IIoCValidatorFactory _validatorFactory;
        private readonly TemplateSearchService _templateSearchService;

        public TemplateBuildController(
            ITemplateBuilderService templateBuilderService,
            IIoCValidatorFactory validatorFactory,
            TemplateSearchService templateSearchService)
        {
            _templateBuilderService = templateBuilderService;
            _validatorFactory = validatorFactory;
            _templateSearchService = templateSearchService;
        }

        /// <summary>
        /// Gets a template by templateId
        /// </summary>
        /// <param name="templateId">The template id</param>
        /// <returns>200 with template if successful</returns>
        [HttpGet("api/templates/build/{templateId}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TemplateResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTemplate([FromRoute] string templateId)
        {
            if (string.IsNullOrWhiteSpace(templateId))
            {
                return new BadRequestObjectResult("Null or empty template id");
            }

            TemplateResponse result = await _templateBuilderService.GetTemplate(templateId);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        /// <summary>
        /// Gets a specific template version by templateId and version
        /// </summary>
        /// <param name="templateId">The templateId</param>
        /// <param name="version">The version</param>
        /// <returns>200 with template version if successful</returns>
        [HttpGet("api/templates/build/{templateId}/versions/{version}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TemplateResponse))]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetTemplateVersion([FromRoute] string templateId, [FromRoute] string version)
        {
            if (string.IsNullOrWhiteSpace(templateId))
            {
                return new BadRequestObjectResult("Null or empty template id");
            }
            if (string.IsNullOrWhiteSpace(version))
            {
                return new BadRequestObjectResult("Null or empty template version");
            }

            TemplateResponse result = await _templateBuilderService.GetTemplateVersion(templateId, version);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        /// <summary>
        /// Create a new template
        /// </summary>
        /// <param name="command">Payload for creating a template</param>
        /// <returns>201 if template created successfully</returns>
        [HttpPost("api/templates/build")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateTemplate(TemplateCreateCommand command)
        {
            ValidationResult validationResult = await _validatorFactory.Validate(command);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            Reference author = ControllerContext.HttpContext.Request?.GetUserOrDefault();

            CommandResult result = await _templateBuilderService.CreateTemplate(command, author);

            if (result.Succeeded)
            {
                return new CreatedResult($"api/templates/build/{result.TemplateId}", result.TemplateId);
            }

            if (result.ValidationResult != null)
            {
                return result.ValidationResult.AsBadRequest();
            }

            return new InternalServerErrorResult(result.ErrorMessage ?? result.Exception?.Message ?? "Unknown error occurred");
        }

        /// <summary>
        /// Create a template clone
        /// </summary>
        /// <param name="command">Payload for cloning a template</param>
        /// <returns>201 if template clone created successfully</returns>
        [HttpPost("api/templates/build/clone")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateTemplateAsClone(TemplateCreateAsCloneCommand command)
        {
            ValidationResult validationResult = await _validatorFactory.Validate(command);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            Reference author = ControllerContext.HttpContext.Request?.GetUserOrDefault();

            CommandResult result = await _templateBuilderService.CreateTemplateAsClone(command, author);

            if (result.Succeeded)
            {
                return new CreatedResult($"api/templates/build/{result.TemplateId}", result.TemplateId);
            }

            if (result.ValidationResult != null)
            {
                return result.ValidationResult.AsBadRequest();
            }

            return new InternalServerErrorResult(result.ErrorMessage ?? result.Exception?.Message ?? "Unknown error occurred");
        }

        /// <summary>
        /// Update the template content, i.e. the template json
        /// </summary>
        /// <param name="command">Payload for updating the template content</param>
        /// <returns>200 if template updated successfully</returns>
        [HttpPut("api/templates/build/content")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTemplateContent(TemplateFundingLinesUpdateCommand command)
        {
            ValidationResult validationResult = await _validatorFactory.Validate(command);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            Reference author = ControllerContext.HttpContext.Request?.GetUserOrDefault();

            CommandResult result = await _templateBuilderService.UpdateTemplateContent(command, author);

            if (result.Succeeded)
            {
                return Ok(result.Version);
            }
            if (result.ValidationModelState != null)
            {
                return BadRequest(result.ValidationModelState);
            }

            return new InternalServerErrorResult(result.ErrorMessage ?? result.Exception?.Message ?? "Unknown error occurred");
        }

        /// <summary>
        /// Restores a template to a previous version
        /// </summary>
        /// <param name="command">Payload for restoring a template</param>
        /// <param name="templateId">TemplateId of the template to be restored</param>
        /// <param name="version">Version of the template to restore to</param>
        /// <returns>200 with response containing the new version number if restored successfully</returns>
        [HttpPut("api/templates/build/{templateId}/restore/{version}")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> RestoreContent(TemplateFundingLinesUpdateCommand command, [FromRoute] string templateId, [FromRoute] string version)
        {
            ValidationResult validationResult = await _validatorFactory.Validate(command);

            if (!validationResult.IsValid || command.TemplateId != templateId)
            {
                return validationResult.AsBadRequest();
            }

            Reference author = ControllerContext.HttpContext.Request?.GetUserOrDefault();

            CommandResult result = await _templateBuilderService.RestoreTemplateContent(command, author);

            if (result.Succeeded)
            {
                return Ok(result.Version);
            }
            if (result.ValidationModelState != null)
            {
                return BadRequest(result.ValidationModelState);
            }

            return new InternalServerErrorResult(result.ErrorMessage ?? result.Exception?.Message ?? "Unknown error occurred");
        }

        /// <summary>
        /// Update the description field of a template
        /// </summary>
        /// <param name="command">description for template</param>
        /// <returns>200 with no content if successful</returns>
        [HttpPut("api/templates/build/metadata")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateTemplateDescription(TemplateDescriptionUpdateCommand command)
        {
            ValidationResult validationResult = await _validatorFactory.Validate(command);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            Reference author = ControllerContext.HttpContext.Request?.GetUserOrDefault();

            CommandResult result = await _templateBuilderService.UpdateTemplateDescription(command, author);

            if (result.Succeeded)
            {
                return Ok();
            }

            if (result.ValidationResult != null)
            {
                return result.ValidationResult.AsBadRequest();
            }

            return new InternalServerErrorResult(result.ErrorMessage ?? result.Exception?.Message ?? "Unknown error occurred");
        }

        /// <summary>
        /// Get all versions of a template, filtered by status with paging, in descending last updated order
        /// </summary>
        /// <param name="templateId">template ID</param>
        /// <param name="statuses">optional [Draft, Published]</param>
        /// <param name="page">if 0 or missing, all results are returned (up to 1000)</param>
        /// <param name="itemsPerPage">if 0 or missing, all results are returned (up to 1000)</param>
        /// <returns>versions of a template matching input criteria</returns>
        [HttpGet("api/templates/build/{templateId}/versions")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TemplateSummaryResponse>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTemplateVersions([FromRoute] string templateId, [FromQuery] string statuses, [FromQuery] int page, [FromQuery] int itemsPerPage)
        {
            List<TemplateStatus> templateStatuses = !string.IsNullOrWhiteSpace(statuses) ? statuses.Split(',')
                .Select(s => (TemplateStatus)Enum.Parse(typeof(TemplateStatus), s))
                .ToList() : new List<TemplateStatus>();

            TemplateVersionListResponse templateVersionResponses =
                await _templateBuilderService.FindTemplateVersions(templateId, templateStatuses, page, itemsPerPage);

            return Ok(templateVersionResponses);
        }

        /// <summary>
        /// Find valid combinations of Funding streams and periods that have not been already allocated to a template
        /// </summary>
        /// <returns>collection of Funding streams with available periods</returns>
        [HttpGet("api/templates/build/available-stream-periods")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<FundingStreamWithPeriods>))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetFundingStreamPeriodsWithoutTemplates()
        {
            IEnumerable<FundingStreamWithPeriods> results = await _templateBuilderService.GetFundingStreamAndPeriodsWithoutTemplates();
            return Ok(results);
        }

        /// <summary>
        /// Search among versions of a specific template
        /// </summary>
        /// <param name="query">criteria for finding versions</param>
        /// <returns>search results</returns>
        [HttpPost("api/templates/build/versions/search")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<TemplateSummaryResponse>))]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetTemplateVersions(FindTemplateVersionQuery query)
        {
            ValidationResult validationResult = await _validatorFactory.Validate(query);
            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            IEnumerable<TemplateSummaryResponse> templateVersionResponses =
                await _templateBuilderService.FindVersionsByFundingStreamAndPeriod(query);

            return Ok(templateVersionResponses);
        }

        /// <summary>
        /// Publish a specific version of this template
        /// </summary>
        /// <param name="templateId">template ID</param>
        /// <param name="command">Inputs for publishing: version, notes</param>
        /// <returns>result with Succeeded flag</returns>
        [HttpPost("api/templates/build/{templateId}/publish")]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(CommandResult))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> PublishTemplate([FromRoute] string templateId, [FromBody] TemplatePublishCommand command)
        {
            Guard.ArgumentNotNull(command, nameof(command));

            command.Author = ControllerContext.HttpContext.Request?.GetUserOrDefault();

            CommandResult response = await _templateBuilderService.PublishTemplate(command);

            if (response.Succeeded)
            {
                return Ok(response);
            }

            if (response.ValidationResult != null)
            {
                return response.ValidationResult.AsBadRequest();
            }

            return new InternalServerErrorResult(response.ErrorMessage ?? response.Exception?.Message ?? "Unknown error occurred");
        }

        /// <summary>
        /// Search for templates
        /// (current versions only)
        /// </summary>
        /// <param name="searchModel">search filters</param>
        /// <returns>template results</returns>
        [Route("api/templates/templates-search")]
        [HttpPost]
        [Produces(typeof(TemplateSearchResults))]
        [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(TemplateSearchResults))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> TemplatesSearch([FromBody] SearchModel searchModel)
        {
            return await _templateSearchService.SearchTemplates(searchModel);
        }

        [HttpGet("api/templates/reindex")]
        [ProducesResponseType(204)]
        public async Task<IActionResult> ReIndex()
        {
            return await _templateSearchService.ReIndex(GetUser(),
              GetCorrelationId());
        }

        private Reference GetUser()
        {
            return Request.GetUser();
        }

        private string GetCorrelationId()
        {
            return Request.GetCorrelationId();
        }
    }
}