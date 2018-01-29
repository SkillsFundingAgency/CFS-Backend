using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class MockPreviewService : IPreviewService
    {
        private readonly ILogger _logger;
        private readonly IBuildProjectsRepository _buildProjectsRepository;
        private readonly IValidator<PreviewRequest> _previewRequestValidator;
        private readonly ICalculationsRepository _calculationsRepository;

        public MockPreviewService(
           ILogger logger, IBuildProjectsRepository buildProjectsRepository,
           IValidator<PreviewRequest> previewRequestValidator, ICalculationsRepository calculationsRepository)
        {

            _logger = logger;
            _buildProjectsRepository = buildProjectsRepository;
            _previewRequestValidator = previewRequestValidator;
            _calculationsRepository = calculationsRepository;
        }

        public async Task<IActionResult> Compile(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            var previewRequest = JsonConvert.DeserializeObject<PreviewRequest>(json);

            if (previewRequest == null)
            {
                _logger.Error("A null preview request was supplied");

                return new BadRequestObjectResult("A null preview request was provided");
            }

            var validationResult = await _previewRequestValidator.ValidateAsync(previewRequest);

            if (!validationResult.IsValid)
            {
                string errors = string.Join(";", validationResult.Errors.Select(m => m.ErrorMessage).ToArraySafe());

                _logger.Error($"The preview request failed to validate with errors: {errors}");

                return new BadRequestObjectResult("The preview request failed to validate");
            }

            Calculation calculation = await _calculationsRepository.GetCalculationById(previewRequest.CalculationId);

            if (calculation == null)
            {
                _logger.Error($"Calculation could not be found for calculation id {previewRequest.CalculationId}");
                return new StatusCodeResult(412);
            }

            if (string.IsNullOrWhiteSpace(calculation.BuildProjectId))
            {
                _logger.Error($"Calculation with id {calculation.Id} does not contain a build project id");
                return new StatusCodeResult(412);
            }

            BuildProject buildProject = await _buildProjectsRepository.GetBuildProjectById(calculation.BuildProjectId);

            if (buildProject == null)
            {
                _logger.Error($"Build project for build project id {calculation.BuildProjectId} could not be found");

                return new StatusCodeResult(412);
            }

            calculation = buildProject.Calculations?.FirstOrDefault(m => m.Id == previewRequest.CalculationId);

            if (calculation == null)
            {
                _logger.Error($"Calculation could not be found for on build project id {buildProject.Id}");
                return new StatusCodeResult(412);
            }

            calculation.Current.SourceCode = previewRequest.SourceCode;

            PreviewResponse previewResponse = new PreviewResponse
            {
                Calculation = calculation,
                CompilerOutput = new Build
                {
                    Success = !previewRequest.SourceCode.Contains("dataset")
                }
            };

            if (!previewResponse.CompilerOutput.Success)
            {
                previewResponse.CompilerOutput.CompilerMessages = new[]
                {
                    new CompilerMessage
                    {
                        Severity = Models.Calcs.Severity.Error,
                        Message = "'Console' is not declared. It may be inaccessible due to its protection level."
                    }
                }.ToList();
            }

            return new OkObjectResult(previewResponse);
        }

       
    }
}
