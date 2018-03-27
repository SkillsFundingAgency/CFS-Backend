using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class GherkinParserService : IGherkinParserService
    {
        private readonly IGherkinParser _gherkinParser;
        private readonly ILogger _logger;
        private readonly IBuildProjectRepository _buildProjectRepository;

        public GherkinParserService(
            IGherkinParser gherkinParser, 
            ILogger logger,
            IBuildProjectRepository buildProjectRepository)
        {
            _gherkinParser = gherkinParser;
            _logger = logger;
            _buildProjectRepository = buildProjectRepository;
        }

        public async Task<IActionResult> ValidateGherkin(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            ValidateGherkinRequestModel model = JsonConvert.DeserializeObject<ValidateGherkinRequestModel>(json);

            if(model == null)
            {
                _logger.Error("Null model was provided to ValidateGherkin");
                return new BadRequestObjectResult("Null or empty specification id provided");
            }

            if (string.IsNullOrWhiteSpace(model.SpecificationId))
            {
                _logger.Error("No specification id was provided to ValidateGherkin");
                return new BadRequestObjectResult("Null or empty specification id provided");
            }

            if (string.IsNullOrWhiteSpace(model.Gherkin))
            {
                _logger.Error("Null or empty gherkin was provided to ValidateGherkin");
                return new BadRequestObjectResult("Null or empty gherkin name provided");
            }

            BuildProject buildProject = await _buildProjectRepository.GetBuildProjectBySpecificationId(model.SpecificationId);

            if(buildProject == null || buildProject.Build == null)
            {
                _logger.Error($"Failed to find a valid build project for specification id: {model.SpecificationId}");

                return new StatusCodeResult(412);
            }

            GherkinParseResult parseResult = await _gherkinParser.Parse(model.Gherkin, buildProject);

            if (parseResult.HasErrors)
            {
                _logger.Information($"Gherkin parser failed validation with ");
            }

            return new OkObjectResult(parseResult.Errors);
        }
    }
}
