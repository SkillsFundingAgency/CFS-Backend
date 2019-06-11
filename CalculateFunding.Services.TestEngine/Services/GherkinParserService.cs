using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Gherkin;
using CalculateFunding.Models.Scenarios;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.TestRunner.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Polly;
using Serilog;

namespace CalculateFunding.Services.TestRunner.Services
{
    public class GherkinParserService : IGherkinParserService
    {
        private readonly IGherkinParser _gherkinParser;
        private readonly ILogger _logger;
        private readonly IBuildProjectRepository _buildProjectRepository;
        private readonly Policy _buildProjectRepositoryPolicy;

        public GherkinParserService(
            IGherkinParser gherkinParser,
            ILogger logger,
            IBuildProjectRepository buildProjectRepository,
            ITestRunnerResiliencePolicies resiliencePolicies)
        {
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));

            _gherkinParser = gherkinParser;
            _logger = logger;
            _buildProjectRepository = buildProjectRepository;

            _buildProjectRepositoryPolicy = resiliencePolicies.BuildProjectRepository;
        }

        public async Task<IActionResult> ValidateGherkin(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            ValidateGherkinRequestModel model = JsonConvert.DeserializeObject<ValidateGherkinRequestModel>(json);

            if (model == null)
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

            BuildProject buildProject = await _buildProjectRepositoryPolicy.ExecuteAsync(() => _buildProjectRepository.GetBuildProjectBySpecificationId(model.SpecificationId));

            if (buildProject == null || buildProject.Build == null)
            {
                _logger.Error($"Failed to find a valid build project for specification id: {model.SpecificationId}");

                return new StatusCodeResult(412);
            }

            GherkinParseResult parseResult = await _gherkinParser.Parse(model.SpecificationId, model.Gherkin, buildProject);

            if (parseResult.HasErrors)
            {
                _logger.Information($"Gherkin parser failed validation with ");
            }

            return new OkObjectResult(parseResult.Errors);
        }
    }
}
