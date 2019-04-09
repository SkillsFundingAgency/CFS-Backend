using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler;
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
        public const string DoubleToNullableDecimalErrorMessage = "Option Strict On disallows implicit conversions from 'Double' to 'Decimal?'.";
        public const string NullableDoubleToDecimalErrorMessage = "Option Strict On disallows implicit conversions from 'Double?' to 'Decimal?'.";
        public const string DoubleToDecimalErrorMessage = "Option Strict On disallows implicit conversions from 'Double' to 'Decimal'.";

        private readonly ILogger _logger;
        private readonly IBuildProjectsService _buildProjectsService;
        private readonly IValidator<PreviewRequest> _previewRequestValidator;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly IDatasetRepository _datasetRepository;
        private readonly IFeatureToggle _featureToggle;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISourceCodeService _sourceCodeService;

        public PreviewService(
            ILogger logger,
            IBuildProjectsService buildProjectsService,
            IValidator<PreviewRequest> previewRequestValidator, 
            ICalculationsRepository calculationsRepository,
            IDatasetRepository datasetRepository, 
            IFeatureToggle featureToggle, 
            ICacheProvider cacheProvider, 
            ISourceCodeService sourceCodeService)
        {
            _logger = logger;
            _buildProjectsService = buildProjectsService;
            _previewRequestValidator = previewRequestValidator;
            _calculationsRepository = calculationsRepository;
            _datasetRepository = datasetRepository;
            _featureToggle = featureToggle;
            _cacheProvider = cacheProvider;
            _sourceCodeService = sourceCodeService;
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

            Task<BuildProject> buildProjectTask = _buildProjectsService.GetBuildProjectForSpecificationId(previewRequest.SpecificationId);
            Task<CompilerOptions> compilerOptionsTask = _calculationsRepository.GetCompilerOptions(previewRequest.SpecificationId);

            await TaskHelper.WhenAllAndThrow(calculationsTask, buildProjectTask, compilerOptionsTask);

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

            CompilerOptions compilerOptions = compilerOptionsTask.Result;

            if (_featureToggle.IsAggregateOverCalculationsEnabled())
            {
                return await GenerateAndCompile(buildProject, calculation, calculations, compilerOptions, previewRequest);
            }
            else
            {
                return await GenerateAndCompile(buildProject, calculation, calculations, compilerOptions);
            }
        }

        private async Task<IActionResult> GenerateAndCompile(BuildProject buildProject, Calculation calculationToPreview, IEnumerable<Calculation> calculations, CompilerOptions compilerOptions, PreviewRequest previewRequest = null)
        {
            Build compilerOutput = _sourceCodeService.Compile(buildProject, calculations, compilerOptions);

            compilerOutput = FilterDoubleToDecimalErrors(compilerOutput);

            if (compilerOutput.Success)
            {
                _logger.Information($"Build compiled succesfully for calculation id {calculationToPreview.Id}");

                if (_featureToggle.IsAggregateOverCalculationsEnabled())
                {
                    string calculationIdentifier = VisualBasicTypeGenerator.GenerateIdentifier(calculationToPreview.Name);

                    IDictionary<string, string> functions = _sourceCodeService.GetCalulationFunctions(compilerOutput.SourceFiles);

                    if (!functions.ContainsKey(calculationIdentifier))
                    {
                        compilerOutput.Success = false;
                        compilerOutput.CompilerMessages.Add(new CompilerMessage { Message = $"{calculationIdentifier} is not an aggregatable field", Severity = Models.Calcs.Severity.Error });
                    }
                    else
                    {
                        if (previewRequest != null)
                        {
                            IEnumerable<string> aggregateParameters = SourceCodeHelpers.GetCalculationAggregateFunctionParameters(previewRequest.SourceCode);

                            bool continueChecking = true;

                            if (!aggregateParameters.IsNullOrEmpty())
                            {
                                foreach(string aggregateParameter in aggregateParameters)
                                {
                                    if (!functions.ContainsKey(aggregateParameter))
                                    {
                                        compilerOutput.Success = false;
                                        compilerOutput.CompilerMessages.Add(new CompilerMessage { Message = $"{aggregateParameter} is not an aggregatable field", Severity = Models.Calcs.Severity.Error });
                                        continueChecking = false;
                                        break;
                                    }
                                }

                                if (continueChecking)
                                {
                                    if (SourceCodeHelpers.IsCalcReferencedInAnAggregate(functions, calculationIdentifier))
                                    {
                                        compilerOutput.Success = false;
                                        compilerOutput.CompilerMessages.Add(new CompilerMessage { Message = $"{calculationIdentifier} is already referenced in an aggregation that would cause nesting", Severity = Models.Calcs.Severity.Error });
                                    }
                                    else if (SourceCodeHelpers.CheckSourceForExistingCalculationAggregates(functions, previewRequest.SourceCode))
                                    {
                                        compilerOutput.Success = false;
                                        compilerOutput.CompilerMessages.Add(new CompilerMessage { Message = $"{calculationIdentifier} cannot reference another calc that is being aggregated", Severity = Models.Calcs.Severity.Error });
                                    }
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

            await _sourceCodeService.SaveSourceFiles(compilerOutput.SourceFiles, buildProject.SpecificationId, SourceCodeType.Preview);

            if (compilerOutput.Success)
            {
                Build nonPreviewCompilerOutput = _sourceCodeService.Compile(buildProject, calculations, new CompilerOptions { SpecificationId = buildProject.SpecificationId, OptionStrictEnabled = false });

                if (nonPreviewCompilerOutput.Success)
                {
                    await _sourceCodeService.SaveSourceFiles(nonPreviewCompilerOutput.SourceFiles, buildProject.SpecificationId, SourceCodeType.Release);
                }
            }

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

        private Build FilterDoubleToDecimalErrors(Build compilerOutput)
        {
            if (compilerOutput.CompilerMessages.IsNullOrEmpty())
            {
                return compilerOutput;
            }

            compilerOutput.CompilerMessages = compilerOutput.CompilerMessages.Where(m => 
            m.Message != DoubleToNullableDecimalErrorMessage &&
            m.Message != NullableDoubleToDecimalErrorMessage &&
            m.Message != DoubleToDecimalErrorMessage).ToList();

            compilerOutput.Success  = !compilerOutput.CompilerMessages.AnyWithNullCheck(m => m.Severity == Models.Calcs.Severity.Error);

            return compilerOutput;
        }
    }
}
