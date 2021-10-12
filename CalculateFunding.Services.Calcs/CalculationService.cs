using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Results;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Calcs.ObsoleteItems;
using CalculateFunding.Models.Code;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Analysis.ObsoleteItems;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.ResultModels;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Processing;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Polly;
using Serilog;
using ApiClientDatasetDefinition = CalculateFunding.Common.ApiClient.DataSets.Models.DatasetDefinition;
using ApiClientFieldDefinition = CalculateFunding.Common.ApiClient.DataSets.Models.FieldDefinition;
using ApiClientSelectDatasourceModel = CalculateFunding.Common.ApiClient.DataSets.Models.SelectDatasourceModel;
using ApiClientTableDefinition = CalculateFunding.Common.ApiClient.DataSets.Models.TableDefinition;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using CalculationResponseModel = CalculateFunding.Models.Calcs.CalculationResponseModel;
using CalculationType = CalculateFunding.Models.Calcs.CalculationType;
using ApiClientJob = CalculateFunding.Common.ApiClient.Jobs.Models.Job;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type;
using CalculateFunding.Services.CodeGeneration.VisualBasic.Type.Interfaces;
using AutoMapper;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationService : ProcessingService, ICalculationService, IHealthChecker
    {
        public const string reasonForCommenting = "The dataset definition referenced by this calc has been updated and subsequently the code has been commented out";
        public const string exceptionMessage = "Code commented out for definition field updates";
        public const string exceptionType = "DatasetReferenceChangeException";

        private readonly ICalculationsRepository _calculationsRepository;
        private readonly ILogger _logger;
        private readonly ISearchRepository<CalculationIndex> _searchRepository;
        private readonly IBuildProjectsService _buildProjectsService;
        private readonly ICacheProvider _cacheProvider;
        private readonly Polly.AsyncPolicy _calculationRepositoryPolicy;
        private readonly Polly.AsyncPolicy _calculationSearchRepositoryPolicy;
        private readonly Polly.AsyncPolicy _cachePolicy;
        private readonly Polly.AsyncPolicy _calculationVersionsRepositoryPolicy;
        private readonly IVersionRepository<CalculationVersion> _calculationVersionRepository;
        private readonly ISourceCodeService _sourceCodeService;
        private readonly IFeatureToggle _featureToggle;
        private readonly IBuildProjectsRepository _buildProjectsRepository;
        private readonly Polly.AsyncPolicy _buildProjectRepositoryPolicy;
        private readonly ICalculationCodeReferenceUpdate _calculationCodeReferenceUpdate;
        private readonly IValidator<CalculationCreateModel> _calculationCreateModelValidator;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly Polly.AsyncPolicy _policiesApiClientPolicy;
        private readonly IResultsApiClient _resultsApiClient;
        private readonly IApproveAllCalculationsJobAction _approveAllCalculationsJobAction;
        private readonly Polly.AsyncPolicy _resultsApiClientPolicy;
        private readonly IDatasetsApiClient _datasetsApiClient;
        private readonly AsyncPolicy _datasetsApiClientPolicy;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly Polly.AsyncPolicy _specificationsApiClientPolicy;
        private readonly IValidator<CalculationEditModel> _calculationEditModelValidator;
        private readonly ICalculationNameInUseCheck _calculationNameInUseCheck;
        private readonly IInstructionAllocationJobCreation _instructionAllocationJobCreation;
        private readonly ICreateCalculationService _createCalculationService;
        private readonly IGraphRepository _graphRepository;
        private readonly IJobManagement _jobManagement;
        private readonly ICodeContextCache _codeContextCache;
        private readonly ITypeIdentifierGenerator _typeIdentifierGenerator;
        private readonly IObsoleteItemCleanup _obsoleteItemCleanup;
        private readonly IMapper _mapper;

        public CalculationService(
            ICalculationsRepository calculationsRepository,
            ILogger logger,
            ISearchRepository<CalculationIndex> searchRepository,
            IBuildProjectsService buildProjectsService,
            IPoliciesApiClient policiesApiClient,
            ICacheProvider cacheProvider,
            ICalcsResiliencePolicies resiliencePolicies,
            IVersionRepository<CalculationVersion> calculationVersionRepository,
            ISourceCodeService sourceCodeService,
            IFeatureToggle featureToggle,
            IBuildProjectsRepository buildProjectsRepository,
            ICalculationCodeReferenceUpdate calculationCodeReferenceUpdate,
            IValidator<CalculationCreateModel> calculationCreateModelValidator,
            IValidator<CalculationEditModel> calculationEditModelValidator,
            ISpecificationsApiClient specificationsApiClient,
            ICalculationNameInUseCheck calculationNameInUseCheck,
            IInstructionAllocationJobCreation instructionAllocationJobCreation,
            ICreateCalculationService createCalculationService,
            IGraphRepository graphRepository,
            IJobManagement jobManagement,
            ICodeContextCache codeContextCache,
            IResultsApiClient resultsApiClient,
            IDatasetsApiClient datasetsApiClient,
            IApproveAllCalculationsJobAction approveAllCalculationsJobAction,
            IObsoleteItemCleanup obsoleteItemCleanup,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(buildProjectsService, nameof(buildProjectsService));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(calculationVersionRepository, nameof(calculationVersionRepository));
            Guard.ArgumentNotNull(sourceCodeService, nameof(sourceCodeService));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(buildProjectsRepository, nameof(buildProjectsRepository));
            Guard.ArgumentNotNull(calculationCodeReferenceUpdate, nameof(calculationCodeReferenceUpdate));
            Guard.ArgumentNotNull(calculationCreateModelValidator, nameof(calculationCreateModelValidator));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(calculationEditModelValidator, nameof(calculationEditModelValidator));
            Guard.ArgumentNotNull(calculationNameInUseCheck, nameof(calculationNameInUseCheck));
            Guard.ArgumentNotNull(instructionAllocationJobCreation, nameof(instructionAllocationJobCreation));
            Guard.ArgumentNotNull(createCalculationService, nameof(createCalculationService));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsRepository, nameof(resiliencePolicies.CalculationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsSearchRepository, nameof(resiliencePolicies.CalculationsSearchRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.CacheProviderPolicy, nameof(resiliencePolicies.CacheProviderPolicy));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsVersionsRepositoryPolicy, nameof(resiliencePolicies.CalculationsVersionsRepositoryPolicy));
            Guard.ArgumentNotNull(resiliencePolicies?.BuildProjectRepositoryPolicy, nameof(resiliencePolicies.BuildProjectRepositoryPolicy));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.ResultsApiClient, nameof(resiliencePolicies.ResultsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.DatasetsApiClient, nameof(resiliencePolicies.DatasetsApiClient));
            Guard.ArgumentNotNull(graphRepository, nameof(graphRepository));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(codeContextCache, nameof(codeContextCache));
            Guard.ArgumentNotNull(resultsApiClient, nameof(resultsApiClient));
            Guard.ArgumentNotNull(datasetsApiClient, nameof(datasetsApiClient));
            Guard.ArgumentNotNull(obsoleteItemCleanup, nameof(obsoleteItemCleanup));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _calculationsRepository = calculationsRepository;
            _logger = logger;
            _searchRepository = searchRepository;
            _cacheProvider = cacheProvider;
            _calculationRepositoryPolicy = resiliencePolicies.CalculationsRepository;
            _calculationVersionRepository = calculationVersionRepository;
            _calculationSearchRepositoryPolicy = resiliencePolicies.CalculationsSearchRepository;
            _cachePolicy = resiliencePolicies.CacheProviderPolicy;
            _calculationVersionsRepositoryPolicy = resiliencePolicies.CalculationsVersionsRepositoryPolicy;
            _sourceCodeService = sourceCodeService;
            _featureToggle = featureToggle;
            _buildProjectsService = buildProjectsService;
            _buildProjectsRepository = buildProjectsRepository;
            _buildProjectRepositoryPolicy = resiliencePolicies.BuildProjectRepositoryPolicy;
            _calculationCodeReferenceUpdate = calculationCodeReferenceUpdate;
            _calculationCodeReferenceUpdate = calculationCodeReferenceUpdate;
            _calculationCreateModelValidator = calculationCreateModelValidator;
            _policiesApiClient = policiesApiClient;
            _policiesApiClientPolicy = resiliencePolicies.PoliciesApiClient;
            _specificationsApiClient = specificationsApiClient;
            _calculationNameInUseCheck = calculationNameInUseCheck;
            _instructionAllocationJobCreation = instructionAllocationJobCreation;
            _createCalculationService = createCalculationService;
            _specificationsApiClientPolicy = resiliencePolicies.SpecificationsApiClient;
            _calculationEditModelValidator = calculationEditModelValidator;
            _graphRepository = graphRepository;
            _jobManagement = jobManagement;
            _codeContextCache = codeContextCache;
            _resultsApiClient = resultsApiClient;
            _approveAllCalculationsJobAction = approveAllCalculationsJobAction;
            _obsoleteItemCleanup = obsoleteItemCleanup;
            _resultsApiClientPolicy = resiliencePolicies?.ResultsApiClient;
            _datasetsApiClient = datasetsApiClient;
            _datasetsApiClientPolicy = resiliencePolicies?.DatasetsApiClient;
            _mapper = mapper;

            _typeIdentifierGenerator = new VisualBasicTypeIdentifierGenerator();
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth calcsRepoHealth = await ((IHealthChecker)_calculationsRepository).IsHealthOk();
            (bool Ok, string Message) searchRepoHealth = await _searchRepository.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(CalculationService)
            };
            health.Dependencies.AddRange(calcsRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });

            return health;
        }

        public async Task<IActionResult> GetCalculationHistory(string calculationId)
        {
            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to GetCalculationHistory");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            IEnumerable<CalculationVersion> history = await _calculationVersionsRepositoryPolicy.ExecuteAsync(() => _calculationVersionRepository.GetVersions(calculationId));

            if (history.IsNullOrEmpty())
            {
                _logger.Information($"A calculation was not found for calculation id {calculationId}");

                return new NotFoundResult();
            }

            IEnumerable<CalculationVersionResponseModel> result = history.Select(c => c.ToResponseModel());

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> GetCalculationVersions(CalculationVersionsCompareModel calculationVersionsCompareModel)
        {
            //Need custom validator here

            if (calculationVersionsCompareModel == null || string.IsNullOrEmpty(calculationVersionsCompareModel.CalculationId) || calculationVersionsCompareModel.Versions == null || calculationVersionsCompareModel.Versions.Count() < 2)
            {
                _logger.Warning("A null or invalid compare model was provided for comparing models");

                return new BadRequestObjectResult("A null or invalid compare model was provided for comparing models");
            }

            IEnumerable<CalculationVersion> allVersions = await _calculationVersionsRepositoryPolicy.ExecuteAsync(() => _calculationVersionRepository.GetVersions(calculationVersionsCompareModel.CalculationId));

            if (allVersions.IsNullOrEmpty())
            {
                _logger.Information($"No history was not found for calculation id {calculationVersionsCompareModel.CalculationId}");

                return new NotFoundResult();
            }

            List<CalculationVersionResponseModel> versions = new List<CalculationVersionResponseModel>();

            foreach (int version in calculationVersionsCompareModel.Versions)
            {
                CalculationVersion versionModel = allVersions.FirstOrDefault(m => m.Version == version);
                if (versionModel != null)
                {
                    versions.Add(versionModel.ToResponseModel());
                }
            }

            if (!versions.Any())
            {
                _logger.Information($"A calculation was not found for calculation id {calculationVersionsCompareModel.CalculationId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(versions);
        }

        public async Task<IActionResult> GetCalculationById(string calculationId)
        {
            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to GetCalculationCurrentVersion");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            string cacheKey = $"{CacheKeys.CurrentCalculation}{calculationId}";

            CalculationResponseModel calculation = await _cachePolicy.ExecuteAsync(() => _cacheProvider.GetAsync<CalculationResponseModel>(cacheKey));

            if (calculation == null)
            {
                Calculation repoCalculation = await _calculationsRepository.GetCalculationById(calculationId);
                if (repoCalculation == null)
                {
                    _logger.Information($"A calculation was not found for calculation id {calculationId}");

                    return new NotFoundResult();
                }

                if (repoCalculation.Current == null)
                {
                    _logger.Information($"A current calculation was not found for calculation id {calculationId}");

                    return new NotFoundResult();
                }

                calculation = repoCalculation.ToResponseModel();

                await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, calculation, TimeSpan.FromDays(7), true));
            }

            return new OkObjectResult(calculation);
        }

        public async Task<IActionResult> GetCurrentCalculationsForSpecification(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Warning("No specificationId was provided to GetCalculationsForSpecification");

                return new BadRequestObjectResult("Null or empty specificationId provided");
            }
            string cacheKey = $"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}";

            List<CalculationResponseModel> calculations = await _cachePolicy.ExecuteAsync(() => _cacheProvider.GetAsync<List<CalculationResponseModel>>(cacheKey));
            if (calculations == null)
            {
                IEnumerable<Calculation> calculationsFromRepository = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId));

                if (calculationsFromRepository == null)
                {
                    _logger.Warning($"Calculations from repository returned null for specification ID of '{specificationId}'");

                    return new InternalServerErrorResult("Calculations from repository returned null");
                }

                calculations = new List<CalculationResponseModel>(calculationsFromRepository.Count());
                foreach (Calculation calculation in calculationsFromRepository)
                {
                    calculations.Add(calculation.ToResponseModel());
                }

                await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, calculations, TimeSpan.FromDays(7), true));
            }

            return new OkObjectResult(calculations);
        }

        public async Task<IActionResult> GetCalculationSummariesForSpecification(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Warning("No specificationId was provided to GetCalculationSummariesForSpecification");

                return new BadRequestObjectResult("Null or empty specificationId provided");
            }
            string cacheKey = $"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}";

            List<CalculationSummaryModel> calculations = await _cachePolicy.ExecuteAsync(() => _cacheProvider.GetAsync<List<CalculationSummaryModel>>(cacheKey));
            if (calculations == null)
            {
                IEnumerable<Calculation> calculationsFromRepository = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId));

                if (calculationsFromRepository == null)
                {
                    _logger.Warning($"Calculations from repository returned null for specification ID of '{specificationId}'");

                    return new InternalServerErrorResult("Calculations from repository returned null");
                }

                calculations = new List<CalculationSummaryModel>(calculationsFromRepository.Count());
                foreach (Calculation calculation in calculationsFromRepository)
                {
                    calculations.Add(calculation.ToSummaryModel());
                }

                await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, calculations, TimeSpan.FromDays(7), true));
            }

            return new OkObjectResult(calculations);
        }

        public async Task<IActionResult> GetCalculationsMetadataForSpecification(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error($"No specificationId was provided to {nameof(GetCalculationsMetadataForSpecification)}");

                return new BadRequestObjectResult("Null or empty specificationId provided");
            }

            string cacheKey = $"{CacheKeys.CalculationsMetadataForSpecification}{specificationId}";

            IEnumerable<CalculationMetadata> calculations = await _cachePolicy.ExecuteAsync(() => _cacheProvider.GetAsync<List<CalculationMetadata>>(cacheKey));

            if (calculations == null)
            {
                calculations = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsMetatadataBySpecificationId(specificationId));

                if (!calculations.IsNullOrEmpty())
                {
                    await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetAsync<List<CalculationMetadata>>(cacheKey, calculations.ToList()));
                }
                else
                {
                    calculations = Enumerable.Empty<CalculationMetadata>();
                }
            }

            return new OkObjectResult(calculations);
        }

        public async Task<IActionResult> QueueCalculationRun(string specificationId, QueueCalculationRunModel model)
        {
            Trigger trigger = _mapper.Map<Trigger>(model.Trigger);

            ApiClientJob jobResponse = await _instructionAllocationJobCreation.SendInstructAllocationsToJobService(specificationId,
                model.Author?.Id,
                model.Author?.Name,
                trigger,
                model.CorrelationId);

            return new OkObjectResult(jobResponse);
        }

        public async Task<IActionResult> CreateAdditionalCalculation(
            string specificationId,
            CalculationCreateModel model,
            Reference author,
            string correlationId,
            bool skipCalcRun = false,
            bool skipQueueCodeContextCacheUpdate = false,
            bool overrideCreateModelAuthor = false)
        {
            ApiResponse<SpecificationSummary> specificationApiResponse = await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

            if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
            {
                return new PreconditionFailedResult("Specification not found");
            }

            SpecificationSummary specificationSummary = specificationApiResponse.Content;

            CreateCalculationResponse createCalculationResponse = await _createCalculationService.CreateCalculation(specificationId,
                model,
                CalculationNamespace.Additional,
                CalculationType.Additional,
                overrideCreateModelAuthor ? model.Author : author,
                correlationId,
                CalculationDataType.Decimal,
                initiateCalcRun: !skipCalcRun);

            if (createCalculationResponse.Succeeded)
            {
                Calculation calculation = createCalculationResponse.Calculation;

                await UpdateCalculationInCache(calculation.ToResponseModel());

                if (!skipQueueCodeContextCacheUpdate)
                {
                    await _codeContextCache.QueueCodeContextCacheUpdate(specificationId);
                }

                return new OkObjectResult(createCalculationResponse.Calculation.ToResponseModel());
            }

            if (createCalculationResponse.ErrorType == CreateCalculationErrorType.InvalidRequest)
            {
                return createCalculationResponse.ValidationResult != null
                    ? createCalculationResponse.ValidationResult.AsBadRequest()
                    : new BadRequestObjectResult(createCalculationResponse.ErrorMessage);
            }

            return new InternalServerErrorResult(createCalculationResponse.ErrorMessage);
        }

        public override async Task Process(Message message)
        {
            SpecificationVersionComparisonModel specificationVersionComparison = message.GetPayloadAsInstanceOf<SpecificationVersionComparisonModel>();

            Models.Messages.SpecificationVersion specificationVersion = specificationVersionComparison.Current;

            if (specificationVersionComparison == null || specificationVersion == null)
            {
                _logger.Error("A null specificationVersionComparison was provided to UpdateCalculationsForSpecification");

                throw new InvalidModelException(nameof(SpecificationVersionComparisonModel), new[] { "Null or invalid model provided" });
            }

            if (specificationVersionComparison.HasNoChanges && !specificationVersionComparison.HasNameChange)
            {
                _logger.Information("No changes detected");
                return;
            }

            string specificationId = specificationVersionComparison.Id;

            IEnumerable<Calculation> calculations = (await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId))).ToArraySafe();

            if (calculations.IsNullOrEmpty())
            {
                _logger.Information($"No calculations found for specification id: {specificationId}");
                return;
            }

            IList<CalculationIndex> calcIndexes = new List<CalculationIndex>();

            foreach (Calculation calculation in calculations)
            {
                string fundingStreamName = specificationVersion.FundingStreams?.FirstOrDefault(_ => _.Id == calculation.FundingStreamId)?.Name;

                calcIndexes.Add(CreateCalculationIndexItem(calculation, specificationVersion.Name, fundingStreamName));
            }

            BuildProject buildProject = await _buildProjectsService.GetBuildProjectForSpecificationId(specificationId);

            await TaskHelper.WhenAllAndThrow(
                _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.UpdateCalculations(calculations)),
                _calculationSearchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Index(calcIndexes))
            );

            IDictionary<string, string> properties = message.BuildMessageProperties();

            string userId = properties.ContainsKey("user-id") ? properties["user-id"] : string.Empty;
            string userName = properties.ContainsKey("user-name") ? properties["user-name"] : string.Empty;
            string correlationId = message.GetCorrelationId();

            Trigger trigger = new Trigger
            {
                EntityId = specificationId,
                EntityType = "Specification",
                Message = $"Updating calculations for specification: '{specificationId}'"
            };

            Job job = await SendInstructAllocationsToJobService(specificationId, userId, userName, trigger, correlationId);

            if (job == null)
            {
                string errorMessage = $"Failed to create job: '{JobConstants.DefinitionNames.CreateInstructAllocationJob} for specification id '{specificationId}'";

                _logger.Error(errorMessage);

                throw new RetriableException(errorMessage);
            }

            _logger.Information($"New job of type '{job.JobDefinitionId}' created with id: '{job.Id}'");
        }

        public async Task<IActionResult> EditCalculation(string specificationId,
            string calculationId,
            CalculationEditModel calculationEditModel,
            Reference author,
            string correlationId,
            bool setAdditional = false,
            bool skipInstruct = false,
            bool skipValidation = false,
            bool updateBuildProject = true,
            bool setTemplate = false,
            CalculationEditMode calculationEditMode = CalculationEditMode.User,
            Calculation existingCalculation = null
            )
        {
            Guard.ArgumentNotNull(calculationEditModel, nameof(calculationEditModel));
            Guard.ArgumentNotNull(author, nameof(author));

            calculationEditModel.SpecificationId = specificationId;
            calculationEditModel.CalculationId = calculationId;

            try
            {
                if (!skipValidation)
                {
                    BadRequestObjectResult validationResult = (await _calculationEditModelValidator.ValidateAsync(calculationEditModel)).PopulateModelState();

                    if (validationResult != null)
                    {
                        return validationResult;
                    }
                }

                Calculation calculation = existingCalculation ?? await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationById(calculationId));

                if (calculation == null)
                {
                    _logger.Error($"A calculation was not found for calculation id {calculationId}");

                    return new NotFoundResult();
                }

                CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

                if (setAdditional)
                {
                    calculationVersion.WasTemplateCalculation = true;
                    calculationVersion.CalculationType = CalculationType.Additional;
                }
                else if (setTemplate)
                {
                    calculationVersion.WasTemplateCalculation = false;
                    calculationVersion.CalculationType = CalculationType.Template;
                }

                calculationVersion.SourceCode = calculationEditModel.SourceCode;
                calculationVersion.ValueType = calculationEditModel.ValueType.GetValueOrDefault();

                bool isUserEditingTemplateCalculation = calculationEditMode == CalculationEditMode.User
                     && calculation.Current.CalculationType == CalculationType.Template;

                if (!isUserEditingTemplateCalculation)
                {
                    calculationVersion.Name = calculationEditModel.Name;
                    calculationVersion.SourceCodeName = _typeIdentifierGenerator.GenerateIdentifier(calculationEditModel.Name);
                    calculationVersion.Description = calculationEditModel.Description;
                }

                if (calculationEditMode == CalculationEditMode.System)
                {
                    ApplySystemAllowedEditsToCalculation(calculationEditModel, calculationVersion);
                }

                UpdateCalculationResult result = await UpdateCalculation(calculation, calculationVersion, author, updateBuildProject);

                string cacheKey = $"{CacheKeys.CalculationsMetadataForSpecification}{specificationId}";

                await _cachePolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<List<CalculationMetadata>>(cacheKey));

                if (skipInstruct)
                {
                    return new OkObjectResult(result.CurrentVersion);
                }

                Job job = await SendInstructAllocationsToJobService(result.BuildProject.SpecificationId, author.Id, author.Name, new Trigger
                {
                    EntityId = calculation.Id,
                    EntityType = nameof(Calculation),
                    Message = $"Saving calculation: '{calculationId}' for specification: '{calculation.SpecificationId}'"
                }, correlationId);

                if (job != null)
                {
                    _logger.Information($"New job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' created with id: '{job.Id}'");

                    return new OkObjectResult(result.CurrentVersion);
                }
                else
                {
                    string errorMessage = $"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{calculation.SpecificationId}'";

                    _logger.Error(errorMessage);

                    return new InternalServerErrorResult(errorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message, ex);
                return new InternalServerErrorResult(ex.Message);
            }
        }

        private static void ApplySystemAllowedEditsToCalculation(CalculationEditModel calculationEditModel, CalculationVersion calculationVersion)
        {
            calculationVersion.AllowedEnumTypeValues = calculationEditModel.AllowedEnumTypeValues;
            calculationVersion.DataType = calculationEditModel.DataType;
        }

        public async Task<IActionResult> UpdateCalculationStatus(string calculationId, EditStatusModel editStatusModel)
        {
            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to EditCalculationStatus");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            if (editStatusModel == null)
            {
                _logger.Error("A null status model was provided");
                return new BadRequestObjectResult("Null status model provided");
            }

            Calculation calculation = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationById(calculationId));

            if (calculation == null)
            {
                _logger.Warning($"A calculation was not found for calculation id {calculationId}");

                return new NotFoundObjectResult("Calculation not found");
            }

            if (calculation.Current == null)
            {
                _logger.Warning($"A current calculation was not found for calculation id {calculationId}");

                return new NotFoundResult();
            }

            if (calculation.Current.PublishStatus == editStatusModel.PublishStatus)
            {
                return new OkObjectResult(calculation.Current);
            }

            if ((calculation.Current.PublishStatus == Models.Versioning.PublishStatus.Approved || calculation.Current.PublishStatus == Models.Versioning.PublishStatus.Updated) && editStatusModel.PublishStatus == Models.Versioning.PublishStatus.Draft)
            {
                return new BadRequestObjectResult("Publish status can't be changed to Draft from Updated or Approved");
            }

            ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse =
                    await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(calculation.SpecificationId));

            if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
            {
                return new PreconditionFailedResult("Specification not found");
            }

            SpecModel.SpecificationSummary specificationSummary = specificationApiResponse.Content;

            CalculationVersion previousCalculationVersion = calculation.Current;

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

            calculationVersion.PublishStatus = editStatusModel.PublishStatus;

            HttpStatusCode statusCode = await UpdateCalculation(calculation, calculationVersion, previousCalculationVersion);

            if (!statusCode.IsSuccess())
            {
                return new StatusCodeResult((int)statusCode);
            }

            string fundingStreamName = specificationSummary.FundingStreams.FirstOrDefault(_ => _.Id == calculation.FundingStreamId)?.Name;

            await UpdateSearch(calculation, specificationSummary.Name, fundingStreamName);

            PublishStatusResultModel result = new PublishStatusResultModel()
            {
                PublishStatus = calculation.Current.PublishStatus,
            };

            CalculationResponseModel currentVersion = calculation.ToResponseModel();

            await UpdateCalculationInCache(currentVersion);

            return new OkObjectResult(result);
        }

        /// <summary>
        /// Get calculation code context
        /// </summary>
        /// <param name="specificationId">Specification Id</param>
        /// <returns></returns>
        public async Task<ActionResult<IEnumerable<TypeInformation>>> GetCalculationCodeContext(string specificationId)
        {
            try
            {
                IEnumerable<TypeInformation> result = await _codeContextCache.GetCodeContext(specificationId);

                return new OkObjectResult(result);
            }
            catch (EntityNotFoundException ex)
            {
                return new PreconditionFailedResult(ex.Message);
            }
        }

        public async Task<IActionResult> ReIndex()
        {
            IEnumerable<Calculation> calculations = await _calculationsRepository.GetAllCalculations();

            IList<CalculationIndex> calcIndexItems = new List<CalculationIndex>();

            Dictionary<string, SpecModel.SpecificationSummary> specifications = new Dictionary<string, SpecModel.SpecificationSummary>();

            foreach (Calculation calculation in calculations)
            {
                SpecModel.SpecificationSummary specification = null;
                if (specifications.ContainsKey(calculation.SpecificationId))
                {
                    specification = specifications[calculation.SpecificationId];
                }
                else
                {
                    ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse =
                    await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(calculation.SpecificationId));

                    if (specificationApiResponse.StatusCode.IsSuccess() && specificationApiResponse.Content != null)
                    {
                        specification = specificationApiResponse.Content;
                        specifications.Add(calculation.SpecificationId, specification);
                    }
                }

                //bad data has crept into Test so we've added this temp guard to make the reindex more
                //resilient to breaks in referential integrity between calcs and specs
                if (specification == null)
                {
                    _logger.Warning($"Did not locate the specification for calculation {calculation.Id} " +
                                    $"with id {calculation.SpecificationId}. Skipping indexing this calculation");

                    continue;
                }

                string fundingStreamName = specification.FundingStreams.FirstOrDefault(_ => _.Id == calculation.FundingStreamId)?.Name;

                CalculationIndex indexItem = CreateCalculationIndexItem(calculation, specification.Name, fundingStreamName);

                calcIndexItems.Add(indexItem);
            }

            IEnumerable<IndexError> indexingResults = await _calculationSearchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Index(calcIndexItems));

            if (indexingResults.Any())
            {
                string errorMessage = $"Failed to re-index calculation with the following errors: {string.Join(";", indexingResults.Select(m => m.ErrorMessage).ToArraySafe())}";
                _logger.Error(errorMessage);

                return new InternalServerErrorResult(errorMessage);
            }

            return new NoContentResult();
        }

        public async Task<IActionResult> GetCalculationStatusCounts(SpecificationListModel specifications)
        {
            if (specifications == null)
            {
                _logger.Error("Null specification model provided");

                return new BadRequestObjectResult("Null specifications model provided");
            }

            if (specifications.SpecificationIds.IsNullOrEmpty())
            {
                _logger.Error("Null or empty specification ids provided");

                return new BadRequestObjectResult("Null or empty specification ids provided");
            }

            ConcurrentBag<CalculationStatusCountsModel> statusCountModels = new ConcurrentBag<CalculationStatusCountsModel>();

            IList<Task> statusCountsTasks = new List<Task>();

            foreach (string specificationId in specifications.SpecificationIds)
            {
                statusCountsTasks.Add(Task.Run(async () =>
                {
                    StatusCounts statusCounts = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetStatusCounts(specificationId));

                    statusCountModels.Add(new CalculationStatusCountsModel
                    {
                        SpecificationId = specificationId,
                        Approved = statusCounts.Approved,
                        Updated = statusCounts.Updated,
                        Draft = statusCounts.Draft
                    });

                }));
            }

            try
            {
                await TaskHelper.WhenAllAndThrow(statusCountsTasks.ToArray());
            }
            catch (Exception ex)
            {
                return new InternalServerErrorResult($"An error occurred when obtaining calculation steps with the follwing message: \n {ex.Message}");
            }

            return new OkObjectResult(statusCountModels);
        }

        public async Task<IActionResult> GetCalculationByName(CalculationGetModel model)
        {
            Guard.ArgumentNotNull(model, nameof(model));

            if (string.IsNullOrWhiteSpace(model.SpecificationId))
            {
                _logger.Error("No specification id was provided to GetCalculationByName");
                return new BadRequestObjectResult("Null or empty specification id provided");
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                _logger.Error("No calculation name was provided to GetCalculationByName");
                return new BadRequestObjectResult("Null or empty calculation name provided");
            }

            Calculation calculation = await _calculationsRepository.GetCalculationBySpecificationIdAndCalculationName(model.SpecificationId, model.Name);

            if (calculation == null)
            {
                _logger.Information($"A calculation was not found for specification id {model.SpecificationId} and name {model.Name}");

                return new NotFoundResult();
            }

            return new OkObjectResult(calculation.ToResponseModel());
        }

        public async Task<IActionResult> IsCalculationNameValid(string specificationId, string calculationName, string existingCalculationId)
        {
            bool? isNameInUseCheckResult = await _calculationNameInUseCheck.IsCalculationNameInUse(specificationId, calculationName, existingCalculationId);

            if (isNameInUseCheckResult == null)
            {
                return new NotFoundResult();
            }

            return isNameInUseCheckResult == true ? (IActionResult)new ConflictResult() : new OkResult();
        }

        public async Task ResetCalculationForFieldDefinitionChanges(IEnumerable<DatasetSpecificationRelationshipViewModel> relationships, string specificationId, IEnumerable<string> currentFieldDefinitionNames)
        {
            Guard.ArgumentNotNull(relationships, nameof(relationships));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.ArgumentNotNull(currentFieldDefinitionNames, nameof(currentFieldDefinitionNames));

            IEnumerable<Calculation> calculations = (await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId))).ToList();

            if (calculations.IsNullOrEmpty())
            {
                _logger.Information($"No calculations found to reset for specification id '{specificationId}'");
                return;
            }

            List<string> fieldIdentifiers = new List<string>();

            foreach (DatasetSpecificationRelationshipViewModel datasetSpecificationRelationshipViewModel in relationships)
            {
                fieldIdentifiers.AddRange(
                    currentFieldDefinitionNames.Select(m =>
                        $"Datasets.{_typeIdentifierGenerator.GenerateIdentifier(datasetSpecificationRelationshipViewModel.Name)}.{_typeIdentifierGenerator.GenerateIdentifier(m)}"));
            }

            await _cacheProvider.RemoveAsync<List<DatasetSchemaRelationshipModel>>($"{CacheKeys.DatasetRelationshipFieldsForSpecification}{specificationId}");

            IEnumerable<Calculation> calcsToUpdate = calculations.Where(m => SourceCodeHelpers.CodeContainsFullyQualifiedDatasetFieldIdentifier(m.Current?.SourceCode, fieldIdentifiers));

            if (calcsToUpdate.IsNullOrEmpty())
            {
                _logger.Information($"No calculations required resetting for specification id '{specificationId}'");
                return;
            }

            foreach (Calculation calculation in calcsToUpdate)
            {
                string sourceCode = calculation.Current.SourceCode;

                CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

                calculationVersion.SourceCode = SourceCodeHelpers.CommentOutCode(sourceCode, reasonForCommenting, exceptionMessage, exceptionType);
                calculationVersion.Comment = reasonForCommenting;
                await UpdateCalculation(calculation, calculationVersion, new Reference("System", "System"));
            }

            await _sourceCodeService.DeleteAssembly(specificationId);
        }

        public async Task<IActionResult> ProcessTemplateMappings(string specificationId, string templateVersion, string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(templateVersion, nameof(templateVersion));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse =
                    await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

            if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
            {
                string message = $"No specification ID {specificationId} were returned from the repository, result came back null";
                _logger.Error(message);

                return new PreconditionFailedResult(message);
            }

            SpecModel.SpecificationSummary specificationSummary = specificationApiResponse.Content;

            bool? specificationContainsGivenFundingStream = specificationSummary.FundingStreams?.Any(x => x.Id == fundingStreamId);
            if (!specificationContainsGivenFundingStream.GetValueOrDefault())
            {
                string message = $"Specification ID {specificationId} does not have contain given funding stream with ID {fundingStreamId}";
                _logger.Error(message);

                return new PreconditionFailedResult(message);
            }

            ApiResponse<TemplateMetadataContents> fundingTemplateContentsResponse =
                await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingTemplateContents(fundingStreamId,
                specificationSummary.FundingPeriod.Id, templateVersion));

            if (fundingTemplateContentsResponse?.StatusCode != HttpStatusCode.OK)
            {
                string message = $"Retrieve funding template with fundingStreamId: {fundingStreamId}, fundingPeriodId: {specificationSummary.FundingPeriod.Id} and templateId: {templateVersion} did not return OK.";
                _logger.Error(message);

                return new PreconditionFailedResult(message);
            }

            if (fundingTemplateContentsResponse.Content == null)
            {
                string message = $"Retrieved funding template with fundingStreamId: {fundingStreamId} and templateId: {templateVersion} has null content. Can not use it for further processing.";
                _logger.Error(message);

                return new PreconditionFailedResult(message);
            }

            TemplateMetadataContents templateMetadataContents = fundingTemplateContentsResponse.Content;

            string existingSaveVersionOfTemplateMapping = null;

            TemplateMapping templateMapping = await _calculationsRepository.GetTemplateMapping(specificationId, fundingStreamId);
            if (templateMapping == null)
            {
                templateMapping = new TemplateMapping()
                {
                    FundingStreamId = fundingStreamId,
                    SpecificationId = specificationId,
                    TemplateMappingItems = new List<TemplateMappingItem>()
                };
            }
            else
            {
                existingSaveVersionOfTemplateMapping = JsonConvert.SerializeObject(templateMapping);
            }

            await ProcessTemplateMappingChanges(templateMapping, templateMetadataContents);

            // Only save if changed
            if (existingSaveVersionOfTemplateMapping != JsonConvert.SerializeObject(templateMapping))
            {
                await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.UpdateTemplateMapping(specificationId, fundingStreamId, templateMapping));

                string cacheKey = $"{CacheKeys.TemplateMapping}{specificationId}-{fundingStreamId}";
                await _cachePolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<TemplateMapping>(cacheKey));
                await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.UpdateFundingStructureLastModified(
                    new Common.ApiClient.Specifications.Models.UpdateFundingStructureLastModifiedRequest
                    {
                        LastModified = DateTimeOffset.UtcNow,
                        FundingPeriodId = specificationSummary.FundingPeriod?.Id,
                        FundingStreamId = fundingStreamId,
                        SpecificationId = specificationId
                    }));
                await _cachePolicy.ExecuteAsync(() => _cacheProvider.RemoveByPatternAsync($"{CacheKeys.CalculationFundingLines}{specificationId}"));
            }

            return new OkObjectResult(templateMapping.ToSummaryResponseModel());
        }

        public async Task<IActionResult> CheckHasAllApprovedTemplateCalculationsForSpecificationId(string specificationId)
        {
            Guard.ArgumentNotNull(specificationId, nameof(specificationId));

            int countNonApproved = await _calculationSearchRepositoryPolicy.ExecuteAsync(() => _calculationsRepository
                .GetCountOfNonApprovedTemplateCalculations(specificationId));

            Models.BooleanResponseModel booleanResponseModel = new Models.BooleanResponseModel
            {
                Value = countNonApproved == 0
            };

            return new OkObjectResult(booleanResponseModel);
        }

        public async Task<IActionResult> GetMappedCalculationsOfSpecificationTemplate(string specificationId, string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId));

            string cacheKey = $"{CacheKeys.TemplateMapping}{specificationId}-{fundingStreamId}";
            TemplateMapping templateMapping = await _cachePolicy.ExecuteAsync(() => _cacheProvider.GetAsync<TemplateMapping>(cacheKey));

            if (templateMapping == null || templateMapping.TemplateMappingItems.Any(tm => tm.CalculationId == null))
            {
                templateMapping = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetTemplateMapping(specificationId, fundingStreamId));
                if (templateMapping?.TemplateMappingItems == null)
                {
                    string message = $"A template mapping was not found for specification id {specificationId} and funding stream Id {fundingStreamId}";
                    _logger.Information(message);

                    return new NotFoundObjectResult(message);
                }

                await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, templateMapping, TimeSpan.FromDays(7), true));
            }

            TemplateMappingSummary result = templateMapping.ToSummaryResponseModel();

            return new OkObjectResult(result);
        }

        public async Task DeleteCalculations(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string jobId = message.GetUserProperty<string>("jobId");

            Guard.IsNullOrWhiteSpace(jobId, nameof(jobId));

            try
            {
                // Update job to set status to processing
                await _jobManagement.UpdateJobStatus(jobId, 0, 0, null, null);

                string specificationId = message.UserProperties["specification-id"].ToString();
                if (string.IsNullOrEmpty(specificationId))
                {
                    string error = "Null or empty specification Id provided for deleting calculations";
                    _logger.Error(error);
                    throw new Exception(error);
                }

                string deletionTypeProperty = message.UserProperties["deletion-type"].ToString();
                if (string.IsNullOrEmpty(deletionTypeProperty))
                {
                    string error = "Null or empty deletion type provided for deleting calculations";
                    _logger.Error(error);
                    throw new Exception(error);
                }

                await _calculationsRepository.DeleteCalculationsBySpecificationId(specificationId, deletionTypeProperty.ToDeletionType());

                await _calculationsRepository.DeleteTemplateMappingsBySpecificationId(specificationId, deletionTypeProperty.ToDeletionType());

                await _jobManagement.UpdateJobStatus(jobId, 0, 0, true, null);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unable to complete delete calculations job");

                await TrackJobFailed(jobId, exception);

                throw new NonRetriableException("Unable to delete delete calculations.", exception);
            }
        }

        public async Task<IActionResult> UpdateTemplateCalculationsForSpecification(string specificationId, string datasetRelationshipId, Reference user)
        {
            IEnumerable<Calculation> templateCalculations = (await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetTemplateCalculationsBySpecificationId(specificationId))).ToList();

            if (templateCalculations.IsNullOrEmpty())
            {
                string message = $"No template calculations found for specification id '{specificationId}'";
                _logger.Information(message);
                return new NotFoundObjectResult(message);
            }

            ApiResponse<ApiClientSelectDatasourceModel> datasetRelationshipResponse =
                await _datasetsApiClientPolicy.ExecuteAsync(() => _datasetsApiClient.GetDataSourcesByRelationshipId(datasetRelationshipId, top: null, pageNumber: null));
            if (!datasetRelationshipResponse.StatusCode.IsSuccess() || datasetRelationshipResponse.Content == null)
            {
                string message = $"No dataset relationship found for dataset relationship id '{datasetRelationshipId}'";
                _logger.Information(message);
                return new NotFoundObjectResult(message);
            }
            ApiClientSelectDatasourceModel relationshipDatasourceModel = datasetRelationshipResponse.Content;

            ApiResponse<ApiClientDatasetDefinition> datasetDefinitionResponse = await _datasetsApiClientPolicy.ExecuteAsync(() => _datasetsApiClient.GetDatasetDefinitionById(relationshipDatasourceModel.DefinitionId));
            if (!datasetDefinitionResponse.StatusCode.IsSuccess() || datasetDefinitionResponse.Content == null)
            {
                string message = $"No dataset definition found for dataset definition id '{relationshipDatasourceModel.DefinitionId}'";
                _logger.Information(message);
                return new NotFoundObjectResult(message);
            }

            ApiResponse<SpecModel.SpecificationSummary> specificationResponse = await _specificationsApiClientPolicy.ExecuteAsync(() =>
                _specificationsApiClient.GetSpecificationSummaryById(specificationId));
            SpecModel.SpecificationSummary specificationSummary = specificationResponse?.Content;

            if (!specificationResponse.StatusCode.IsSuccess() || specificationSummary == null)
            {
                string message = $"No specification with id {specificationId}. Unable to get Specification Summary for calculation";
                _logger.Information(message);
                return new NotFoundObjectResult(message);
            }

            ApiClientDatasetDefinition datasetDefinition = datasetDefinitionResponse.Content;
            ApiClientTableDefinition tableDefinition = datasetDefinition.TableDefinitions.First();

            string datasetRelationshipVisualBasicVariableName = _typeIdentifierGenerator.GenerateIdentifier(relationshipDatasourceModel.RelationshipName);

            foreach (Calculation templateCalculation in templateCalculations)
            {
                if (templateCalculation.Current.DataType != CalculationDataType.Enum)
                {
                    ApiClientFieldDefinition fieldDefinition = tableDefinition.FieldDefinitions.FirstOrDefault(x => x.Name == templateCalculation.Name);

                    if (fieldDefinition != null)
                    {
                        CalculationVersion calculationVersion = templateCalculation.Current.Clone() as CalculationVersion;

                        string fieldDefinitionVisualBasicName = _typeIdentifierGenerator.GenerateIdentifier(fieldDefinition.Name);

                        calculationVersion.SourceCode = @$"If Datasets.{datasetRelationshipVisualBasicVariableName}.HasValue = False Then Return Nothing

Return Datasets.{datasetRelationshipVisualBasicVariableName}.{fieldDefinitionVisualBasicName}";

                        calculationVersion.PublishStatus = Models.Versioning.PublishStatus.Approved;

                        await UpdateCalculation(templateCalculation, calculationVersion, user, updateBuildProject: false);
                    }
                }
                else if (templateCalculation.Current.DataType == CalculationDataType.Enum)
                {
                    CalculationVersion calculationVersion = templateCalculation.Current.Clone() as CalculationVersion;

                    string calculationNameVisualBasicVariable = _typeIdentifierGenerator.GenerateIdentifier(templateCalculation.Current.Name);

                    StringBuilder stringBuilder = new StringBuilder();

                    stringBuilder.Append(@$"If Datasets.{datasetRelationshipVisualBasicVariableName}.HasValue = False Then Return Nothing
Dim stringValue As String = Nothing

If (String.IsNullOrWhiteSpace(Datasets.{datasetRelationshipVisualBasicVariableName}.{calculationNameVisualBasicVariable}) <> False) Then
    stringValue = Datasets.{datasetRelationshipVisualBasicVariableName}.{calculationNameVisualBasicVariable}.ToLowerInvariant()
End If

");

                    stringBuilder.Append(@$"Select Case stringValue
    Case Nothing
        Return Nothing
    Case """"
        Return Nothing
");
                    foreach (string value in templateCalculation.Current.AllowedEnumTypeValues)
                    {
                        stringBuilder.Append(@$"    Case ""{value}""
        Return {_typeIdentifierGenerator.GenerateIdentifier(templateCalculation.Current.Name)}Options.{_typeIdentifierGenerator.GenerateIdentifier(value)}
");
                    }

                    stringBuilder.Append($@"    Case Else
        Throw New InvalidOperationException(""Unable to find option "" + Datasets.{datasetRelationshipVisualBasicVariableName}.{calculationNameVisualBasicVariable})
End Select");

                    calculationVersion.SourceCode = stringBuilder.ToString();

                    calculationVersion.PublishStatus = Models.Versioning.PublishStatus.Approved;

                    await UpdateCalculation(templateCalculation, calculationVersion, user, updateBuildProject: false);
                }
            }

            await UpdateBuildProject(specificationSummary);

            string cacheKey = $"{CacheKeys.CalculationsMetadataForSpecification}{specificationId}";
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<List<CalculationMetadata>>(cacheKey));

            return new OkResult();
        }

        public async Task<IActionResult> QueueApproveAllSpecificationCalculations(
            string specificationId, Reference author, string correlationId)
        {
            try
            {
                Job approveCalculationsJob = await _approveAllCalculationsJobAction.Run(specificationId, author, correlationId);

                return new OkObjectResult(approveCalculationsJob);
            }
            catch (Exception ex)
            {
                string errorMessage = "Unable to create approve all calculations job";

                _logger.Error(ex, errorMessage);

                return new InternalServerErrorResult(errorMessage);
            }
        }

        private async Task TrackJobFailed(string jobId, Exception exception)
        {
            await AddJobTracking(jobId, new JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = exception.ToString()
            });
        }
        private async Task AddJobTracking(string jobId, JobLogUpdateModel tracking)
        {
            await _jobManagement.AddJobLog(jobId, tracking);
        }

        private async Task UpdateSearch(Calculation calculation, string specificationName, string fundingStreamName)
        {
            await _searchRepository.Index(new List<CalculationIndex>
            {
                CreateCalculationIndexItem(calculation, specificationName, fundingStreamName)
            });
        }

        private CalculationIndex CreateCalculationIndexItem(Calculation calculation,
            string specificationName,
            string fundingStreamName)
        {
            return new CalculationIndex
            {
                Id = calculation.Id,
                SpecificationId = calculation.SpecificationId,
                SpecificationName = specificationName,
                Name = calculation.Current.Name,
                ValueType = calculation.Current.ValueType.ToString(),
                FundingStreamId = calculation.FundingStreamId ?? "N/A",
                FundingStreamName = fundingStreamName ?? "N/A",
                Namespace = calculation.Current.Namespace.ToString(),
                CalculationType = calculation.Current.CalculationType.ToString(),
                Description = calculation.Current.Description,
                WasTemplateCalculation = calculation.Current.WasTemplateCalculation,
                Status = calculation.Current.PublishStatus.ToString(),
                LastUpdatedDate = DateTimeOffset.Now
            };
        }

        public async Task<IEnumerable<Calculation>> UpdateCalculationCodeOnCalculationOrFundinglineChange(string oldSourceCodeName, string newSourceCodeName, string specificationId, string @namespace, Reference user, bool isEnum)
        {
            List<Calculation> updatedCalculations = new List<Calculation>();

            string oldSourceCodeNameEscaped = _typeIdentifierGenerator.GenerateIdentifier(oldSourceCodeName);
            string newSourceCodeNameEscaped = _typeIdentifierGenerator.GenerateIdentifier(newSourceCodeName);
            string namespaceEscaped = _typeIdentifierGenerator.GenerateIdentifier(@namespace);

            if (oldSourceCodeNameEscaped != newSourceCodeNameEscaped)
            {
                IEnumerable<Calculation> calculations = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId));

                foreach (Calculation calculation in calculations)
                {
                    string sourceCode = calculation.Current.SourceCode;

                    string result = _calculationCodeReferenceUpdate.ReplaceSourceCodeReferences(sourceCode,
                        oldSourceCodeNameEscaped,
                        newSourceCodeNameEscaped,
                        namespaceEscaped);

                    if (isEnum)
                    {
                        result = _calculationCodeReferenceUpdate.ReplaceSourceCodeReferences(result,
                            $"{oldSourceCodeNameEscaped}Options",
                            $"{newSourceCodeNameEscaped}Options",
                            null);
                    }

                    if (result != sourceCode)
                    {
                        CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;
                        calculationVersion.SourceCode = result;

                        UpdateCalculationResult updateCalculationResult = await UpdateCalculation(calculation, calculationVersion, user, updateBuildProject: false);

                        updatedCalculations.Add(updateCalculationResult.Calculation);
                    }
                }
            }

            return updatedCalculations;
        }

        //TODO: plumb in the obsolete item stuff here
        private async Task<UpdateCalculationResult> UpdateCalculation(Calculation calculation, CalculationVersion calculationVersion, Reference user, bool updateBuildProject = true)
        {
            Guard.ArgumentNotNull(calculation, nameof(calculation));
            Guard.ArgumentNotNull(calculationVersion, nameof(calculationVersion));
            Guard.ArgumentNotNull(user, nameof(user));

            if (calculation.Current == null)
            {
                _logger.Warning($"Current for {calculation.Id} was null and needed recreating.");
                calculation.Current = calculationVersion;
            }

            CalculationVersion previousCalculationVersion = calculation.Current;

            calculationVersion.Author = user;

            HttpStatusCode statusCode = await UpdateCalculation(calculation, calculationVersion, previousCalculationVersion);

            if (statusCode != HttpStatusCode.OK)
            {
                throw new InvalidOperationException($"Update calculation returned status code '{statusCode}' instead of OK");
            }

            await _obsoleteItemCleanup.ProcessCalculation(calculation);

            BuildProject buildProject = null;

            ApiResponse<SpecModel.SpecificationSummary> specificationResponse = await _specificationsApiClientPolicy.ExecuteAsync(() =>
                _specificationsApiClient.GetSpecificationSummaryById(calculation.SpecificationId));

            SpecModel.SpecificationSummary specificationSummary = specificationResponse?.Content;

            if (!specificationResponse.StatusCode.IsSuccess() || specificationSummary == null)
            {
                string errorMsg = $"No specification with id {calculation.SpecificationId}. Unable to get Specification Summary for calculation";
                _logger.Error(errorMsg);
                throw new Exception(errorMsg);
            }

            if (updateBuildProject)
            {
                buildProject = await UpdateBuildProject(specificationSummary);
            }

            string fundingStreamName = specificationSummary.FundingStreams.FirstOrDefault(_ => _.Id == calculation.FundingStreamId)?.Name;

            await UpdateSearch(calculation, specificationSummary.Name, fundingStreamName);

            CalculationResponseModel currentVersion = calculation.ToResponseModel();

            await UpdateCalculationInCache(currentVersion);

            return new UpdateCalculationResult()
            {
                BuildProject = buildProject,
                Calculation = calculation,
                CurrentVersion = currentVersion,
            };
        }

        public async Task<BuildProject> UpdateBuildProject(SpecModel.SpecificationSummary specificationSummary)
        {
            Task<IEnumerable<Calculation>> calculationsRequest = _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationSummary.Id));
            Task<BuildProject> buildProjectRequest = _buildProjectsService.GetBuildProjectForSpecificationId(specificationSummary.Id);

            await TaskHelper.WhenAllAndThrow(calculationsRequest, buildProjectRequest);

            List<Calculation> calculations = new List<Calculation>(calculationsRequest.Result);
            BuildProject buildProject = buildProjectRequest.Result;

            return await UpdateBuildProject(specificationSummary, calculations, buildProject);
        }


        private async Task<BuildProject> UpdateBuildProject(SpecModel.SpecificationSummary specificationSummary, IEnumerable<Calculation> calculations, BuildProject buildProject = null)
        {
            buildProject ??= await _buildProjectsService.GetBuildProjectForSpecificationId(specificationSummary.Id);

            CompilerOptions compilerOptions = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCompilerOptions(specificationSummary.Id));

            if (compilerOptions == null)
            {
                compilerOptions = new CompilerOptions();
            }

            //forcing off for calc runs only
            compilerOptions.OptionStrictEnabled = false;

            IEnumerable<ObsoleteItem> obsoleteItems = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetObsoleteItemsForSpecification(buildProject.SpecificationId));

            buildProject.Build = _sourceCodeService.Compile(buildProject, calculations, obsoleteItems, compilerOptions);

            if (!buildProject.Build.Success)
            {
                string compilerMessages = string.Join(Environment.NewLine, buildProject.Build.CompilerMessages?.Select(_ => _.Message) ?? ArraySegment<string>.Empty);

                throw new NonRetriableException(
                    $"Compilation failed during build for Specification {specificationSummary.Name}.{Environment.NewLine}{compilerMessages}");
            }

            await _sourceCodeService.SaveSourceFiles(buildProject.Build.SourceFiles, specificationSummary.Id, SourceCodeType.Release);

            await _sourceCodeService.SaveAssembly(buildProject);

            if (!_featureToggle.IsDynamicBuildProjectEnabled())
            {
                await _buildProjectRepositoryPolicy.ExecuteAsync(() => _buildProjectsRepository.UpdateBuildProject(buildProject));
            }

            return buildProject;
        }

        private async Task<Job> SendInstructAllocationsToJobService(string specificationId, string userId, string userName, Trigger trigger, string correlationId)
        {
            return await _instructionAllocationJobCreation.SendInstructAllocationsToJobService(specificationId, userId, userName, trigger, correlationId);
        }

        private async Task UpdateCalculationInCache(CalculationResponseModel currentVersion)
        {
            // Invalidate cached calculations for this specification
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.KeyDeleteAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{currentVersion.SpecificationId}"));
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.KeyDeleteAsync<List<CalculationResponseModel>>($"{CacheKeys.CurrentCalculationsForSpecification}{currentVersion.SpecificationId}"));
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.KeyDeleteAsync<List<CalculationResponseModel>>($"{CacheKeys.CalculationsMetadataForSpecification}{currentVersion.SpecificationId}"));

            // Set current version in cache
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetAsync($"{CacheKeys.CurrentCalculation}{currentVersion.Id}", currentVersion, TimeSpan.FromDays(7), true));
        }

        private async Task<HttpStatusCode> UpdateCalculation(Calculation calculation, CalculationVersion calculationVersion, CalculationVersion previousVersion)
        {
            calculationVersion = await _calculationVersionsRepositoryPolicy.ExecuteAsync(() => _calculationVersionRepository.CreateVersion(calculationVersion, previousVersion));

            calculation.Current = calculationVersion;

            HttpStatusCode result = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.UpdateCalculation(calculation));
            if (result == HttpStatusCode.OK)
            {
                await _calculationVersionsRepositoryPolicy.ExecuteAsync(() => _calculationVersionRepository.SaveVersion(calculationVersion));
            }

            return result;
        }

        private async Task<bool> ProcessTemplateMappingChanges(TemplateMapping templateMapping, TemplateMetadataContents fundingTemplateContents)
        {
            bool madeChanges = false;

            List<Common.TemplateMetadata.Models.FundingLine> allFundingLines = fundingTemplateContents
                .RootFundingLines
                .Flatten(_ => _.FundingLines).ToList();

            List<Common.TemplateMetadata.Models.Calculation> templateCalculations = allFundingLines
                .SelectMany(c =>
                {
                    return c.Calculations.Flatten(_ => _.Calculations);
                }).ToList();

            IEnumerable<ReferenceData> templateReferenceData = templateCalculations
                .SelectMany(c =>
                {
                    if (c.ReferenceData.IsNullOrEmpty())
                    {
                        return Enumerable.Empty<ReferenceData>();
                    }

                    return c.ReferenceData;
                });

            List<TemplateMappingItem> itemsToRemove = new List<TemplateMappingItem>();

            foreach (TemplateMappingItem mapping in templateMapping.TemplateMappingItems)
            {
                if (mapping.EntityType == TemplateMappingEntityType.Calculation)
                {
                    bool stillExists = templateCalculations.Any(c => c.TemplateCalculationId == mapping.TemplateId);
                    if (!stillExists)
                    {
                        if (mapping.CalculationId != null)
                        {
                            Calculation existingCalculation = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationById(mapping.CalculationId));

                            if (existingCalculation != null)
                            {
                                CalculationEditModel calculationEditModel = new CalculationEditModel
                                {
                                    Description = existingCalculation.Current.Description,
                                    SourceCode = existingCalculation.Current.SourceCode,
                                    Name = existingCalculation.Current.Name,
                                    ValueType = existingCalculation.Current.ValueType,
                                };

                                IActionResult editCalculationResult = await EditCalculation(existingCalculation.SpecificationId,
                                                    mapping.CalculationId,
                                                    calculationEditModel,
                                                    existingCalculation.Current.Author,
                                                    Guid.NewGuid().ToString(),
                                                    setAdditional: true,
                                                    skipInstruct: true,
                                                    skipValidation: true,
                                                    updateBuildProject: false,
                                                    setTemplate: false,
                                                    calculationEditMode: CalculationEditMode.System,
                                                    existingCalculation: existingCalculation);

                                if (!(editCalculationResult is OkObjectResult))
                                {
                                    string error = "Unable to edit template calculation for template mapping";
                                    _logger.Error(error);
                                    throw new Exception(error);
                                }
                            }
                        }

                        itemsToRemove.Add(mapping);
                    }
                }
                if (mapping.EntityType == TemplateMappingEntityType.ReferenceData)
                {
                    bool stillExists = templateReferenceData.Any(c => c.TemplateReferenceId == mapping.TemplateId);
                    if (!stillExists)
                    {
                        itemsToRemove.Add(mapping);
                    }
                }
            }

            foreach (TemplateMappingItem item in itemsToRemove)
            {
                templateMapping.TemplateMappingItems.Remove(item);
            }

            foreach (Common.TemplateMetadata.Models.Calculation calculation in templateCalculations)
            {
                TemplateMappingItem existingItem = templateMapping.TemplateMappingItems
                    .FirstOrDefault(s => s.EntityType == TemplateMappingEntityType.Calculation && s.TemplateId == calculation.TemplateCalculationId);

                if (existingItem == null)
                {
                    TemplateMappingItem mappingCalculation = new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.Calculation,
                        Name = calculation.Name,
                        TemplateId = calculation.TemplateCalculationId,
                    };

                    templateMapping.TemplateMappingItems.Add(mappingCalculation);
                    madeChanges = true;
                }
                else
                {
                    if (existingItem.Name != calculation.Name)
                    {
                        existingItem.Name = calculation.Name;
                        madeChanges = true;
                    }
                }
            }

            foreach (ReferenceData referenceData in templateReferenceData)
            {
                TemplateMappingItem existingItem = templateMapping.TemplateMappingItems
                    .FirstOrDefault(s => s.EntityType == TemplateMappingEntityType.ReferenceData && s.TemplateId == referenceData.TemplateReferenceId);

                if (existingItem == null)
                {
                    TemplateMappingItem mappingCalculation = new TemplateMappingItem()
                    {
                        CalculationId = null,
                        EntityType = TemplateMappingEntityType.ReferenceData,
                        Name = referenceData.Name,
                        TemplateId = referenceData.TemplateReferenceId,
                    };

                    templateMapping.TemplateMappingItems.Add(mappingCalculation);
                    madeChanges = true;
                }
                else
                {
                    if (existingItem.Name != referenceData.Name)
                    {
                        existingItem.Name = referenceData.Name;
                        madeChanges = true;
                    }
                }
            }

            return madeChanges;
        }
    }
}
