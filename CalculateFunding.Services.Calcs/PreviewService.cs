using CalculateFunding.Models.Calcs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
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

            Task<IEnumerable<Calculation>> calculationsTask = _calculationsRepository.GetCalculationsBySpecificationId(previewRequest.SpecificationId);
            Task<BuildProject> buildProjectTask = _buildProjectsRepository.GetBuildProjectBySpecificationId(previewRequest.SpecificationId);

            await TaskHelper.WhenAllAndThrow(calculationsTask, buildProjectTask);

            BuildProject buildProject = buildProjectTask.Result;
            if (buildProject == null)
            {
                _logger.Warning($"Build project for specification '{previewRequest.SpecificationId}' could not be found");

                return new PreconditionFailedResult($"Build project for specification '{previewRequest.SpecificationId}' could not be found");
            }

            IEnumerable<Calculation> calculations = calculationsTask.Result;

            Calculation calculation = calculations.FirstOrDefault(m => m.Id == previewRequest.CalculationId);
            if (calculation == null)
            {
                _logger.Warning($"Calculation ('{previewRequest.CalculationId}') could not be found for specification Id '{previewRequest.SpecificationId}'");
                return new PreconditionFailedResult($"Calculation ('{previewRequest.CalculationId}') could not be found for specification Id '{previewRequest.SpecificationId}'");
            }

            calculation.Current.SourceCode = previewRequest.SourceCode;

            return GenerateAndCompile(buildProject, calculation, calculations);
        }

        IActionResult GenerateAndCompile(BuildProject buildProject, Calculation calculationToPreview, IEnumerable<Calculation> calculations)
        {
            ISourceFileGenerator sourceFileGenerator = _sourceFileGeneratorProvider.CreateSourceFileGenerator(TargetLanguage.VisualBasic);

            if (sourceFileGenerator == null)
            {
                _logger.Warning("Source file generator was not created");

                return new InternalServerErrorResult("Source file generator was not created");
            }

            IEnumerable<SourceFile> sourceFiles = sourceFileGenerator.GenerateCode(buildProject, calculations);

            if (sourceFiles.IsNullOrEmpty())
            {
                _logger.Warning("Source file generator did not generate any source file");

                return new InternalServerErrorResult("Source file generator did not generate any source file");
            }

            ICompiler compiler = _compilerFactory.GetCompiler(sourceFiles);

            Build compilerOutput = compiler.GenerateCode(sourceFiles.ToList());

            if (compilerOutput.Success)
                _logger.Information($"Build compiled succesfully for calculation id {calculationToPreview.Id}");
            else
            {
                _logger.Information($"Build did not compile succesfully for calculation id {calculationToPreview.Id}");
            }


            PreviewResponse response = new PreviewResponse()
            {
                Calculation = calculationToPreview,
                CompilerOutput = compilerOutput
            };

            return new OkObjectResult(response);
        }

    }

}
