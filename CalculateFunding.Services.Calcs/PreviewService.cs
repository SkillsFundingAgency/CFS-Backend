using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Severity = CalculateFunding.Models.Calcs.Severity;

namespace CalculateFunding.Services.Calcs
{
    public class PreviewService : IPreviewService, IHealthChecker
    {
        public const string DoubleToNullableDecimalErrorMessage = "Option Strict On disallows implicit conversions from 'Double' to 'Decimal?'.";
        public const string NullableDoubleToDecimalErrorMessage = "Option Strict On disallows implicit conversions from 'Double?' to 'Decimal?'.";
        public const string DoubleToDecimalErrorMessage = "Option Strict On disallows implicit conversions from 'Double' to 'Decimal'.";
        public const string TempCalculationId = "temp-calc-id";
        public const string TempCalculationName = "Temp Calc";

        private readonly ILogger _logger;
        private readonly IBuildProjectsService _buildProjectsService;
        private readonly IValidator<PreviewRequest> _previewRequestValidator;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly IDatasetsApiClient _datasetsApiClient;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISourceCodeService _sourceCodeService;
        private readonly ITokenChecker _tokenChecker;
        private readonly Polly.AsyncPolicy _datasetsApiClientPolicy;
        private readonly IMapper _mapper;

        public PreviewService(
            ILogger logger,
            IBuildProjectsService buildProjectsService,
            IValidator<PreviewRequest> previewRequestValidator,
            ICalculationsRepository calculationsRepository,
            IDatasetsApiClient datasetsApiClient,
            ICacheProvider cacheProvider,
            ISourceCodeService sourceCodeService,
            ITokenChecker tokenChecker,
            ICalcsResiliencePolicies resiliencePolicies,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(buildProjectsService, nameof(buildProjectsService));
            Guard.ArgumentNotNull(previewRequestValidator, nameof(previewRequestValidator));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(datasetsApiClient, nameof(datasetsApiClient));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(sourceCodeService, nameof(sourceCodeService));
            Guard.ArgumentNotNull(tokenChecker, nameof(tokenChecker));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _logger = logger;
            _buildProjectsService = buildProjectsService;
            _previewRequestValidator = previewRequestValidator;
            _calculationsRepository = calculationsRepository;
            _datasetsApiClient = datasetsApiClient;
            _cacheProvider = cacheProvider;
            _sourceCodeService = sourceCodeService;
            _tokenChecker = tokenChecker;
            _datasetsApiClientPolicy = resiliencePolicies.DatasetsApiClient;
            _mapper = mapper;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth calcsRepoHealth = await ((IHealthChecker)_calculationsRepository).IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(CalculationService)
            };
            health.Dependencies.AddRange(calcsRepoHealth.Dependencies);

            return health;
        }

        public async Task<IActionResult> Compile(PreviewRequest previewRequest)
        {
            if (previewRequest == null)
            {
                _logger.Error("A null preview request was supplied");

                return new BadRequestObjectResult("A null preview request was provided");
            }

            if (string.IsNullOrWhiteSpace(previewRequest.CalculationId))
            {
                previewRequest.CalculationId = TempCalculationId;
            }

            Calculation tempCalculation = new Calculation
            {
                Id = TempCalculationId,
                SpecificationId = previewRequest.SpecificationId,
                Current = new CalculationVersion
                {
                    Name = !string.IsNullOrWhiteSpace(previewRequest.Name) ? previewRequest.Name : TempCalculationName,
                    CalculationId = TempCalculationId,
                    SourceCodeName = VisualBasicTypeGenerator.GenerateIdentifier(!string.IsNullOrWhiteSpace(previewRequest.Name) ? previewRequest.Name : TempCalculationName),
                    SourceCode = previewRequest.SourceCode,
                }
            };

            FluentValidation.Results.ValidationResult validationResult = await _previewRequestValidator.ValidateAsync(previewRequest);

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
                calculation = tempCalculation;
                calculation.Current.Namespace = CalculationNamespace.Additional;
                calculations.Add(tempCalculation);
            }

            calculation.Current.SourceCode = previewRequest.SourceCode;

            Build build = await CheckDatasetValidAggregations(previewRequest);

            if (build != null && build.CompilerMessages.Any(m => m.Severity == Severity.Error))
            {
                PreviewResponse response = new PreviewResponse
                {
                    Calculation = calculation.ToResponseModel(),
                    CompilerOutput = build
                };

                return new OkObjectResult(response);
            }

