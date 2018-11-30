using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models;
using CalculateFunding.Models.Aggregations;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Code;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Models.Specs;
using CalculateFunding.Models.Versioning;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.Calcs.ResultModels;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.CodeMetadataGenerator.Interfaces;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Services;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Serilog;
using Calculation = CalculateFunding.Models.Calcs.Calculation;
using CalculationCurrentVersion = CalculateFunding.Models.Calcs.CalculationCurrentVersion;
using CalculationType = CalculateFunding.Models.Calcs.CalculationType;

namespace CalculateFunding.Services.Calcs
{
    public class CalculationService : ICalculationService, IHealthChecker
    {
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly ILogger _logger;
        private readonly ISearchRepository<CalculationIndex> _searchRepository;
        private readonly IValidator<Calculation> _calculationValidator;
        private readonly IBuildProjectsRepository _buildProjectsRepository;
        private readonly ICompilerFactory _compilerFactory;
        private readonly ISourceFileGenerator _sourceFileGenerator;
        private readonly IMessengerService _messengerService;
        private readonly ICodeMetadataGeneratorService _codeMetadataGenerator;
        private readonly ISpecificationRepository _specsRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly ITelemetry _telemetry;
        private readonly Polly.Policy _calculationRepositoryPolicy;
        private readonly Polly.Policy _calculationSearchRepositoryPolicy;
        private readonly Polly.Policy _cachePolicy;
        private readonly Polly.Policy _calculationVersionsRepositoryPolicy;
        private readonly Polly.Policy _specificationsRepositoryPolicy;
        private readonly Polly.Policy _buildProjectRepositoryPolicy;
        private readonly Polly.Policy _messagePolicy;
        private readonly Polly.Policy _jobsRepositoryPolicy;
        private readonly IVersionRepository<CalculationVersion> _calculationVersionRepository;
        private readonly IJobsRepository _jobsRepository;
        private readonly IFeatureToggle _featureToggle;

        public CalculationService(
            ICalculationsRepository calculationsRepository,
            ILogger logger,
            ITelemetry telemetry,
            ISearchRepository<CalculationIndex> searchRepository,
            IValidator<Calculation> calculationValidator,
            IBuildProjectsRepository buildProjectsRepository,
            ISourceFileGeneratorProvider sourceFileGeneratorProvider,
            ICompilerFactory compilerFactory,
            IMessengerService messengerService,
            ICodeMetadataGeneratorService codeMetadataGenerator,
            ISpecificationRepository specificationRepository,
            ICacheProvider cacheProvider,
            ICalcsResilliencePolicies resilliencePolicies,
            IVersionRepository<CalculationVersion> calculationVersionRepository,
            IJobsRepository jobsRepository,
            IFeatureToggle featureToggle)
        {
            Guard.ArgumentNotNull(codeMetadataGenerator, nameof(codeMetadataGenerator));
            Guard.ArgumentNotNull(specificationRepository, nameof(specificationRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            Guard.ArgumentNotNull(calculationValidator, nameof(calculationValidator));
            Guard.ArgumentNotNull(compilerFactory, nameof(compilerFactory));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(calculationVersionRepository, nameof(calculationVersionRepository));
            Guard.ArgumentNotNull(jobsRepository, nameof(jobsRepository));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

            _calculationsRepository = calculationsRepository;
            _logger = logger;
            _telemetry = telemetry;
            _searchRepository = searchRepository;
            _calculationValidator = calculationValidator;
            _buildProjectsRepository = buildProjectsRepository;
            _compilerFactory = compilerFactory;
            _sourceFileGenerator = sourceFileGeneratorProvider.CreateSourceFileGenerator(TargetLanguage.VisualBasic);
            _messengerService = messengerService;
            _codeMetadataGenerator = codeMetadataGenerator;
            _specsRepository = specificationRepository;
            _cacheProvider = cacheProvider;
            _calculationRepositoryPolicy = resilliencePolicies.CalculationsRepository;
            _calculationVersionRepository = calculationVersionRepository;
            _calculationSearchRepositoryPolicy = resilliencePolicies.CalculationsSearchRepository;
            _cachePolicy = resilliencePolicies.CacheProviderPolicy;
            _specificationsRepositoryPolicy = resilliencePolicies.SpecificationsRepositoryPolicy;
            _buildProjectRepositoryPolicy = resilliencePolicies.BuildProjectRepositoryPolicy;
            _messagePolicy = resilliencePolicies.MessagePolicy;
            _calculationVersionsRepositoryPolicy = resilliencePolicies.CalculationsVersionsRepositoryPolicy;
            _jobsRepository = jobsRepository;
            _jobsRepositoryPolicy = resilliencePolicies.JobsRepository;
            _featureToggle = featureToggle;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth calcsRepoHealth = await ((IHealthChecker)_calculationsRepository).IsHealthOk();
            var searchRepoHealth = await _searchRepository.IsHealthOk();
            string queueName = ServiceBusConstants.QueueNames.CalculationJobInitialiser;
            var messengerServiceHealth = await _messengerService.IsHealthOk(queueName);

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(CalculationService)
            };
            health.Dependencies.AddRange(calcsRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = searchRepoHealth.Ok, DependencyName = _searchRepository.GetType().GetFriendlyName(), Message = searchRepoHealth.Message });
            health.Dependencies.Add(new DependencyHealth { HealthOk = messengerServiceHealth.Ok, DependencyName = $"{_messengerService.GetType().GetFriendlyName()} for queue: {queueName}", Message = messengerServiceHealth.Message });

            return health;
        }

