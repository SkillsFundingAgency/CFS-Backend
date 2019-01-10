﻿using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Common.Caching;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;

namespace CalculateFunding.Services.Calcs
{
    public class PreviewService : IPreviewService, IHealthChecker
    {
        private readonly ISourceFileGeneratorProvider _sourceFileGeneratorProvider;
        private readonly ILogger _logger;
        private readonly IBuildProjectsRepository _buildProjectsRepository;
        private readonly ICompilerFactory _compilerFactory;
        private readonly IValidator<PreviewRequest> _previewRequestValidator;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly IDatasetRepository _datasetRepository;
        private readonly IFeatureToggle _featureToggle;
        private readonly ICacheProvider _cacheProvider;

        public PreviewService(ISourceFileGeneratorProvider sourceFileGeneratorProvider,
            ILogger logger, IBuildProjectsRepository buildProjectsRepository, ICompilerFactory compilerFactory,
            IValidator<PreviewRequest> previewRequestValidator, ICalculationsRepository calculationsRepository,
            IDatasetRepository datasetRepository, IFeatureToggle featureToggle, ICacheProvider cacheProvider)
        {
            _sourceFileGeneratorProvider = sourceFileGeneratorProvider;
            _logger = logger;
            _buildProjectsRepository = buildProjectsRepository;
            _compilerFactory = compilerFactory;
            _previewRequestValidator = previewRequestValidator;
            _calculationsRepository = calculationsRepository;
            _datasetRepository = datasetRepository;
            _featureToggle = featureToggle;
            _cacheProvider = cacheProvider;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth calcsRepoHealth = await ((IHealthChecker)_calculationsRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(CalculationService)
            };
            health.Dependencies.AddRange(calcsRepoHealth.Dependencies);

            return health;
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

            List<Calculation> calculations = new List<Calculation>(calculationsTask.Result);

            Calculation calculation = calculations.FirstOrDefault(m => m.Id == previewRequest.CalculationId);
            if (calculation == null)
            {
                _logger.Warning($"Calculation ('{previewRequest.CalculationId}') could not be found for specification Id '{previewRequest.SpecificationId}'");
                return new PreconditionFailedResult($"Calculation ('{previewRequest.CalculationId}') could not be found for specification Id '{previewRequest.SpecificationId}'");
            }

            calculation.Current.SourceCode = previewRequest.SourceCode;

            if (_featureToggle.IsAggregateSupportInCalculationsEnabled())
            {
                Build build = await CheckDatasetValidAggregations(calculation, previewRequest);

                if (build != null && build.CompilerMessages.Any(m => m.Severity == Models.Calcs.Severity.Error))
                {
                    PreviewResponse response = new PreviewResponse
                    {
                        Calculation = calculation,
                        CompilerOutput = build
                    };

                    return new OkObjectResult(response);
                }

            }

            if (_featureToggle.IsAggregateOverCalculationsEnabled())
            {
                return GenerateAndCompile(buildProject, calculation, calculations, previewRequest);
            }
            else
            {
                return GenerateAndCompile(buildProject, calculation, calculations);
            }
        }

        private IActionResult GenerateAndCompile(BuildProject buildProject, Calculation calculationToPreview, IEnumerable<Calculation> calculations, PreviewRequest previewRequest = null)
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
            {
                _logger.Information($"Build compiled succesfully for calculation id {calculationToPreview.Id}");

                if (_featureToggle.IsAggregateOverCalculationsEnabled())
                {
                    string calculationIdentifier = VisualBasicTypeGenerator.GenerateIdentifier(calculationToPreview.Name);

                    IDictionary<string, string> functions = compiler.GetCalulationFunctions(compilerOutput.SourceFiles);

                    if (!functions.ContainsKey(calculationIdentifier))
                    {
                        compilerOutput.CompilerMessages.Add(new CompilerMessage { Message = $"{calculationIdentifier} is not an aggregatable field", Severity = Models.Calcs.Severity.Error });
                    }
                    else
                    {
                        if (previewRequest != null)
                        {
                            if (SourceCodeHelpers.GetCalculationAggregateFunctionParameters(previewRequest.SourceCode).Any())
                            {
                                if (SourceCodeHelpers.IsCalcReferencedInAnAggregate(functions, calculationIdentifier))
                                {
                                    compilerOutput.CompilerMessages.Add(new CompilerMessage { Message = $"{calculationIdentifier} is already referenced in an aggregation that would cause nesting", Severity = Models.Calcs.Severity.Error });
                                }
                                else if (SourceCodeHelpers.CheckSourceForExistingCalculationAggregates(functions, previewRequest.SourceCode))
                                {
                                    compilerOutput.CompilerMessages.Add(new CompilerMessage { Message = $"{calculationIdentifier} cannot reference another calc that is being aggregated", Severity = Models.Calcs.Severity.Error });
                                }
                            }
                        }
                    }
                   
                }
            }
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

        private async Task<Build> CheckDatasetValidAggregations(Calculation calculation, PreviewRequest previewRequest)
        {
            Build build = null;

            IEnumerable<string> aggregateParameters = SourceCodeHelpers.GetDatasetAggregateFunctionParameters(previewRequest.SourceCode);

            if (aggregateParameters.IsNullOrEmpty())
            {
                return build;
            }

            string cacheKey = $"{CacheKeys.DatasetRelationshipFieldsForSpecification}{previewRequest.SpecificationId}";

            IEnumerable<DatasetSchemaRelationshipModel> datasetSchemaRelationshipModels = Enumerable.Empty<DatasetSchemaRelationshipModel>();

            datasetSchemaRelationshipModels = await _cacheProvider.GetAsync<List<DatasetSchemaRelationshipModel>>(cacheKey);

            if (datasetSchemaRelationshipModels.IsNullOrEmpty())
            {
                datasetSchemaRelationshipModels = await _datasetRepository.GetDatasetSchemaRelationshipModelsForSpecificationId(previewRequest.SpecificationId);

                await _cacheProvider.SetAsync<List<DatasetSchemaRelationshipModel>>(cacheKey, datasetSchemaRelationshipModels.ToList());
            }

            HashSet<string> compilerErrors = new HashSet<string>();

            IEnumerable<string> datasetAggregationFields = datasetSchemaRelationshipModels?.SelectMany(m => m.Fields?.Where(f => f.IsAggregable).Select(f => f.FullyQualifiedSourceName));

            foreach (string aggregateParameter in aggregateParameters)
            {
                if (datasetAggregationFields.IsNullOrEmpty() || !datasetAggregationFields.Any(m => string.Equals(m.Trim(), aggregateParameter.Trim(), System.StringComparison.CurrentCultureIgnoreCase)))
                {
                    compilerErrors.Add($"{aggregateParameter} is not an aggretable field");
                }
            }

            if (compilerErrors.Any())
            {
                build = new Build
                {
                    CompilerMessages = compilerErrors.Select(m => new CompilerMessage { Message = m, Severity = Models.Calcs.Severity.Error }).ToList()
                };

            }
  
            return build;
        }

    }
}
