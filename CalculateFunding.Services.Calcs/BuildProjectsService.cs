using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.Results;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public class BuildProjectsService : IBuildProjectsService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly ITelemetry _telemetry;
        private readonly IProviderResultsRepository _providerResultsRepository;
        private readonly ISpecificationRepository _specificationsRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly IFeatureToggle _featureToggle;
        private readonly IJobsApiClient _jobsApiClient;
        private readonly Polly.Policy _jobsApiClientPolicy;
        private readonly EngineSettings _engineSettings;
        private readonly ISourceCodeService _sourceCodeService;
        private readonly IDatasetRepository _datasetRepository;
        private readonly Polly.Policy _datasetRepositoryPolicy;
        private readonly IBuildProjectsRepository _buildProjectsRepository;
        private readonly Polly.Policy _buildProjectsRepositoryPolicy;

        public BuildProjectsService(
            ILogger logger,
            ITelemetry telemetry,
            IProviderResultsRepository providerResultsRepository,
            ISpecificationRepository specificationsRepository,
            ICacheProvider cacheProvider,
            ICalculationsRepository calculationsRepository,
            IFeatureToggle featureToggle,
            IJobsApiClient jobsApiClient,
            ICalcsResiliencePolicies resiliencePolicies,
            EngineSettings engineSettings,
            ISourceCodeService sourceCodeService,
            IDatasetRepository datasetRepository,
            IBuildProjectsRepository buildProjectsRepository)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(providerResultsRepository, nameof(providerResultsRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(engineSettings, nameof(engineSettings));
            Guard.ArgumentNotNull(sourceCodeService, nameof(sourceCodeService));
            Guard.ArgumentNotNull(datasetRepository, nameof(datasetRepository));
            Guard.ArgumentNotNull(buildProjectsRepository, nameof(buildProjectsRepository));

            _logger = logger;
            _telemetry = telemetry;
            _providerResultsRepository = providerResultsRepository;
            _specificationsRepository = specificationsRepository;
            _cacheProvider = cacheProvider;
            _calculationsRepository = calculationsRepository;
            _featureToggle = featureToggle;
            _jobsApiClient = jobsApiClient;
            _jobsApiClientPolicy = resiliencePolicies.JobsApiClient;
            _engineSettings = engineSettings;
            _sourceCodeService = sourceCodeService;
            _datasetRepository = datasetRepository;
            _datasetRepositoryPolicy = resiliencePolicies.DatasetsRepository;
            _buildProjectsRepository = buildProjectsRepository;
            _buildProjectsRepositoryPolicy = resiliencePolicies.BuildProjectRepositoryPolicy;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth calcsRepoHealth = await ((IHealthChecker)_calculationsRepository).IsHealthOk();
            (bool Ok, string Message) cacheRepoHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(BuildProjectsService)
            };
            health.Dependencies.AddRange(calcsRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheRepoHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheRepoHealth.Message });

            return health;
        }

        public async Task UpdateAllocations(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            JobViewModel job = null;

            if (!message.UserProperties.ContainsKey("jobId"))
            {
                _logger.Error("Missing parent job id to instruct generating allocations");

                return;
            }

            string jobId = message.UserProperties["jobId"].ToString();

            ApiResponse<JobViewModel> response = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.GetJobById(jobId));

            if (response == null || response.Content == null)
            {
                _logger.Error($"Could not find the parent job with job id: '{jobId}'");

                throw new Exception($"Could not find the parent job with job id: '{jobId}'");
            }

            job = response.Content;

            if (job.CompletionStatus.HasValue)
            {
                _logger.Information($"Received job with id: '{job.Id}' is already in a completed state with status {job.CompletionStatus.ToString()}");

                return;
            }

            await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(jobId, new Common.ApiClient.Jobs.Models.JobLogUpdateModel()));

            IDictionary<string, string> properties = message.BuildMessageProperties();

            string specificationId = message.UserProperties["specification-id"].ToString();

            BuildProject buildProject = await GetBuildProjectForSpecificationId(specificationId);

            if (message.UserProperties.ContainsKey("ignore-save-provider-results"))
            {
                properties.Add("ignore-save-provider-results", "true");
            }

            string cacheKey = "";

            if (message.UserProperties.ContainsKey("provider-cache-key"))
            {
                cacheKey = message.UserProperties["provider-cache-key"].ToString();
            }
            else
            {
                cacheKey = $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";
            }

            bool summariesExist = await _cacheProvider.KeyExists<ProviderSummary>(cacheKey);
            long totalCount = await _cacheProvider.ListLengthAsync<ProviderSummary>(cacheKey);

            bool refreshCachedScopedProviders = false;

            if (summariesExist)
            {
                IEnumerable<string> scopedProviderIds = await _providerResultsRepository.GetScopedProviderIds(specificationId);

                if (scopedProviderIds.Count() != totalCount)
                {
                    refreshCachedScopedProviders = true;
                }
                else
                {
                    IEnumerable<ProviderSummary> cachedScopedSummaries = await _cacheProvider.ListRangeAsync<ProviderSummary>(cacheKey, 0, (int)totalCount);

                    IEnumerable<string> differences = scopedProviderIds.Except(cachedScopedSummaries.Select(m => m.Id));

                    refreshCachedScopedProviders = differences.AnyWithNullCheck();
                }
            }

            if (!summariesExist || refreshCachedScopedProviders)
            {
                totalCount = await _providerResultsRepository.PopulateProviderSummariesForSpecification(specificationId);
            }

            const string providerSummariesPartitionIndex = "provider-summaries-partition-index";

            const string providerSummariesPartitionSize = "provider-summaries-partition-size";

            properties.Add(providerSummariesPartitionSize, _engineSettings.MaxPartitionSize.ToString());

            properties.Add("provider-cache-key", cacheKey);

            properties.Add("specification-id", specificationId);

            IList<IDictionary<string, string>> allJobProperties = new List<IDictionary<string, string>>();

            for (int partitionIndex = 0; partitionIndex < totalCount; partitionIndex += _engineSettings.MaxPartitionSize)
            {
                if (properties.ContainsKey(providerSummariesPartitionIndex))
                {
                    properties[providerSummariesPartitionIndex] = partitionIndex.ToString();
                }
                else
                {
                    properties.Add(providerSummariesPartitionIndex, partitionIndex.ToString());
                }

                IDictionary<string, string> jobProperties = new Dictionary<string, string>();

                foreach (KeyValuePair<string, string> item in properties)
                {
                    jobProperties.Add(item.Key, item.Value);

                }
                allJobProperties.Add(jobProperties);
            }

            if (_featureToggle.IsAllocationLineMajorMinorVersioningEnabled())
            {
                await _specificationsRepository.UpdateCalculationLastUpdatedDate(specificationId);
            }

            try
            {
                if (!allJobProperties.Any())
                {
                    _logger.Information($"No scoped providers set for specification '{specificationId}'");

                    JobLogUpdateModel jobCompletedLog = new JobLogUpdateModel
                    {
                        CompletedSuccessfully = true,
                        Outcome = "Calculations not run as no scoped providers set for specification"
                    };
                    await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(job.Id, jobCompletedLog));

                    return;
                }

                IEnumerable<Job> newJobs = await CreateGenerateAllocationJobs(job, allJobProperties);

                int newJobsCount = newJobs.Count();
                int batchCount = allJobProperties.Count();

                if (newJobsCount != batchCount)
                {
                    throw new Exception($"Only {newJobsCount} child jobs from {batchCount} were created with parent id: '{job.Id}'");
                }
                else
                {
                    _logger.Information($"{newJobsCount} child jobs were created for parent id: '{job.Id}'");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);

                throw new Exception($"Failed to create child jobs for parent job: '{job.Id}'");
            }
        }

        public async Task<IActionResult> UpdateBuildProjectRelationships(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out Microsoft.Extensions.Primitives.StringValues specId);

            string specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to UpdateBuildProjectRelationships");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            string json = await request.GetRawBodyStringAsync();

            DatasetRelationshipSummary relationship = JsonConvert.DeserializeObject<DatasetRelationshipSummary>(json);

            if (relationship == null)
            {
                _logger.Error("A null relationship message was provided to UpdateBuildProjectRelationships");

                return new BadRequestObjectResult("Null relationship provided");
            }

            BuildProject buildProject = await GetBuildProjectForSpecificationId(specificationId);

            IEnumerable<Models.Calcs.Calculation> calculations = await _calculationsRepository.GetCalculationsBySpecificationId(specificationId);

            buildProject.Build = _sourceCodeService.Compile(buildProject, calculations ?? Enumerable.Empty<Models.Calcs.Calculation>());

            if (!_featureToggle.IsDynamicBuildProjectEnabled())
            {
                await _buildProjectsRepositoryPolicy.ExecuteAsync(() => _buildProjectsRepository.UpdateBuildProject(buildProject));
            }

            await _sourceCodeService.SaveAssembly(buildProject);

            return new OkObjectResult(buildProject);
        }

        public async Task<IActionResult> CompileAndSaveAssembly(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            BuildProject buildProject = await GetBuildProjectForSpecificationId(specificationId);

            IEnumerable<Models.Calcs.Calculation> calculations = await _calculationsRepository.GetCalculationsBySpecificationId(specificationId);
            CompilerOptions compilerOptions = await _calculationsRepository.GetCompilerOptions(specificationId);

            buildProject.Build = _sourceCodeService.Compile(buildProject, calculations ?? Enumerable.Empty<Models.Calcs.Calculation>(), compilerOptions);

            if (!_featureToggle.IsDynamicBuildProjectEnabled())
            {
                await _buildProjectsRepositoryPolicy.ExecuteAsync(() => _buildProjectsRepository.UpdateBuildProject(buildProject));
            }

            await _sourceCodeService.SaveAssembly(buildProject);

            return new NoContentResult();
        }

        public async Task<IActionResult> GenerateAndSaveSourceProject(string specificationId, SourceCodeType sourceCodeType)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            BuildProject buildProject = await GetBuildProjectForSpecificationId(specificationId);

            IEnumerable<Models.Calcs.Calculation> calculations = await _calculationsRepository.GetCalculationsBySpecificationId(specificationId);
            CompilerOptions compilerOptions = await _calculationsRepository.GetCompilerOptions(specificationId);
            if (compilerOptions == null)
            {
                throw new InvalidOperationException("Compiler options returned were null");
            }

            if (sourceCodeType == SourceCodeType.Diagnostics)
            {
                compilerOptions.UseDiagnosticsMode = true;
            }

            IEnumerable<SourceFile> sourceFiles = _sourceCodeService.GenerateSourceFiles(buildProject, calculations, compilerOptions);

            await _sourceCodeService.SaveSourceFiles(sourceFiles, specificationId, sourceCodeType);

            return new NoContentResult();
        }

        public async Task UpdateBuildProjectRelationships(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            DatasetRelationshipSummary relationship = message.GetPayloadAsInstanceOf<DatasetRelationshipSummary>();

            if (relationship == null)
            {
                _logger.Error("A null relationship message was provided to UpdateBuildProjectRelationships");

                throw new ArgumentNullException(nameof(relationship));
            }

            if (!message.UserProperties.ContainsKey("specification-id"))
            {
                _logger.Error("Message properties does not contain a specification id");

                throw new KeyNotFoundException("specification-id");
            }

            string specificationId = message.UserProperties["specification-id"].ToString();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error($"Message does not contain a specification id");

                throw new ArgumentNullException(nameof(specificationId));
            }

            BuildProject buildProject = await GetBuildProjectForSpecificationId(specificationId);

            if (buildProject.DatasetRelationships == null)
            {
                buildProject.DatasetRelationships = new List<DatasetRelationshipSummary>();
            }

            if (!buildProject.DatasetRelationships.Any(m => m.Name == relationship.Name))
            {
                buildProject.DatasetRelationships.Add(relationship);

                IEnumerable<Models.Calcs.Calculation> calculations = await _calculationsRepository.GetCalculationsBySpecificationId(specificationId);
                CompilerOptions compilerOptions = await _calculationsRepository.GetCompilerOptions(specificationId);

                buildProject.Build = _sourceCodeService.Compile(buildProject, calculations ?? Enumerable.Empty<Models.Calcs.Calculation>(), compilerOptions);

                if (!_featureToggle.IsDynamicBuildProjectEnabled())
                {
                    await _buildProjectsRepositoryPolicy.ExecuteAsync(() => _buildProjectsRepository.UpdateBuildProject(buildProject));
                }

                await _sourceCodeService.SaveAssembly(buildProject);
            }
        }

        public async Task<IActionResult> GetBuildProjectBySpecificationId(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out Microsoft.Extensions.Primitives.StringValues specId);

            string specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetBuildProjectBySpecificationId");

                return new BadRequestObjectResult("Null or empty specificationId Id provided");
            }

            BuildProject buildProject = await GetBuildProjectForSpecificationId(specificationId);

            return new OkObjectResult(buildProject);
        }

        public async Task<BuildProject> GetBuildProjectForSpecificationId(string specificationId)
        {
            BuildProject buildProject = null;

            if (_featureToggle.IsDynamicBuildProjectEnabled())
            {
                buildProject = await GenerateBuildProject(specificationId);
            }
            else
            {
                buildProject = await _buildProjectsRepositoryPolicy.ExecuteAsync(() => _buildProjectsRepository.GetBuildProjectBySpecificationId(specificationId));

                if (buildProject == null)
                {
                    buildProject = await GenerateBuildProject(specificationId);

                    await _buildProjectsRepositoryPolicy.ExecuteAsync(() => _buildProjectsRepository.CreateBuildProject(buildProject));
                }
            }

            return buildProject;
        }

        public async Task<IActionResult> GetAssemblyBySpecificationId(string specificationId)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specificationId was provided to GetAssemblyBySpecificationId");

                return new BadRequestObjectResult("Null or empty specificationId provided");
            }

            BuildProject buildProject = await GetBuildProjectForSpecificationId(specificationId);

            byte[] assembly = await _sourceCodeService.GetAssembly(buildProject);

            if (assembly.IsNullOrEmpty())
            {
                _logger.Error($"Failed to get assembly for specification id '{specificationId}'");

                return new InternalServerErrorResult($"Failed to get assembly for specification id '{specificationId}'");
            }

            return new OkObjectResult(assembly);
        }

        private async Task<IEnumerable<Job>> CreateGenerateAllocationJobs(JobViewModel parentJob, IEnumerable<IDictionary<string, string>> jobProperties)
        {
            HashSet<string> calculationsToAggregate = new HashSet<string>();

            if (parentJob.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob)
            {
                string calculationAggregatesCacheKeyPrefix = $"{CacheKeys.CalculationAggregations}{parentJob.SpecificationId}";

                await _cacheProvider.RemoveByPatternAsync(calculationAggregatesCacheKeyPrefix);

                IEnumerable<Models.Calcs.Calculation> calculations = await _calculationsRepository.GetCalculationsBySpecificationId(parentJob.SpecificationId);

                foreach (Models.Calcs.Calculation calculation in calculations)
                {
                    IEnumerable<string> aggregateParameters = SourceCodeHelpers.GetCalculationAggregateFunctionParameters(calculation.Current.SourceCode);
                    if (aggregateParameters.IsNullOrEmpty())
                    {
                        continue;
                    }

                    foreach (string aggregateParameter in aggregateParameters)
                    {
                        Models.Calcs.Calculation referencedCalculation = calculations.FirstOrDefault(m => string.Equals(CalculationTypeGenerator.GenerateIdentifier(m.Name.Trim()), aggregateParameter.Trim(), StringComparison.InvariantCultureIgnoreCase));

                        if (referencedCalculation != null)
                        {
                            calculationsToAggregate.Add(aggregateParameter);
                        }
                    }
                }
            }

            IList<JobCreateModel> jobCreateModels = new List<JobCreateModel>();

            Trigger trigger = new Trigger
            {
                EntityId = parentJob.Id,
                EntityType = nameof(Job),
                Message = $"Triggered by parent job with id: '{parentJob.Id}"
            };

            int batchNumber = 1;
            int batchCount = jobProperties.Count();
            string calcsToAggregate = string.Join(",", calculationsToAggregate);

            foreach (IDictionary<string, string> properties in jobProperties)
            {
                properties.Add("batch-number", batchNumber.ToString());
                properties.Add("batch-count", batchCount.ToString());
                properties.Add("calculations-to-aggregate", calcsToAggregate);

                JobCreateModel jobCreateModel = new JobCreateModel
                {
                    InvokerUserDisplayName = parentJob.InvokerUserDisplayName,
                    InvokerUserId = parentJob.InvokerUserId,
                    JobDefinitionId = parentJob.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructAllocationJob ? JobConstants.DefinitionNames.CreateAllocationJob : JobConstants.DefinitionNames.GenerateCalculationAggregationsJob,
                    SpecificationId = parentJob.SpecificationId,
                    Properties = properties,
                    ParentJobId = parentJob.Id,
                    Trigger = trigger,
                    CorrelationId = parentJob.CorrelationId
                };

                batchNumber++;

                jobCreateModels.Add(jobCreateModel);
            }

            return await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.CreateJobs(jobCreateModels));
        }

        private async Task<BuildProject> GenerateBuildProject(string specificationId)
        {
            BuildProject buildproject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId,
                DatasetRelationships = new List<DatasetRelationshipSummary>(),
                Build = new Build()
            };

            IEnumerable<DatasetSpecificationRelationshipViewModel> datasetRelationshipModels = await _datasetRepositoryPolicy.ExecuteAsync(() => _datasetRepository.GetCurrentRelationshipsBySpecificationId(specificationId));

            if (!datasetRelationshipModels.IsNullOrEmpty())
            {
                ConcurrentBag<DatasetDefinition> datasetDefinitions = new ConcurrentBag<DatasetDefinition>();

                IList<Task> definitionTasks = new List<Task>();

                IEnumerable<string> definitionIds = datasetRelationshipModels.Select(m => m.Definition?.Id);

                foreach (string definitionId in definitionIds)
                {
                    Task task = Task.Run(async () =>
                    {
                        DatasetDefinition datasetDefinition = await _datasetRepositoryPolicy.ExecuteAsync(() => _datasetRepository.GetDatasetDefinitionById(definitionId));

                        if (datasetDefinition != null)
                        {
                            datasetDefinitions.Add(datasetDefinition);
                        }
                    });

                    definitionTasks.Add(task);
                }

                await TaskHelper.WhenAllAndThrow(definitionTasks.ToArray());

                foreach (DatasetSpecificationRelationshipViewModel datasetRelationshipModel in datasetRelationshipModels)
                {
                    buildproject.DatasetRelationships.Add(new DatasetRelationshipSummary
                    {
                        DatasetDefinitionId = datasetRelationshipModel.Definition.Id,
                        DatasetId = datasetRelationshipModel.DatasetId,
                        Relationship = new Common.Models.Reference(datasetRelationshipModel.Id, datasetRelationshipModel.Name),
                        DefinesScope = datasetRelationshipModel.IsProviderData,
                        Id = datasetRelationshipModel.Id,
                        Name = datasetRelationshipModel.Name,
                        DatasetDefinition = datasetDefinitions.FirstOrDefault(m => m.Id == datasetRelationshipModel.Definition.Id)
                    });
                }
            }

            return buildproject;
        }
    }
}