        Build Compile(BuildProject buildProject, IEnumerable<Calculation> calculations)
        {
            IEnumerable<SourceFile> sourceFiles = _sourceFileGenerator.GenerateCode(buildProject, calculations);

            //string sourceDirectory = @"c:\dev\vbout";
            //foreach (SourceFile sourceFile in sourceFiles)
            //{
            //    string filename = sourceDirectory + "\\" + sourceFile.FileName;
            //    string directory = System.IO.Path.GetDirectoryName(filename);
            //    if (!System.IO.Directory.Exists(directory))
            //    {
            //        System.IO.Directory.CreateDirectory(directory);
            //    }

            //    System.IO.File.WriteAllText(filename, sourceFile.SourceCode);
            //}

            ICompiler compiler = _compilerFactory.GetCompiler(sourceFiles);

            return compiler.GenerateCode(sourceFiles?.ToList());
        }

        async public Task<IActionResult> GetCalculationHistory(HttpRequest request)
        {
            request.Query.TryGetValue("calculationId", out var calcId);

            string calculationId = calcId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to GetCalculationHistory");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            IEnumerable<CalculationVersion> history = await _calculationVersionsRepositoryPolicy.ExecuteAsync(() =>_calculationVersionRepository.GetVersions(calculationId));

            if (history.IsNullOrEmpty())
            {
                _logger.Information($"A calculation was not found for calculation id {calculationId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(history);
        }

        async public Task<IActionResult> GetCalculationVersions(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            CalculationVersionsCompareModel compareModel = JsonConvert.DeserializeObject<CalculationVersionsCompareModel>(json);

            //Need custom validator here

            if (compareModel == null || string.IsNullOrEmpty(compareModel.CalculationId) || compareModel.Versions == null || compareModel.Versions.Count() < 2)
            {
                _logger.Warning("A null or invalid compare model was provided for comparing models");

                return new BadRequestObjectResult("A null or invalid compare model was provided for comparing models");
            }

            IEnumerable<CalculationVersion> allVersions = await _calculationVersionsRepositoryPolicy.ExecuteAsync(() => _calculationVersionRepository.GetVersions(compareModel.CalculationId));

            if (allVersions.IsNullOrEmpty())
            {
                _logger.Information($"No history was not found for calculation id {compareModel.CalculationId}");

                return new NotFoundResult();
            }

            IList<CalculationVersion> versions = new List<CalculationVersion>();

            foreach (var version in compareModel.Versions)
            {
                versions.Add(allVersions.FirstOrDefault(m => m.Version == version));
            }

            if (!versions.Any())
            {
                _logger.Information($"A calculation was not found for calculation id {compareModel.CalculationId}");

                return new NotFoundResult();
            }

            return new OkObjectResult(versions);
        }

        async public Task<IActionResult> GetCalculationCurrentVersion(HttpRequest request)
        {
            request.Query.TryGetValue("calculationId", out var calcId);

            var calculationId = calcId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to GetCalculationCurrentVersion");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            string cacheKey = $"{CacheKeys.CurrentCalculation}{calculationId}";

            CalculationCurrentVersion calculation = await _cachePolicy.ExecuteAsync(() => _cacheProvider.GetAsync<CalculationCurrentVersion>(cacheKey));

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

                calculation = GetCurrentVersionFromCalculation(repoCalculation);

                await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetAsync<CalculationCurrentVersion>(cacheKey, calculation, TimeSpan.FromDays(7), true));
            }

            return new OkObjectResult(calculation);
        }

        async public Task<IActionResult> GetCalculationById(HttpRequest request)
        {
            request.Query.TryGetValue("calculationId", out var calcId);

            var calculationId = calcId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to GetCalculationById");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            Models.Calcs.Calculation calculation = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationById(calculationId));

            if (calculation != null)
            {
                _logger.Information($"A calculation was found for calculation id {calculationId}");

                return new OkObjectResult(calculation);
            }

            _logger.Information($"A calculation was not found for calculation id {calculationId}");

            return new NotFoundResult();
        }

        public async Task<IActionResult> GetCurrentCalculationsForSpecification(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            string specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Warning("No specificationId was provided to GetCalculationsForSpecification");

                return new BadRequestObjectResult("Null or empty specificationId provided");
            }
            string cacheKey = $"{CacheKeys.CurrentCalculationsForSpecification}{specificationId}";

            List<CalculationCurrentVersion> calculations = await _cachePolicy.ExecuteAsync(() => _cacheProvider.GetAsync<List<CalculationCurrentVersion>>(cacheKey));
            if (calculations == null)
            {
                IEnumerable<Calculation> calculationsFromRepository = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId));

                if (calculationsFromRepository == null)
                {
                    _logger.Warning($"Calculations from repository returned null for specification ID of '{specificationId}'");

                    return new InternalServerErrorResult("Calculations from repository returned null");
                }

