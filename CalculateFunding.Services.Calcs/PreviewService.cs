using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Calcs
{
    public class PreviewService : IPreviewService
    {
        private readonly ISourceFileGeneratorProvider _sourceFileGeneratorProvider;
        private readonly ILogger _logger;
        private readonly IBuildProjectsRepository _buildProjectsRepository;
        private readonly ICompilerFactory _compilerFactory;
        private readonly IValidator<PreviewRequest> _previewRequestValidator;
        private readonly ICalculationsRepository _calculationsRepository;

        public PreviewService(ISourceFileGeneratorProvider sourceFileGeneratorProvider,
            ILogger logger, IBuildProjectsRepository buildProjectsRepository, ICompilerFactory compilerFactory,
            IValidator<PreviewRequest> previewRequestValidator, ICalculationsRepository calculationsRepository)
        {
            _sourceFileGeneratorProvider = sourceFileGeneratorProvider;
            _logger = logger;
            _buildProjectsRepository = buildProjectsRepository;
            _compilerFactory = compilerFactory;
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

                _logger.Warning($"The preview request failed to validate with errors: {errors}");

                return new BadRequestObjectResult("The preview request failed to validate");
            }

            Calculation calculation = await _calculationsRepository.GetCalculationById(previewRequest.CalculationId);

            if (calculation == null)
            {
                _logger.Error($"Calculation could not be found for calculation id {previewRequest.CalculationId}");
                return new StatusCodeResult(412);
            }

            BuildProject buildProject = await _buildProjectsRepository.GetBuildProjectBySpecificationId(calculation.SpecificationId);
            if (buildProject == null)
            {
                _logger.Error($"Build project for specification {calculation.SpecificationId} could not be found");

                return new StatusCodeResult(412);
            }

            calculation = buildProject.Calculations?.FirstOrDefault(m => m.Id == previewRequest.CalculationId);

            if (calculation == null)
            {
                _logger.Error($"Calculation could not be found for on build project id {buildProject.Id}");
                return new StatusCodeResult(412);
            }

            calculation.Current.SourceCode = previewRequest.SourceCode;

            return GenerateAndCompile(buildProject, calculation);
        }

        IActionResult GenerateAndCompile(BuildProject buildProject, Calculation calculation)
        {
            ISourceFileGenerator sourceFileGenerator = _sourceFileGeneratorProvider.CreateSourceFileGenerator(TargetLanguage.VisualBasic);

            if (sourceFileGenerator == null)
            {
                _logger.Error("Source file generator was not created");

                return new StatusCodeResult(500);
            }

            IEnumerable<SourceFile> sourceFiles = sourceFileGenerator.GenerateCode(buildProject);

            if (sourceFiles.IsNullOrEmpty())
            {
                _logger.Error("Source file generator did not generate any source file");

                return new StatusCodeResult(500);
            }

            ICompiler compiler = _compilerFactory.GetCompiler(sourceFiles);

            Build compilerOutput = compiler.GenerateCode(sourceFiles.ToList());

            if (compilerOutput.Success)
                _logger.Information($"Build compiled succesfully for calculation id {calculation.Id}");
            else
            {
                _logger.Information($"Build did not compile succesfully for calculation id {calculation.Id}");
            }


            PreviewResponse response = new PreviewResponse()
            {
                Calculation = calculation,
                CompilerOutput = compilerOutput
            };

            return new OkObjectResult(response);
        }

    }

}
