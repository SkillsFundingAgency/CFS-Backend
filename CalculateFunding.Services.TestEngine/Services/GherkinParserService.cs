using System.Net;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Calcs;
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
        private readonly ICalculationsApiClient _calcsApiClient;
        private readonly AsyncPolicy _calcsApiClientPolicy;
        private readonly IMapper _mapper;

        public GherkinParserService(
            IGherkinParser gherkinParser,
            ILogger logger,
            ICalculationsApiClient calcsApiClient,
            ITestRunnerResiliencePolicies resiliencePolicies,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsApiClient, nameof(resiliencePolicies.CalculationsApiClient));
            Guard.ArgumentNotNull(gherkinParser, nameof(gherkinParser));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(calcsApiClient, nameof(calcsApiClient));

            _gherkinParser = gherkinParser;
            _logger = logger;
            _calcsApiClient = calcsApiClient;
            _mapper = mapper;

            _calcsApiClientPolicy = resiliencePolicies.CalculationsApiClient;
        }

        public async Task<IActionResult> ValidateGherkin(ValidateGherkinRequestModel model)
        {
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

            BuildProject buildProject = _mapper.Map<BuildProject>(await _calcsApiClientPolicy.ExecuteAsync(() => _calcsApiClient.GetBuildProjectBySpecificationId(model.SpecificationId)));

            if (buildProject == null || buildProject.Build == null)
            {
                _logger.Error($"Failed to find a valid build project for specification id: {model.SpecificationId}");

                return new StatusCodeResult((int)HttpStatusCode.PreconditionFailed);
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
