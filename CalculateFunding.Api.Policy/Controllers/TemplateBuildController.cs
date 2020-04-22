using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models.Policy.TemplateBuilder;
using CalculateFunding.Services.Policy.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Policy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CalculateFunding.Api.Policy.Controllers
{
    [ApiController]
    public class TemplateBuildController : ControllerBase
    {
        private readonly ITemplateBuilderService _templateBuilderService;

        public TemplateBuildController(ITemplateBuilderService templateBuilderService)
        {
            _templateBuilderService = templateBuilderService;
        }
        
        [HttpPost("api/templates/build")]
        [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(string))]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateTemplate(TemplateCreateCommand command)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            
            Reference author = ControllerContext.HttpContext.Request?.GetUserOrDefault();

            CreateTemplateResponse result = await _templateBuilderService.CreateTemplate(command, author);

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
    }
}