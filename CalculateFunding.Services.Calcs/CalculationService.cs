using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Code;
using CalculateFunding.Models.Datasets;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Messages;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.ResultModels;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Serilog;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using CalculationType = CalculateFunding.Models.Calcs.CalculationType;
using CalculationResponseModel = CalculateFunding.Models.Calcs.CalculationResponseModel;
using Job = CalculateFunding.Common.ApiClient.Jobs.Models.Job;
using SpecModel = CalculateFunding.Common.ApiClient.Specifications.Models;
using Trigger = CalculateFunding.Common.ApiClient.Jobs.Models.Trigger;
using CalculateFunding.Models.Graph;


namespace CalculateFunding.Services.Calcs
{
    public class CalculationService : ICalculationService, IHealthChecker
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
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly Polly.AsyncPolicy _specificationsApiClientPolicy;
        private readonly IValidator<CalculationEditModel> _calculationEditModelValidator;
        private readonly ICalculationNameInUseCheck _calculationNameInUseCheck;
        private readonly IInstructionAllocationJobCreation _instructionAllocationJobCreation;
        private readonly ICreateCalculationService _createCalculationService;
        private readonly IGraphRepository _graphRepository;
        

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
            IGraphRepository graphRepository)
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
            Guard.ArgumentNotNull(graphRepository, nameof(graphRepository));


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

        public async Task<IActionResult> CreateAdditionalCalculation(string specificationId, CalculationCreateModel model, Reference author, string correlationId)
        {
            ApiResponse<SpecModel.SpecificationSummary> specificationApiResponse = await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

            if (!specificationApiResponse.StatusCode.IsSuccess() || specificationApiResponse.Content == null)
            {
                return new PreconditionFailedResult("Specification not found");
            }

            CreateCalculationResponse createCalculationResponse = await _createCalculationService.CreateCalculation(specificationId,
                model,
                CalculationNamespace.Additional,
                CalculationType.Additional,
                author,
                correlationId);

            if (createCalculationResponse.Succeeded)
            {
                IEnumerable<Calculation> calculations = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId));

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

        public async Task UpdateCalculationsForSpecification(Message message)
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

        public async Task<IEnumerable<Calculation>> UpdateCalculationCodeOnCalculationChange(CalculationVersionComparisonModel comparison, Reference user)
        {
            string oldCalcSourceCodeName = VisualBasicTypeGenerator.GenerateIdentifier(comparison.Previous.Name);
            string newCalcSourceCodeName = VisualBasicTypeGenerator.GenerateIdentifier(comparison.Current.Name);

            return await UpdateCalculationCodeOnCalculationChange(oldCalcSourceCodeName, newCalcSourceCodeName, comparison.SpecificationId, user);
        }

        public async Task<IActionResult> EditCalculation(string specificationId, string calculationId, CalculationEditModel calculationEditModel, Reference author, string correlationId, bool setAdditional = false, bool skipInstruct = false)
        {
            Guard.ArgumentNotNull(calculationEditModel, nameof(calculationEditModel));
            Guard.ArgumentNotNull(author, nameof(author));

            calculationEditModel.SpecificationId = specificationId;
            calculationEditModel.CalculationId = calculationId;

            try
            {
                BadRequestObjectResult validationResult = (await _calculationEditModelValidator.ValidateAsync(calculationEditModel)).PopulateModelState();

                if (validationResult != null)
                {
                    return validationResult;
                }

                Calculation calculation = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationById(calculationId));

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

                calculationVersion.SourceCode = calculationEditModel.SourceCode;
                calculationVersion.Name = calculationEditModel.Name;
                calculationVersion.ValueType = calculationEditModel.ValueType.Value;
                calculationVersion.SourceCodeName = VisualBasicTypeGenerator.GenerateIdentifier(calculationEditModel.Name);
                calculationVersion.Description = calculationEditModel.Description;

                UpdateCalculationResult result = await UpdateCalculation(calculation, calculationVersion, author);

                string cacheKey = $"{CacheKeys.CalculationsMetadataForSpecification}{specificationId}";

                await _cachePolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<List<CalculationMetadata>>(cacheKey));

                Job job = null;

                if(skipInstruct)
                {
                    return new OkObjectResult(result.CurrentVersion);
                }

               job = await SendInstructAllocationsToJobService(result.BuildProject.SpecificationId, author.Id, author.Name, new Trigger
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

            await UpdateBuildProject(specificationSummary);

            string fundingStreamName = specificationSummary.FundingStreams.FirstOrDefault(_ => _.Id == calculation.FundingStreamId)?.Name;

            await UpdateSearch(calculation, specificationSummary.Name, fundingStreamName);

            PublishStatusResultModel result = new PublishStatusResultModel()
            {
                PublishStatus = calculation.Current.PublishStatus,
            };

            CalculationResponseModel currentVersion = calculation.ToResponseModel();

            await UpdateCalculationInCache(calculation, currentVersion);

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> GetCalculationCodeContext(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specificationId was provided to GetCalculationCodeContext");

                return new BadRequestObjectResult("Null or empty specificationId provided");
            }

            _logger.Information("Generating code context for {specificationId}", specificationId);

            BuildProject buildProject = await _buildProjectsService.GetBuildProjectForSpecificationId(specificationId);

            IEnumerable<Calculation> calculations = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId));

            buildProject.Build = _sourceCodeService.Compile(buildProject, calculations ?? Enumerable.Empty<Calculation>());

            if (buildProject.Build == null)
            {
                _logger.Error($"Build was null for Specification {specificationId} with Build Project ID {buildProject.Id}");

                return new StatusCodeResult(500);
            }

            IEnumerable<TypeInformation> result = await _sourceCodeService.GetTypeInformation(buildProject);

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> ReIndex()
        {
            await _searchRepository.DeleteIndex();

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

            Calculation calculation = await _calculationsRepository.GetCalculationsBySpecificationIdAndCalculationName(model.SpecificationId, model.Name);

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
                fieldIdentifiers.AddRange(currentFieldDefinitionNames.Select(m => $"Datasets.{VisualBasicTypeGenerator.GenerateIdentifier(datasetSpecificationRelationshipViewModel.Name)}.{VisualBasicTypeGenerator.GenerateIdentifier(m)}"));
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

        public async Task<IActionResult> AssociateTemplateIdWithSpecification(string specificationId, string templateVersion, string fundingStreamId)
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

            Common.ApiClient.Models.ApiResponse<Common.TemplateMetadata.Models.TemplateMetadataContents> fundingTemplateContentsResponse =
                await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingTemplateContents(fundingStreamId, 
                specificationSummary.FundingPeriod.Id,  templateVersion));

            if (fundingTemplateContentsResponse.StatusCode != HttpStatusCode.OK)
            {
                string message = $"Retrieve funding template with fundingStreamId: {fundingStreamId}, fundingPeriodId: {specificationSummary.FundingPeriod.Id} and templateId: {templateVersion} did not return OK.";
                _logger.Error(message);

                return new PreconditionFailedResult(message);
            }

            if (fundingTemplateContentsResponse == null || fundingTemplateContentsResponse.Content == null)
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
                    TemplateMappingItems = new List<TemplateMappingItem>(),
                };
            }
            else
            {
                existingSaveVersionOfTemplateMapping = JsonConvert.SerializeObject(templateMapping);
            }

            IEnumerable<CalculationMetadata> currentCalculations = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsMetatadataBySpecificationId(specificationId));

            ProcessTemplateMappingChanges(templateMapping, templateMetadataContents);

            HttpStatusCode setAssignedTemplateVersionStatusCode = await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.SetAssignedTemplateVersion(specificationId, templateVersion, fundingStreamId));
            if (setAssignedTemplateVersionStatusCode != HttpStatusCode.OK)
            {
                string message = $"Unable to set assigned template version for funding stream: {fundingStreamId} and templateId: {templateVersion} for specification ID {specificationId}, did not return OK, but {setAssignedTemplateVersionStatusCode}";
                _logger.Error(message);

                return new PreconditionFailedResult(message);
            }

            // Only save if changed
            if (existingSaveVersionOfTemplateMapping != JsonConvert.SerializeObject(templateMapping))
            {
                await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.UpdateTemplateMapping(specificationId, fundingStreamId, templateMapping));

                string cacheKey = $"{CacheKeys.TemplateMapping}{specificationId}-{fundingStreamId}";
                await _cachePolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<TemplateMapping>(cacheKey));
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

            if (templateMapping == null)
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

        public async Task<IActionResult> DeleteCalculations(Message message)
        {
            string specificationId = message.UserProperties["specification-id"].ToString();
            if (string.IsNullOrEmpty(specificationId))
                return new BadRequestObjectResult("Null or empty specification Id provided for deleting calculations");

            string deletionTypeProperty = message.UserProperties["deletion-type"].ToString();
            if (string.IsNullOrEmpty(deletionTypeProperty))
                return new BadRequestObjectResult("Null or empty deletion type provided for deleting calculations");

            await _calculationsRepository.DeleteCalculationsBySpecificationId(specificationId, deletionTypeProperty.ToDeletionType());

            return new OkResult();
        }

        public async Task<IActionResult> DeleteCalculationResults(Message message)
        {
            string specificationId = message.UserProperties["specification-id"].ToString();
            if (string.IsNullOrEmpty(specificationId))
                return new BadRequestObjectResult("Null or empty specification Id provided for deleting calculation results");

            string deletionTypeProperty = message.UserProperties["deletion-type"].ToString();
            if (string.IsNullOrEmpty(deletionTypeProperty))
                return new BadRequestObjectResult("Null or empty deletion type provided for deleting calculation results");

            await _calculationsRepository.DeleteCalculationResultsBySpecificationId(specificationId, deletionTypeProperty.ToDeletionType());

            return new OkResult();
        }

        private async Task<IActionResult> GetCalculationsForSpecification(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Warning("No specificationId was provided to GetCalculationSummariesForSpecification");

                return new BadRequestObjectResult("Null or empty specificationId provided");
            }

            string cacheKey = $"{CacheKeys.CalculationsForSpecification}{specificationId}";

            IEnumerable<Calculation> calculations = await _cachePolicy.ExecuteAsync(() => _cacheProvider.GetAsync<List<Calculation>>(cacheKey));
            if (calculations == null)
            {
                calculations = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId));

                if (calculations == null)
                {
                    _logger.Warning($"Calculations from repository returned null for specification ID of '{specificationId}'");

                    return new InternalServerErrorResult("Calculations from repository returned null");
                }

                await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetAsync(cacheKey, calculations, TimeSpan.FromDays(7), true));
            }

            return new OkObjectResult(calculations);
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

        private async Task<IEnumerable<Calculation>> UpdateCalculationCodeOnCalculationChange(string oldCalcSourceCodeName, string newCalcSourceCodeName, string specificationId, Reference user)
        {
            List<Calculation> updatedCalculations = new List<Calculation>();

            if (oldCalcSourceCodeName != newCalcSourceCodeName)
            {
                IEnumerable<Calculation> calculations = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId));

                foreach (Calculation calculation in calculations)
                {
                    string sourceCode = calculation.Current.SourceCode;
                    CalculationNamespace calcNamespace = calculation.Current.Namespace;

                    string result = _calculationCodeReferenceUpdate.ReplaceSourceCodeReferences(calculation,
                        oldCalcSourceCodeName,
                        newCalcSourceCodeName);

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
            await UpdateCalculationInCache(calculation, currentVersion);

            return new UpdateCalculationResult()
            {
                BuildProject = buildProject,
                Calculation = calculation,
                CurrentVersion = currentVersion,
            };
        }

        private async Task<BuildProject> UpdateBuildProject(SpecModel.SpecificationSummary specificationSummary)
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

            if (buildProject == null)
            {
                buildProject = await _buildProjectsService.GetBuildProjectForSpecificationId(specificationSummary.Id);
            }

            CompilerOptions compilerOptions = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCompilerOptions(specificationSummary.Id));

            if (compilerOptions == null)
            {
                compilerOptions = new CompilerOptions();
            }

            //forcing off for calc runs only
            compilerOptions.OptionStrictEnabled = false;

            buildProject.Build = _sourceCodeService.Compile(buildProject, calculations, compilerOptions);

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

        private async Task UpdateCalculationInCache(Calculation calculation, CalculationResponseModel currentVersion)
        {
            // Invalidate cached calculations for this specification
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.KeyDeleteAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{calculation.SpecificationId}"));
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.KeyDeleteAsync<List<CalculationResponseModel>>($"{CacheKeys.CurrentCalculationsForSpecification}{calculation.SpecificationId}"));
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.KeyDeleteAsync<List<CalculationResponseModel>>($"{CacheKeys.CalculationsMetadataForSpecification}{calculation.SpecificationId}"));


            // Set current version in cache
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetAsync($"{CacheKeys.CurrentCalculation}{calculation.Id}", currentVersion, TimeSpan.FromDays(7), true));
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

        private bool ProcessTemplateMappingChanges(TemplateMapping templateMapping, TemplateMetadataContents fundingTemplateContents)
        {
            bool madeChanges = false;

            List<FundingLine> allFundingLines = fundingTemplateContents
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

            foreach (var mapping in templateMapping.TemplateMappingItems)
            {
                if (mapping.EntityType == TemplateMappingEntityType.Calculation)
                {
                    bool stillExists = templateCalculations.Any(c => c.TemplateCalculationId == mapping.TemplateId);
                    if (!stillExists)
                    {
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
                    }
                }
            }

            return madeChanges;
        }
    }
}