                calculations = new List<CalculationCurrentVersion>(calculationsFromRepository.Count());
                foreach (Calculation calculation in calculationsFromRepository)
                {
                    calculations.Add(GetCurrentVersionFromCalculation(calculation));
                }

                await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetAsync<List<CalculationCurrentVersion>>(cacheKey, calculations, TimeSpan.FromDays(7), true));
            }

            return new OkObjectResult(calculations);
        }

        public async Task<IActionResult> GetCalculationSummariesForSpecification(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            string specificationId = specId.FirstOrDefault();

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
                    calculations.Add(GetCalculationSummaryFromCalculation(calculation));
                }

                await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetAsync<List<CalculationSummaryModel>>(cacheKey, calculations, TimeSpan.FromDays(7), true));
            }

            return new OkObjectResult(calculations);
        }

        async public Task CreateCalculation(Message message)
        {
            Reference user = message.GetUserDetails();

            Calculation calculation = message.GetPayloadAsInstanceOf<Calculation>();

            if (calculation == null)
            {
                _logger.Error("A null calculation was provided to CalculateFunding.Services.Calcs.CreateCalculation");
            }
            else
            {
                var validationResult = await _calculationValidator.ValidateAsync(calculation);

                if (!validationResult.IsValid)
                {
                    throw new InvalidModelException(GetType().ToString(), validationResult.Errors.Select(m => m.ErrorMessage).ToArraySafe());
                }

                Models.Specs.SpecificationSummary specificationSummary = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specsRepository.GetSpecificationSummaryById(calculation.SpecificationId));
                if (specificationSummary == null)
                {
                    throw new InvalidModelException(typeof(CalculationService).ToString(), new[] { $"Specification with ID '{calculation.SpecificationId}' not found" });
                }

                CalculationVersion calculationVersion = new CalculationVersion
                {
                    PublishStatus = PublishStatus.Draft,
                    Author = user,
                    Date = DateTimeOffset.Now.ToLocalTime(),
                    Version = 1,
                    DecimalPlaces = 6,
                    SourceCode = CodeGenerationConstants.VisualBasicDefaultSourceCode,
                    CalculationId = calculation.Id
                };

                calculation.Current = calculationVersion;

                Calculation calculationDuplicateCheck = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationByCalculationSpecificationId(calculation.CalculationSpecification.Id));

                if (calculationDuplicateCheck != null)
                {
                    _logger.Error($"The calculation with the same id {calculation.CalculationSpecification.Id} has already been created");
                    throw new InvalidOperationException($"The calculation with the same id {calculation.CalculationSpecification.Id} has already been created");
                }

                HttpStatusCode result = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.CreateDraftCalculation(calculation));

                if (result.IsSuccess())
                {
                    _logger.Information($"Calculation with id: {calculation.Id} was succesfully saved to Cosmos Db");

                    await _calculationVersionsRepositoryPolicy.ExecuteAsync(() => _calculationVersionRepository.SaveVersion(calculationVersion));

                    await UpdateSearch(calculation, specificationSummary.Name);

                    await UpdateBuildProject(calculation.SpecificationId);
                }
                else
                {
                    _logger.Error($"There was problem creating a new calculation with id {calculation.Id} in Cosmos Db with status code {(int)result}");
                }
            }
        }

        public async Task UpdateCalculationsForSpecification(Message message)
        {
            Models.Specs.SpecificationVersionComparisonModel specificationVersionComparison = message.GetPayloadAsInstanceOf<Models.Specs.SpecificationVersionComparisonModel>();

            if (specificationVersionComparison == null || specificationVersionComparison.Current == null)
            {
                _logger.Error("A null specificationVersionComparison was provided to UpdateCalulationsForSpecification");

                throw new InvalidModelException(nameof(Models.Specs.SpecificationVersionComparisonModel), new[] { "Null or invalid model provided" });
            }

            if (specificationVersionComparison.HasNoChanges && !specificationVersionComparison.HasNameChange && !specificationVersionComparison.HasPolicyChanges)
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

            IEnumerable<string> fundingStreamIds = specificationVersionComparison.Current.FundingStreams?.Select(m => m.Id);

            IList<CalculationIndex> calcIndexes = new List<CalculationIndex>();

            foreach (Calculation calculation in calculations)
            {
                calculation.FundingPeriod = specificationVersionComparison.Current.FundingPeriod;

                if (!fundingStreamIds.IsNullOrEmpty() && !fundingStreamIds.Contains(calculation.FundingStream?.Id))
                {
                    calculation.FundingStream = null;
                    calculation.AllocationLine = null;
                }

                if (!calculation.Policies.IsNullOrEmpty())
                {
                    Models.Specs.Policy policy = Models.Specs.ExtensionMethods.GetPolicy(specificationVersionComparison.Current, calculation.Policies.First().Id);

                    if (policy != null)
                    {
                        calculation.Policies.First().Name = policy.Name;
                    }
                }
                calcIndexes.Add(CreateCalculationIndexItem(calculation, specificationVersionComparison.Current.Name));
            }

            BuildProject buildProject = await _buildProjectRepositoryPolicy.ExecuteAsync(() => _buildProjectsRepository.GetBuildProjectBySpecificationId(specificationId));

            if (buildProject == null)
            {
                _logger.Warning($"A build project could not be found for specification id: {specificationId}");

                buildProject = await CreateBuildProject(specificationId, calculations);
            }

            await TaskHelper.WhenAllAndThrow(
                _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.UpdateCalculations(calculations)),
                _calculationSearchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Index(calcIndexes)),
                _buildProjectRepositoryPolicy.ExecuteAsync(() => _buildProjectsRepository.UpdateBuildProject(buildProject))
            );

            IDictionary<string, string> properties = message.BuildMessageProperties();

            if (_featureToggle.IsJobServiceEnabled())
            {
                string userId = properties.ContainsKey("user-id") ? properties["user-id"] : "";
                string userName = properties.ContainsKey("user-name") ? properties["user-name"] : "";
                string correlationId = message.GetCorrelationId();

                try
                {
                    Trigger trigger = new Trigger
                    {
                        EntityId = specificationId,
                        EntityType = nameof(Specification),
                        Message = $"Updating calculations for specification: '{specificationId}'"
                    };

                    Job job = await SendInstructAllocationsToJobService(specificationId, userId, userName, trigger, correlationId);

                    _logger.Information($"New job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' created with id: '{job.Id}'");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{specificationId}'");

                    throw new Exception($"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{specificationId}'", ex);
                }
            }
            else
            {

                properties.Add("specification-id", specificationId);

                await _messagePolicy.ExecuteAsync(() => _messengerService.SendToQueue<string>(ServiceBusConstants.QueueNames.CalculationJobInitialiser,
                    null,
                    properties));
            }
        }

        public async Task UpdateCalculationsForCalculationSpecificationChange(Message message)
        {
            Models.Specs.CalculationVersionComparisonModel calculationVersionComparison = message.GetPayloadAsInstanceOf<Models.Specs.CalculationVersionComparisonModel>();

            if (calculationVersionComparison == null || calculationVersionComparison.Current == null || calculationVersionComparison.Previous == null)
            {
                _logger.Error("A null calculationVersionComparison was provided to UpdateCalculationsForCalculationSpecificationChange");

                throw new InvalidModelException(nameof(Models.Specs.CalculationVersionComparisonModel), new[] { "Null or invalid model provided" });
            }

            string calculationId = calculationVersionComparison.CalculationId;

            string specificationId = calculationVersionComparison.SpecificationId;

            if (!calculationVersionComparison.HasChanges)
            {
                _logger.Information("No changes detected for calculation with id: '{calculationId}' on specification '{specificationId}'", calculationId, specificationId);

                return;
            }

            Models.Specs.SpecificationSummary specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specsRepository.GetSpecificationSummaryById(specificationId));

            if (specification == null)
            {
                throw new Exception($"Specification could not be found for specification id : {specificationId}");
            }

            List<Calculation> calculationsToUpdate = new List<Calculation>();

            if (calculationVersionComparison.Current.Name != calculationVersionComparison.Previous.Name)
            {
                IEnumerable<Calculation> updatedCalculations = await UpdateCalculationCodeOnCalculationSpecificationChange(calculationVersionComparison, message.GetUserDetails());
                calculationsToUpdate.AddRange(updatedCalculations);
            }

            Calculation calculation = calculationsToUpdate.FirstOrDefault(m => m.CalculationSpecification.Id == calculationId);

            if (calculation == null)
            {
                calculation = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationByCalculationSpecificationId(calculationId));

                if (calculation == null)
                {
                    throw new Exception($"Calculation could not be found for calculation id : {calculationId}");
                }

                calculationsToUpdate.Add(calculation);
            }

            calculation.Name = calculationVersionComparison.Current.Name;

            calculation.Description = calculationVersionComparison.Current.Description;

            calculation.IsPublic = calculationVersionComparison.Current.IsPublic;

            calculation.AllocationLine = calculationVersionComparison.Current.AllocationLine;

            if ((int)calculation.CalculationType != (int)calculationVersionComparison.Current.CalculationType)
            {
                if (calculationVersionComparison.Current.CalculationType == Models.Specs.CalculationType.Number)
                {
                    calculation.AllocationLine = null;
                }
                calculation.CalculationType = (CalculationType)calculationVersionComparison.Current.CalculationType;
            }

            if (!string.IsNullOrWhiteSpace(calculation.AllocationLine?.Id)
                && (calculation.AllocationLine.Id != calculationVersionComparison.Previous.AllocationLine?.Id || calculation.FundingStream == null))
            {
                string[] fundingStreamIdsForSpecification = specification.FundingStreams.Select(fs => fs.Id).ToArraySafe();

                List<FundingStream> fundingStreamInSpecificationsAsList = new List<FundingStream>();

                if (!fundingStreamIdsForSpecification.IsNullOrEmpty())
                {
                    IEnumerable<FundingStream> allfundingStreams = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specsRepository.GetFundingStreams());

                    foreach (string fundingStreamId in fundingStreamIdsForSpecification)
                    {
                        FundingStream fundingStream = allfundingStreams.FirstOrDefault(m => m.Id == fundingStreamId);

                        if (fundingStream != null)
                        {
                            fundingStreamInSpecificationsAsList.Add(fundingStream);
                        }
                    }

                    FundingStream fundingStreamToAssign =
                        fundingStreamInSpecificationsAsList
                            .Select(fs => new { FundingStream = fs, fs.AllocationLines })
                            .FirstOrDefault(fsal => fsal.AllocationLines.Any(al => al.Id == calculation.AllocationLine.Id))
                            ?.FundingStream;
                    if (fundingStreamToAssign == null)
                    {
                        string errorTextFundingStreamNotFoundForAllocationLine = $"Calculation: {calculation.Id} could not be updated because allocation line: {calculation.AllocationLine.Id} did not belong to any funding stream in the system";
                        _logger.Error(errorTextFundingStreamNotFoundForAllocationLine);
                        throw new InvalidOperationException(errorTextFundingStreamNotFoundForAllocationLine);
                    }

                    calculation.FundingStream = fundingStreamToAssign;
                }
                else
                {
                    string errorTextSpecificationHasNoFundingStreams = $"Specification: {specification.Id} did not have any funding streams assigned to it";
                    _logger.Error(errorTextSpecificationHasNoFundingStreams);
                    throw new InvalidOperationException(errorTextSpecificationHasNoFundingStreams);
                }
            }

            await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.UpdateCalculations(calculationsToUpdate));

            await _cachePolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{specificationId}"));

            foreach (Calculation calculationToUpdate in calculationsToUpdate)
            {
                await _cachePolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<CalculationCurrentVersion>($"{CacheKeys.CurrentCalculation}{calculationToUpdate.Id}"));
            }

            IEnumerable<CalculationIndex> indexes = calculationsToUpdate.Select(m => CreateCalculationIndexItem(m, specification.Name)).ToArraySafe();

            IEnumerable<IndexError> indexingResults = await _calculationSearchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Index(indexes));

            await UpdateBuildProject(specificationId);
        }

        public async Task<IEnumerable<Calculation>> UpdateCalculationCodeOnCalculationSpecificationChange(Models.Specs.CalculationVersionComparisonModel comparison, Reference user)
        {
            List<Calculation> updatedCalculations = new List<Calculation>();

            if (comparison.Current.Name != comparison.Previous.Name)
            {
                IEnumerable<Calculation> calculations = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(comparison.SpecificationId));

                string existingFunctionName = VisualBasicTypeGenerator.GenerateIdentifier(comparison.Previous.Name);
                string sourceFieldRegex = $"\\b({existingFunctionName})\\((\\s)*\\)";
                string newFunctionReplacement = $"{VisualBasicTypeGenerator.GenerateIdentifier(comparison.Current.Name)}()";

                foreach (Calculation calculation in calculations)
                {
                    string result = Regex.Replace(calculation.Current.SourceCode, sourceFieldRegex, newFunctionReplacement);
                    if (result != calculation.Current.SourceCode)
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

        async public Task<IActionResult> SaveCalculationVersion(HttpRequest request)
        {
            request.Query.TryGetValue("calculationId", out var calcId);

            string calculationId = calcId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to GetCalculationHistory");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            string json = await request.GetRawBodyStringAsync();

            SaveSourceCodeVersion sourceCodeVersion = JsonConvert.DeserializeObject<SaveSourceCodeVersion>(json);

            if (sourceCodeVersion == null || string.IsNullOrWhiteSpace(sourceCodeVersion.SourceCode))
            {
                _logger.Error($"Null or empty source code was provided for calculation id {calculationId}");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            Calculation calculation = await _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationById(calculationId));
            if (calculation == null)
            {
                _logger.Error($"A calculation was not found for calculation id {calculationId}");

                return new NotFoundResult();
            }

            Reference user = request.GetUser();
            CalculationVersion calculationVersion;
            if (calculation.Current == null)
            {
                calculationVersion = new CalculationVersion
                {
                    Date = DateTimeOffset.Now.ToLocalTime(),
                    Author = user,
                    PublishStatus = PublishStatus.Draft,
                    Version = 1
                };
            }
            else
            {
                calculationVersion = calculation.Current.Clone() as CalculationVersion;
            }

            calculationVersion.DecimalPlaces = 6;
            calculationVersion.SourceCode = sourceCodeVersion.SourceCode;
            calculationVersion.CalculationId = calculationId;

            UpdateCalculationResult result = await UpdateCalculation(calculation, calculationVersion, user);

            if (_featureToggle.IsJobServiceEnabled())
            {
                string userId = !string.IsNullOrWhiteSpace(user.Id) ? user.Id : "";
                string userName = !string.IsNullOrWhiteSpace(user.Name) ? user.Name : "";

                try
                {
                    Trigger trigger = new Trigger
                    {
                        EntityId = calculation.Id,
                        EntityType = nameof(Calculation),
                        Message = $"Saving calculation: '{calculationId}' for specification: '{calculation.SpecificationId}'"
                    };

                    string correlationId = request.GetCorrelationId();

                    Job job = await SendInstructAllocationsToJobService(result.BuildProject.SpecificationId, userId, userName, trigger, correlationId);

                    _logger.Information($"New job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' created with id: '{job.Id}'");
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{result.BuildProject.SpecificationId}'");

                    return new InternalServerErrorResult($"Failed to create job of type '{JobConstants.DefinitionNames.CreateInstructAllocationJob}' on specification '{result.BuildProject.SpecificationId}'");
                }
            }
            else
            {
                await SendGenerateAllocationsMessage(result.BuildProject, request);
            }

            _telemetry.TrackEvent("InstructCalculationAllocationEventRun",
                 new Dictionary<string, string>()
                 {
                            { "specificationId" , result.BuildProject.SpecificationId },
                            { "buildProjectId" , result.BuildProject.Id },
                            { "calculationId" , calculationId }
                 },
                 new Dictionary<string, double>()
                 {
                        { "InstructCalculationAllocationEventRunCalc" , 1 },
                        { "InstructCalculationAllocationEventRun" , 1 }
                 }
             );

            return new OkObjectResult(result.CurrentVersion);
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

            if(updateBuildProject)
            {
                buildProject = await UpdateBuildProject(calculation.SpecificationId);
            }

            Models.Specs.SpecificationSummary specificationSummary = await _specsRepository.GetSpecificationSummaryById(calculation.SpecificationId);

            await UpdateSearch(calculation, specificationSummary.Name);

            CalculationCurrentVersion currentVersion = GetCurrentVersionFromCalculation(calculation);
            await UpdateCalculationInCache(calculation, currentVersion);           

            return new UpdateCalculationResult()
            {
                BuildProject = buildProject,
                Calculation = calculation,
                CurrentVersion = currentVersion,
            };
        }

        async public Task<IActionResult> UpdateCalculationStatus(HttpRequest request)
        {
            request.Query.TryGetValue("calculationId", out var calcId);

            var calculationId = calcId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(calculationId))
            {
                _logger.Error("No calculation Id was provided to EditCalculationStatus");

                return new BadRequestObjectResult("Null or empty calculation Id provided");
            }

            string json = await request.GetRawBodyStringAsync();

            EditStatusModel editStatusModel = null;

            try
            {
                editStatusModel = JsonConvert.DeserializeObject<EditStatusModel>(json);

                if (editStatusModel == null)
                {
                    _logger.Error("A null status model was provided");
                    return new BadRequestObjectResult("Null status model provided");
                }
            }
            catch (JsonSerializationException jse)
            {
                _logger.Error(jse, $"An invalid status was provided for calculation: {calculationId}");

                return new BadRequestObjectResult("An invalid status was provided");
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

            if ((calculation.Current.PublishStatus == PublishStatus.Approved || calculation.Current.PublishStatus == PublishStatus.Updated) && editStatusModel.PublishStatus == PublishStatus.Draft)
            {
                return new BadRequestObjectResult("Publish status can't be changed to Draft from Updated or Approved");
            }

            Models.Specs.SpecificationSummary specificationSummary = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specsRepository.GetSpecificationSummaryById(calculation.SpecificationId));
            if (specificationSummary == null)
            {
                return new PreconditionFailedResult("Specification not found");
            }

            Reference user = request.GetUser();

            CalculationVersion previousCalculationVersion = calculation.Current;

            CalculationVersion calculationVersion = calculation.Current.Clone() as CalculationVersion;

            calculationVersion.PublishStatus = editStatusModel.PublishStatus;

            HttpStatusCode statusCode = await UpdateCalculation(calculation, calculationVersion, previousCalculationVersion);

            if (!statusCode.IsSuccess())
            {
                return new StatusCodeResult((int)statusCode);
            }

            await UpdateBuildProject(calculation.SpecificationId);

            await UpdateSearch(calculation, specificationSummary.Name);

            PublishStatusResultModel result = new PublishStatusResultModel()
            {
                PublishStatus = calculation.Current.PublishStatus,
            };

            CalculationCurrentVersion currentVersion = GetCurrentVersionFromCalculation(calculation);

            await UpdateCalculationInCache(calculation, currentVersion);

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> GetCalculationCodeContext(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specificationId was provided to GetCalculationCodeContext");

                return new BadRequestObjectResult("Null or empty specificationId provided");
            }

            _logger.Information("Generating code context for {specificationId}", specificationId);

            BuildProject project = await _buildProjectRepositoryPolicy.ExecuteAsync(() => _buildProjectsRepository.GetBuildProjectBySpecificationId(specificationId));
            if (project == null)
            {
                Models.Specs.SpecificationSummary specificationSummary = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specsRepository.GetSpecificationSummaryById(specificationId));
                if (specificationSummary == null)
                {
                    return new PreconditionFailedResult("Specification not found");
                }

                project = await CreateBuildProject(specificationId, Enumerable.Empty<Calculation>());

                if (project == null)
                {
                    _logger.Error($"Build Project was unable to be created and returned null for Specification ID of '{specificationId}'");

                    return new StatusCodeResult(500);
                }
            }

            if (project.Build == null)
            {
                _logger.Error($"Build was null for Specification {specificationId} with Build Project ID {project.Id}");

                return new StatusCodeResult(500);
            }

            if (project.Build.AssemblyBase64 == null)
            {
                _logger.Error($"Build AssemblyBase64 was null for Specification {specificationId} with Build Project ID {project.Id}");

                return new StatusCodeResult(500);
            }

            if (project.Build.AssemblyBase64.Length == 0)
            {
                _logger.Error($"Build AssemblyBase64 was zero bytes for Specification {specificationId} with Build Project ID {project.Id}");

                return new StatusCodeResult(500);
            }

            byte[] rawAssembly = Convert.FromBase64String(project.Build.AssemblyBase64);

            IEnumerable<TypeInformation> result = _codeMetadataGenerator.GetTypeInformation(rawAssembly);

            return new OkObjectResult(result);
        }

        public async Task<IActionResult> ReIndex()
        {
            //Not spending too much time her as probably will go to sql server
            await _searchRepository.DeleteIndex();

            IEnumerable<Calculation> calculations = await _calculationsRepository.GetAllCalculations();

            IList<CalculationIndex> calcIndexItems = new List<CalculationIndex>();

            Dictionary<string, Models.Specs.SpecificationSummary> specifications = new Dictionary<string, Models.Specs.SpecificationSummary>();

            foreach (Calculation calculation in calculations)
            {
                Models.Specs.SpecificationSummary specification = null;
                if (specifications.ContainsKey(calculation.SpecificationId))
                {
                    specification = specifications[calculation.SpecificationId];
                }
                else
                {
                    specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specsRepository.GetSpecificationSummaryById(calculation.SpecificationId));
                    if (specification != null)
                    {
                        specifications.Add(calculation.SpecificationId, specification);
                    }
                }

                CalculationIndex indexItem = CreateCalculationIndexItem(calculation, specification?.Name);
                indexItem.CalculationType = calculation.AllocationLine == null ? CalculationType.Number.ToString() : CalculationType.Funding.ToString();

                calcIndexItems.Add(indexItem);
            }

            IEnumerable<IndexError> indexingResults = await _calculationSearchRepositoryPolicy.ExecuteAsync(() => _searchRepository.Index(calcIndexItems));

            if (indexingResults.Any())
            {
                _logger.Error($"Failed to re-index calculation with the following errors: {string.Join(";", indexingResults.Select(m => m.ErrorMessage).ToArraySafe())}");

                return new StatusCodeResult(500);
            }

            return new NoContentResult();
        }

        async public Task<IActionResult> GetCalculationStatusCounts(HttpRequest request)
        {
            string json = await request.GetRawBodyStringAsync();

            SpecificationListModel specifications = JsonConvert.DeserializeObject<SpecificationListModel>(json);

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

        async Task UpdateSearch(Calculation calculation, string specificationName)
        {
            IEnumerable<IndexError> indexingResults = await _searchRepository.Index(new List<CalculationIndex>
            {
                CreateCalculationIndexItem(calculation, specificationName)
            });
        }

        CalculationIndex CreateCalculationIndexItem(Calculation calculation, string specificationName)
        {
            return new CalculationIndex
            {
                Id = calculation.Id,
                Name = calculation.Name,
                CalculationSpecificationId = calculation.CalculationSpecification.Id,
                CalculationSpecificationName = calculation.CalculationSpecification.Name,
                SpecificationName = specificationName,
                SpecificationId = calculation.SpecificationId,
                FundingPeriodId = calculation.FundingPeriod.Id,
                FundingPeriodName = calculation.FundingPeriod.Name,
                AllocationLineId = calculation.AllocationLine == null ? string.Empty : calculation.AllocationLine.Id,
                AllocationLineName = calculation.AllocationLine != null ? calculation.AllocationLine.Name : "No allocation line set",
                PolicySpecificationIds = calculation.Policies.Select(m => m.Id).ToArraySafe(),
                PolicySpecificationNames = calculation.Policies.Select(m => m.Name).ToArraySafe(),
                SourceCode = calculation.Current.SourceCode,
                Status = calculation.Current.PublishStatus.ToString(),
                FundingStreamId = calculation.FundingStream == null ? string.Empty : calculation.FundingStream.Id,
                FundingStreamName = calculation.FundingStream == null ? "No funding stream set" : calculation.FundingStream.Name,
                LastUpdatedDate = calculation.Current.Date,
                CalculationType = calculation.CalculationType.ToString()
            };
        }

        public async Task<BuildProject> CreateBuildProject(string specificationId, IEnumerable<Calculation> calculations)
        {
            BuildProject buildproject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId
            };

            buildproject.Build = Compile(buildproject, calculations);

            await _buildProjectRepositoryPolicy.ExecuteAsync(() => _buildProjectsRepository.CreateBuildProject(buildproject));

            return buildproject;
        }

        async Task<BuildProject> UpdateBuildProject(string specificationId)
        {
            Task<IEnumerable<Calculation>> calculationsRequest = _calculationRepositoryPolicy.ExecuteAsync(() => _calculationsRepository.GetCalculationsBySpecificationId(specificationId));
            Task<BuildProject> buildProjectRequest = _buildProjectRepositoryPolicy.ExecuteAsync(() => _buildProjectsRepository.GetBuildProjectBySpecificationId(specificationId));
            Task<IEnumerable<Models.Specs.Calculation>> calculationSpecificationsRequest = _specificationsRepositoryPolicy.ExecuteAsync(() => _specsRepository.GetCalculationSpecificationsForSpecification(specificationId));

            await TaskHelper.WhenAllAndThrow(calculationsRequest, buildProjectRequest, calculationSpecificationsRequest);

            List<Calculation> calculations = new List<Calculation>(calculationsRequest.Result);
            BuildProject buildProject = buildProjectRequest.Result;
            IEnumerable<Models.Specs.Calculation> calculationSpecifications = calculationSpecificationsRequest.Result;

            // Adds the Calculation Description retrieved from the Calculation Specification.
            // Other descriptions are included as part of the denormalised data storage in CosmosDB
            foreach (Models.Specs.Calculation specCalculation in calculationSpecifications)
            {
                Calculation calculation = calculations.Where(c => c.CalculationSpecification.Id == specCalculation.Id).FirstOrDefault();
                if (calculation != null)
                {
                    calculation.Description = specCalculation.Description;
                }
            }

            return await UpdateBuildProject(specificationId, calculations, buildProject);
        }

        private async Task<BuildProject> UpdateBuildProject(string specificationId, IEnumerable<Calculation> calculations, BuildProject buildProject = null)
        {
            if (buildProject == null)
            {
                _logger.Warning($"Build project for specification {specificationId} could not be found, creating a new one");

                buildProject = await CreateBuildProject(specificationId, calculations);
            }
            else
            {
                buildProject.Build = Compile(buildProject, calculations);
                await _buildProjectRepositoryPolicy.ExecuteAsync(() => _buildProjectsRepository.UpdateBuildProject(buildProject));
            }

            return buildProject;
        }

        CalculationCurrentVersion GetCurrentVersionFromCalculation(Calculation calculation)
        {
            CalculationCurrentVersion calculationCurrentVersion = new CalculationCurrentVersion
            {
                SpecificationId = calculation.SpecificationId,
                Author = calculation.Current?.Author,
                Date = calculation.Current?.Date,
                CalculationSpecification = calculation.CalculationSpecification,
                FundingPeriodName = calculation.FundingPeriod.Name,
                FundingPeriodId = calculation.FundingPeriod.Id,
                PublishStatus = calculation.Current.PublishStatus,
                Id = calculation.Id,
                Name = calculation.Name,
                SourceCode = calculation.Current?.SourceCode ?? CodeGenerationConstants.VisualBasicDefaultSourceCode,
                Version = calculation.Current.Version,
                CalculationType = calculation.CalculationType.ToString(),
            };

            return calculationCurrentVersion;
        }

        CalculationSummaryModel GetCalculationSummaryFromCalculation(Calculation calculation)
        {
            CalculationSummaryModel calculationCurrentVersion = new CalculationSummaryModel
            {
                Id = calculation.Id,
                Name = calculation.Name,
                CalculationType = calculation.CalculationType,
                IsPublic = calculation.IsPublic,
                Status = calculation.Current.PublishStatus,
                Version = calculation.Current.Version
            };

            return calculationCurrentVersion;
        }

        private Task SendGenerateAllocationsMessage(BuildProject buildProject, HttpRequest request)
        {
            IDictionary<string, string> properties = request.BuildMessageProperties();

            properties.Add("specification-id", buildProject.SpecificationId);
            properties.Add("buildproject-id", buildProject.Id);

            return _messagePolicy.ExecuteAsync(() => _messengerService.SendToQueue<string>(ServiceBusConstants.QueueNames.CalculationJobInitialiser,
                null,
                properties));
        }

        private async Task<Job> SendInstructAllocationsToJobService(string specificationId, string userId, string userName, Trigger trigger, string correlationId)
        {
            JobCreateModel job = new JobCreateModel
            {
                InvokerUserDisplayName = userName,
                InvokerUserId = userId,
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructAllocationJob,
                SpecificationId = specificationId,
                Properties = new Dictionary<string, string>
                {
                    { "specification-id", specificationId }
                },
                Trigger = trigger,
                CorrelationId = correlationId
            };

            return await _jobsRepositoryPolicy.ExecuteAsync(() => _jobsRepository.CreateJob(job));
        }

        IDictionary<string, string> CreateMessageProperties(HttpRequest request)
        {
            Reference user = request.GetUser();

            IDictionary<string, string> properties = new Dictionary<string, string>
            {
                { "sfa-correlationId", request.GetCorrelationId() }
            };

            if (user != null)
            {
                properties.Add("user-id", user.Id);
                properties.Add("user-name", user.Name);
            }

            return properties;
        }

        private async Task UpdateCalculationInCache(Calculation calculation, CalculationCurrentVersion currentVersion)
        {
            // Invalidate cached calculations for this specification
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.KeyDeleteAsync<List<CalculationSummaryModel>>($"{CacheKeys.CalculationsSummariesForSpecification}{calculation.SpecificationId}"));
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.KeyDeleteAsync<List<CalculationCurrentVersion>>($"{CacheKeys.CurrentCalculationsForSpecification}{calculation.SpecificationId}"));

            // Set current version in cache
            await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetAsync<CalculationCurrentVersion>($"{CacheKeys.CurrentCalculation}{calculation.Id}", currentVersion, TimeSpan.FromDays(7), true));
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
    }
}