            CompilerOptions compilerOptions = compilerOptionsTask.Result ?? new CompilerOptions { SpecificationId = buildProject.SpecificationId };

            return await GenerateAndCompile(buildProject, calculation, calculations, compilerOptions, previewRequest);
        }

        private async Task<IActionResult> GenerateAndCompile(BuildProject buildProject,
            Calculation calculationToPreview,
            IEnumerable<Calculation> calculations,
            CompilerOptions compilerOptions,
            PreviewRequest previewRequest)
        {
            Build compilerOutput = _sourceCodeService.Compile(buildProject, calculations, compilerOptions);

            compilerOutput = FilterDoubleToDecimalErrors(compilerOutput);

            if (compilerOutput.SourceFiles != null)
            {
                await _sourceCodeService.SaveSourceFiles(compilerOutput.SourceFiles, buildProject.SpecificationId, SourceCodeType.Preview);
            }

            if (compilerOutput.Success)
            {
                _logger.Information($"Build compiled successfully for calculation id {calculationToPreview.Id}");

                string calculationIdentifier = $"{VisualBasicTypeGenerator.GenerateIdentifier(calculationToPreview.Namespace)}.{VisualBasicTypeGenerator.GenerateIdentifier(calculationToPreview.Name)}";

                IDictionary<string, string> functions = _sourceCodeService.GetCalculationFunctions(compilerOutput.SourceFiles);
                IDictionary<string, string> calculationIdentifierMap = calculations
                    .Select(_ => new
                    {
                        Identifier = $"{VisualBasicTypeGenerator.GenerateIdentifier(_.Namespace)}.{VisualBasicTypeGenerator.GenerateIdentifier(_.Name)}",
                        CalcName = _.Name
                    })
                    .ToDictionary(d => d.Identifier, d => d.CalcName);

                if (!functions.ContainsKey(calculationIdentifier))
                {
                    compilerOutput.Success = false;
                    compilerOutput.CompilerMessages.Add(new CompilerMessage { Message = $"{calculationIdentifier} is not an aggregable field", Severity = Severity.Error });
                }
                else
                {
                    if (previewRequest != null)
                    {
                        if (!SourceCodeHelpers.HasReturn(previewRequest.SourceCode))
                        {
                            compilerOutput.Success = false;
                            compilerOutput.CompilerMessages.Add(new CompilerMessage { Message = $"{calculationIdentifier} must have a return statement so that a calculation result will be returned", Severity = Severity.Error });
                        }
                        else
                        {
                            IEnumerable<string> aggregateParameters = SourceCodeHelpers.GetCalculationAggregateFunctionParameters(previewRequest.SourceCode);

                            bool continueChecking = true;

                            if (!aggregateParameters.IsNullOrEmpty())
                            {
                                foreach (string aggregateParameter in aggregateParameters)
                                {
                                    if (!functions.ContainsKey(aggregateParameter))
                                    {
                                        compilerOutput.Success = false;
                                        compilerOutput.CompilerMessages.Add(new CompilerMessage { Message = $"{aggregateParameter} is not an aggregable field", Severity = Severity.Error });
                                        continueChecking = false;
                                        break;
                                    }

                                    if (calculationIdentifierMap.ContainsKey(aggregateParameter))
                                    {
                                        Calculation calculation = calculations.SingleOrDefault(_ => _.Name == calculationIdentifierMap[aggregateParameter]);

                                        if (calculation.Current.DataType != CalculationDataType.Decimal)
                                        {
                                            compilerOutput.Success = false;
                                            compilerOutput.CompilerMessages.Add(new CompilerMessage
                                            {
                                                Message =
                                                $"Only decimal fields can be used on aggregation. {aggregateParameter} has data type of {calculation.Current.DataType}",
                                                Severity = Severity.Error
                                            });
                                            continueChecking = false;
                                            break;
                                        }
                                    }
                                }

                                if (continueChecking)
                                {
                                    if (SourceCodeHelpers.IsCalcReferencedInAnAggregate(functions, calculationIdentifier))
                                    {
                                        compilerOutput.Success = false;
                                        compilerOutput.CompilerMessages.Add(new CompilerMessage { Message = $"{calculationIdentifier} is already referenced in an aggregation that would cause nesting", Severity = Severity.Error });
                                    }
                                    else if (SourceCodeHelpers.CheckSourceForExistingCalculationAggregates(functions, previewRequest.SourceCode))
                                    {
                                        compilerOutput.Success = false;
                                        compilerOutput.CompilerMessages.Add(new CompilerMessage { Message = $"{calculationIdentifier} cannot reference another calc that is being aggregated", Severity = Severity.Error });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                _logger.Information($"Build did not compile successfully for calculation id {calculationToPreview.Id}");
            }

            LogMessages(compilerOutput, buildProject, calculationToPreview);

            return new OkObjectResult(new PreviewResponse
            {
                Calculation = calculationToPreview.ToResponseModel(),
                CompilerOutput = compilerOutput
            });
        }

        public void LogMessages(Build compilerOutput, BuildProject buildProject, Calculation calculation)
        {
            if (compilerOutput?.CompilerMessages?.Any() ?? false)
            {
                string specificationId = buildProject.SpecificationId;
                string calculationId = calculation.Id;
                string calculationName = calculation.Name;

                foreach (var compilerMessage in compilerOutput.CompilerMessages)
                {
                    string logMessage = $@"Error while compiling code preview: {compilerMessage.Message}
Line: {compilerMessage.Location?.StartLine + 1}

Specification ID: {{specificationId}}
Calculation ID: {{calculationId}}
Calculation Name: {{calculationName}}";

                    switch (compilerMessage.Severity)
                    {
                        case Severity.Info:
                            _logger.Verbose(logMessage,
                                specificationId,
                                calculationId,
                                calculationName);
                            break;

                        case Severity.Warning:
                            _logger.Warning(logMessage,
                                specificationId,
                                calculationId,
                                calculationName);
                            break;

                        case Severity.Error:
                            _logger.Error(logMessage,
                                specificationId,
                                calculationId,
                                calculationName);
                            break;
                    }
                }
            }
        }

        private async Task<Build> CheckDatasetValidAggregations(PreviewRequest previewRequest)
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
                ApiResponse<IEnumerable<Common.ApiClient.DataSets.Models.DatasetSchemaRelationshipModel>> datasetsApiClientResponse = await _datasetsApiClientPolicy.ExecuteAsync(() => _datasetsApiClient.GetDatasetSchemaRelationshipModelsForSpecificationId(previewRequest.SpecificationId));

                if (!datasetsApiClientResponse.StatusCode.IsSuccess())
                {
                    string message = $"No dataset schema relationship found for specificationId '{previewRequest.SpecificationId}'.";
                    _logger.Error(message);
                    throw new RetriableException(message);
                }

                if (datasetsApiClientResponse.Content != null)
                {
                    datasetSchemaRelationshipModels = _mapper.Map<IEnumerable<DatasetSchemaRelationshipModel>>(datasetsApiClientResponse.Content);
                }

                await _cacheProvider.SetAsync<List<DatasetSchemaRelationshipModel>>(cacheKey, datasetSchemaRelationshipModels.ToList());
            }

            HashSet<string> compilerErrors = new HashSet<string>();

            IEnumerable<string> datasetAggregationFields = datasetSchemaRelationshipModels?.SelectMany(m => m.Fields?.Where(f => f.IsAggregable).Select(f => f.FullyQualifiedSourceName));

            foreach (string aggregateParameter in aggregateParameters)
            {
                if (datasetAggregationFields.IsNullOrEmpty() || !datasetAggregationFields.Any(m => string.Equals(m.Trim(), aggregateParameter.Trim(), StringComparison.CurrentCultureIgnoreCase)))
                {
                    compilerErrors.Add($"{aggregateParameter} is not an aggregable field");
                }
            }

            if (compilerErrors.Any())
            {
                build = new Build
                {
                    CompilerMessages = compilerErrors.Select(m => new CompilerMessage { Message = m, Severity = Severity.Error }).ToList()
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

            compilerOutput.CompilerMessages = compilerOutput.CompilerMessages
                .Where(m => m.Message != DoubleToNullableDecimalErrorMessage &&
                        m.Message != NullableDoubleToDecimalErrorMessage &&
                        m.Message != DoubleToDecimalErrorMessage)
                .ToList();

            compilerOutput.Success = !compilerOutput.CompilerMessages.AnyWithNullCheck(m => m.Severity == Severity.Error);

            return compilerOutput;
        }
    }
}
