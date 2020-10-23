using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.DataSets;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.ApiClient.Providers;
using CalculateFunding.Common.ApiClient.Specifications;
using CalculateFunding.Common.ApiClient.Specifications.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Common.TemplateMetadata.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Datasets.Schema;
using CalculateFunding.Models.Datasets.ViewModels;
using CalculateFunding.Models.ProviderLegacy;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.FeatureToggles;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Options;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;
using static CalculateFunding.Services.Core.Constants.JobConstants;
using CalculationEntity = CalculateFunding.Models.Graph.Entity<CalculateFunding.Common.ApiClient.Graph.Models.Calculation, CalculateFunding.Common.ApiClient.Graph.Models.Relationship>;
using FundingLine = CalculateFunding.Models.Calcs.FundingLine;
using GraphCalculation = CalculateFunding.Common.ApiClient.Graph.Models.Calculation;
using TemplateCalculation = CalculateFunding.Common.TemplateMetadata.Models.Calculation;
using TemplateCalculationType = CalculateFunding.Common.TemplateMetadata.Enums.CalculationType;
using TemplateFundingLine = CalculateFunding.Common.TemplateMetadata.Models.FundingLine;

namespace CalculateFunding.Services.Calcs
{
    public class BuildProjectsService : IBuildProjectsService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly ITelemetry _telemetry;
        private readonly IProvidersApiClient _providersApiClient;
        private readonly AsyncPolicy _providersApiClientPolicy;
        private readonly ICacheProvider _cacheProvider;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly ISourceFileRepository _sourceFileRepository;
        private readonly AsyncPolicy _calculationsRepositoryPolicy;
        private readonly IFeatureToggle _featureToggle;
        private readonly EngineSettings _engineSettings;
        private readonly ISourceCodeService _sourceCodeService;
        private readonly IDatasetsApiClient _datasetsApiClient;
        private readonly IJobManagement _jobManagement;
        private readonly AsyncPolicy _datasetsApiClientPolicy;
        private readonly IBuildProjectsRepository _buildProjectsRepository;
        private readonly AsyncPolicy _buildProjectsRepositoryPolicy;
        private readonly ICalculationEngineRunningChecker _calculationEngineRunningChecker;
        private readonly IGraphRepository _graphRepository;
        private readonly IMapper _mapper;
        private readonly ISpecificationsApiClient _specificationsApiClient;
        private readonly AsyncPolicy _specificationsApiClientPolicy;
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly AsyncPolicy _policiesApiClientPolicy;

        public BuildProjectsService(
            ILogger logger,
            ITelemetry telemetry,
            IProvidersApiClient providersApiClient,
            ICacheProvider cacheProvider,
            ICalculationsRepository calculationsRepository,
            IFeatureToggle featureToggle,
            ICalcsResiliencePolicies resiliencePolicies,
            EngineSettings engineSettings,
            ISourceCodeService sourceCodeService,
            IDatasetsApiClient datasetsApiClient,
            IBuildProjectsRepository buildProjectsRepository,
            ICalculationEngineRunningChecker calculationEngineRunningChecker,
            IJobManagement jobManagement,
            IGraphRepository graphRepository,
            IMapper mapper,
            ISpecificationsApiClient specificationsApiClient,
            IPoliciesApiClient policiesApiClient,
            ISourceFileRepository sourceFileRepository)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(engineSettings, nameof(engineSettings));
            Guard.ArgumentNotNull(sourceCodeService, nameof(sourceCodeService));
            Guard.ArgumentNotNull(datasetsApiClient, nameof(datasetsApiClient));
            Guard.ArgumentNotNull(buildProjectsRepository, nameof(buildProjectsRepository));
            Guard.ArgumentNotNull(providersApiClient, nameof(providersApiClient));
            Guard.ArgumentNotNull(calculationEngineRunningChecker, nameof(calculationEngineRunningChecker));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(graphRepository, nameof(graphRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.DatasetsApiClient, nameof(resiliencePolicies.DatasetsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.ProvidersApiClient, nameof(resiliencePolicies.ProvidersApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.BuildProjectRepositoryPolicy, nameof(resiliencePolicies.BuildProjectRepositoryPolicy));
            Guard.ArgumentNotNull(resiliencePolicies?.CalculationsRepository, nameof(resiliencePolicies.CalculationsRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(specificationsApiClient, nameof(specificationsApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.SpecificationsApiClient, nameof(resiliencePolicies.SpecificationsApiClient));
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(sourceFileRepository, nameof(sourceFileRepository));

            _logger = logger;
            _telemetry = telemetry;
            _providersApiClient = providersApiClient;
            _providersApiClientPolicy = resiliencePolicies.ProvidersApiClient;
            _cacheProvider = cacheProvider;
            _calculationsRepository = calculationsRepository;
            _calculationsRepositoryPolicy = resiliencePolicies.CalculationsRepository;
            _featureToggle = featureToggle;
            _engineSettings = engineSettings;
            _sourceCodeService = sourceCodeService;
            _datasetsApiClient = datasetsApiClient;
            _jobManagement = jobManagement;
            _graphRepository = graphRepository;
            _datasetsApiClientPolicy = resiliencePolicies.DatasetsApiClient;
            _buildProjectsRepository = buildProjectsRepository;
            _buildProjectsRepositoryPolicy = resiliencePolicies.BuildProjectRepositoryPolicy;
            _calculationEngineRunningChecker = calculationEngineRunningChecker;
            _mapper = mapper;
            _specificationsApiClient = specificationsApiClient;
            _specificationsApiClientPolicy = resiliencePolicies.SpecificationsApiClient;
            _policiesApiClient = policiesApiClient;
            _policiesApiClientPolicy = resiliencePolicies.PoliciesApiClient;
            _sourceFileRepository = sourceFileRepository;
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

            JobViewModel job;

            if (!message.UserProperties.ContainsKey("jobId"))
            {
                _logger.Error("Missing parent job id to instruct generating allocations");

                return;
            }

            string jobId = message.UserProperties["jobId"].ToString();

            try
            {
                job = await _jobManagement.RetrieveJobAndCheckCanBeProcessed(jobId);
            }
            catch (JobNotFoundException)
            {
                throw new NonRetriableException($"Could not find the parent job with job id: '{jobId}'");
            }
            catch (JobAlreadyCompletedException)
            {
                return;
            }

            await _jobManagement.AddJobLog(jobId, new JobLogUpdateModel());

            IDictionary<string, string> properties = message.BuildMessageProperties();

            string specificationId = message.UserProperties["specification-id"].ToString();

            IEnumerable<CalculationEntity> circularDependencies = await _graphRepository.GetCircularDependencies(specificationId);

            if (!circularDependencies.IsNullOrEmpty())
            {
                string errorMessage = $"circular dependencies exist for specification: '{specificationId}'";

                foreach (CalculationEntity calculationEntity in circularDependencies)
                {
                    int i = 0;

                    _logger.Information(new[] { calculationEntity.Node.CalculationName }.Concat(calculationEntity.Relationships.Reverse().Select(rel =>
                    {
                        try
                        {
                            return $"|--->{((object)rel.Two).AsJson().AsPoco<GraphCalculation>().CalculationName}".AddLeading(i * 3);
                        }
                        finally
                        {
                            i++;
                        }
                    })).Aggregate((partialLog, log) => $"{partialLog}\r\n{log}"));
                }

                LogAndThrowException<NonRetriableException>(errorMessage);
            }

            if (message.UserProperties.ContainsKey("ignore-save-provider-results"))
            {
                properties.Add("ignore-save-provider-results", "true");
            }

            string specificationSummaryCachekey = message.UserProperties.ContainsKey("specification-summary-cache-key") ? 
                message.UserProperties["specification-summary-cache-key"].ToString() : 
                $"{CacheKeys.SpecificationSummaryById}{specificationId}";

            bool specificationSummaryExists = await _cacheProvider.KeyExists<Models.Specs.SpecificationSummary>(specificationSummaryCachekey);
            if (!specificationSummaryExists)
            {
                ApiResponse<SpecificationSummary> specificationSummaryResponse = await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));
                if (!specificationSummaryResponse.StatusCode.IsSuccess())
                {
                    string errorMessage = $"Unable to get specification summary by id: '{specificationId}' with status code: {specificationSummaryResponse.StatusCode}";
                    LogAndThrowException<NonRetriableException>(errorMessage);
                }
            }

            string providerCacheKey = message.UserProperties.ContainsKey("provider-cache-key") ? 
                message.UserProperties["provider-cache-key"].ToString() : 
                $"{CacheKeys.ScopedProviderSummariesPrefix}{specificationId}";

            bool summariesExist = await _cacheProvider.KeyExists<ProviderSummary>(providerCacheKey);
            long? totalCount = await _cacheProvider.ListLengthAsync<ProviderSummary>(providerCacheKey);

            bool refreshCachedScopedProviders = false;

            if (summariesExist)
            {
                // if there are no provider results for specification then the call returns no content
                ApiResponse<IEnumerable<string>> scopedProviderIds = await _providersApiClient.GetScopedProviderIds(specificationId);

                if (scopedProviderIds?.Content != null)
                {
                    if (scopedProviderIds.Content.Count() != totalCount)
                    {
                        refreshCachedScopedProviders = true;
                    }
                    else
                    {
                        IEnumerable<ProviderSummary> cachedScopedSummaries = await _cacheProvider.ListRangeAsync<ProviderSummary>(providerCacheKey, 0, (int)totalCount);

                        IEnumerable<string> differences = scopedProviderIds.Content.Except(cachedScopedSummaries.Select(m => m.Id));

                        refreshCachedScopedProviders = differences.AnyWithNullCheck();
                    }
                }
                else
                {
                    // if there are no provider results then always refresh scoped providers
                    refreshCachedScopedProviders = true;
                }
            }

            if (!summariesExist || refreshCachedScopedProviders)
            {
                string correlationId = Guid.NewGuid().ToString();

                bool jobCompletedSuccessfully = await _jobManagement.QueueJobAndWait(async () =>
                {
                    ApiResponse<bool> refreshCacheFromApi = await _providersApiClientPolicy.ExecuteAsync(() =>
                                    _providersApiClient.RegenerateProviderSummariesForSpecification(specificationId, !summariesExist));

                    if (!refreshCacheFromApi.StatusCode.IsSuccess())
                    {
                        string errorMessage = $"Unable to re-generate scoped providers while building projects '{specificationId}' with status code: {refreshCacheFromApi.StatusCode}";

                        LogAndThrowException<NonRetriableException>(errorMessage);
                    }

                    // returns true if job queued
                    return refreshCacheFromApi.Content;
                },
                DefinitionNames.PopulateScopedProvidersJob,
                specificationId,
                correlationId,
                ServiceBusConstants.TopicNames.JobNotifications);

                // if scoped provider job not completed successfully
                if (!jobCompletedSuccessfully)
                {
                    string errorMessage = $"Unable to re-generate scoped providers while building projects '{specificationId}' job didn't complete successfully in time";
                    LogAndThrowException<NonRetriableException>(errorMessage);
                }

                totalCount = await _cacheProvider.ListLengthAsync<ProviderSummary>(providerCacheKey);
            }

            const string providerSummariesPartitionIndex = "provider-summaries-partition-index";

            const string providerSummariesPartitionSize = "provider-summaries-partition-size";

            properties.Add(providerSummariesPartitionSize, _engineSettings.MaxPartitionSize.ToString());

            properties.Add("provider-cache-key", providerCacheKey);

            properties.Add("specification-id", specificationId);

            properties.Add("specification-summary-cache-key", specificationSummaryCachekey);
            
            string assemblyETag = await _sourceFileRepository.GetAssemblyETag(specificationId);

            if (assemblyETag.IsNotNullOrWhitespace())
            {
                properties.Add("assembly-etag", assemblyETag);
            }

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
                    await _jobManagement.AddJobLog(job.Id, jobCompletedLog);

                    return;
                }

                IEnumerable<Job> newJobs = await CreateGenerateAllocationJobs(job, allJobProperties);

                int newJobsCount = newJobs.Count();
                int batchCount = allJobProperties.Count;

                if (newJobsCount != batchCount)
                {
                    throw new Exception($"Only {newJobsCount} child jobs from {batchCount} were created with parent id: '{job.Id}'");
                }
                else
                {
                    _logger.Information($"{newJobsCount} child jobs were created for parent id: '{job.Id}'");
                }
            }
            catch (RefreshJobRunningException ex)
            {
                await _jobManagement.UpdateJobStatus(jobId, new JobLogUpdateModel()
                {
                    CompletedSuccessfully = false,
                    Outcome = ex.Message
                });

                throw new NonRetriableException(ex.Message, ex);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to create child jobs for parent job: '{job.Id}'");

                throw;
            }
        }

        public async Task<IActionResult> UpdateBuildProjectRelationships(string specificationId, DatasetRelationshipSummary relationship)
        {
            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to UpdateBuildProjectRelationships");

                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

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
            (compilerOptions ?? (compilerOptions = new CompilerOptions())).ConfigureForReleaseBuild();

            buildProject.Build = _sourceCodeService.Compile(buildProject, calculations ?? Enumerable.Empty<Models.Calcs.Calculation>(), compilerOptions);

            if (buildProject.Build.Success)
            {
                if (!_featureToggle.IsDynamicBuildProjectEnabled())
                {
                    await _buildProjectsRepositoryPolicy.ExecuteAsync(() => _buildProjectsRepository.UpdateBuildProject(buildProject));
                }

                await _sourceCodeService.SaveAssembly(buildProject);
                return new NoContentResult();

            }

            ModelStateDictionary keyValuePairs = new ModelStateDictionary();
            for (int i = 0; i < buildProject.Build.CompilerMessages.Count(); i++)
            {
                keyValuePairs.AddModelError(i.ToString(), buildProject.Build.CompilerMessages[i].Message);
            }

            return new BadRequestObjectResult(keyValuePairs);
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

        public async Task<IActionResult> GetBuildProjectBySpecificationId(string specificationId)
        {
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

            ApiResponse<SpecificationSummary> specificationSummaryResponse = await _specificationsApiClientPolicy.ExecuteAsync(() => _specificationsApiClient.GetSpecificationSummaryById(specificationId));

            if (specificationSummaryResponse?.Content == null)
            {
                _logger.Error($"Failed to get specification for specification id '{specificationId}'");

                throw new ArgumentNullException(nameof(SpecificationSummary));
            }

            SpecificationSummary specificationSummary = specificationSummaryResponse.Content;

            IEnumerable<Reference> fundingStreams = specificationSummary.FundingStreams;
            Reference fundingPeriod = specificationSummary.FundingPeriod;
            IDictionary<string, string> templateIds = specificationSummary.TemplateIds;

            if (_featureToggle.IsDynamicBuildProjectEnabled())
            {
                buildProject = await GenerateBuildProject(specificationId, fundingStreams.Select(_ => (_, fundingPeriod)), templateIds);
            }
            else
            {
                buildProject = await _buildProjectsRepositoryPolicy.ExecuteAsync(() => _buildProjectsRepository.GetBuildProjectBySpecificationId(specificationId));

                if (buildProject == null)
                {
                    buildProject = await GenerateBuildProject(specificationId, fundingStreams.Select(_ => (_, fundingPeriod)), templateIds);

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

            byte[] assembly = await _sourceCodeService.GetAssembly(buildProject, false);

            if (assembly.IsNullOrEmpty())
            {
                _logger.Error($"Failed to get assembly for specification id '{specificationId}'");

                return new InternalServerErrorResult($"Failed to get assembly for specification id '{specificationId}'");
            }

            return new OkObjectResult(assembly);
        }

        private async Task<IEnumerable<Job>> CreateGenerateAllocationJobs(JobViewModel parentJob, IEnumerable<IDictionary<string, string>> jobProperties)
        {
            bool calculationEngineRunning = await _calculationEngineRunningChecker.IsCalculationEngineRunning(parentJob.SpecificationId, new string[] { DefinitionNames.RefreshFundingJob });

            if (calculationEngineRunning)
            {
                throw new RefreshJobRunningException($"Can not create job for specification: {parentJob.SpecificationId} as there is an existing Refresh Funding Job running for it. Please wait for that job to finish.");
            }

            HashSet<string> calculationsToAggregate = new HashSet<string>();

            if (parentJob.JobDefinitionId == DefinitionNames.CreateInstructGenerateAggregationsAllocationJob)
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
                        Models.Calcs.Calculation referencedCalculation = calculations.FirstOrDefault(m => string.Equals(VisualBasicTypeGenerator.GenerateIdentifier(m.Name.Trim()), aggregateParameter.Trim(), StringComparison.InvariantCultureIgnoreCase));

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
                    JobDefinitionId = parentJob.JobDefinitionId == DefinitionNames.CreateInstructAllocationJob ? DefinitionNames.CreateAllocationJob : DefinitionNames.GenerateCalculationAggregationsJob,
                    SpecificationId = parentJob.SpecificationId,
                    Properties = properties,
                    ParentJobId = parentJob.Id,
                    Trigger = trigger,
                    CorrelationId = parentJob.CorrelationId
                };

                batchNumber++;

                jobCreateModels.Add(jobCreateModel);
            }

            return await _jobManagement.QueueJobs(jobCreateModels);
        }
        
        private static IEnumerable<TemplateCalculation> GetCalculations(IEnumerable<TemplateCalculation> calculations)
        {
            return calculations?.SelectMany(_ =>
            {
                if (_.Type == TemplateCalculationType.Cash)
                {
                    return new[] { _ };
                }
                else
                {
                    _.Calculations ??= new TemplateCalculation[0];
                    return GetCalculations(_.Calculations);
                }
            });
        }

        private static IEnumerable<FundingLine> GetFundingLines(TemplateMetadataContents templateMetadataContents, string fundingStreamId)
        {
            IEnumerable<FundingLine> flattenedFundingLines = templateMetadataContents.RootFundingLines?.Flatten(_ =>
            {
                // get all calculations for current funding line
                _.Calculations = GetCalculations(_.Calculations);

                IEnumerable<TemplateFundingLine> currentFlattenedFundingLines = _.FundingLines?.Flatten(_ =>
                {
                    return _.FundingLines;
                });

                // concat all calculations for all funding lines below current funding line
                _.Calculations = _.Calculations?.Concat(currentFlattenedFundingLines?.SelectMany(_ => GetCalculations(_.Calculations)));

                return _.FundingLines;
            }).Where(_ => _.Calculations.AnyWithNullCheck()).Select(_ =>
            {
                return new FundingLine
                {
                    Id = _.TemplateLineId,
                    Name = _.Name,
                    Namespace = fundingStreamId,
                    SourceCodeName = VisualBasicTypeGenerator.GenerateIdentifier(_.Name),
                    Calculations = _.Calculations?.DistinctBy(_ => _.TemplateCalculationId).Select(calc => new FundingLineCalculation
                    {
                        Id = calc.TemplateCalculationId,
                        Name = calc.Name,
                        Namespace = fundingStreamId,
                        SourceCodeName = VisualBasicTypeGenerator.GenerateIdentifier(calc.Name)
                    }).ToList()
                };
            }).ToList();

            return flattenedFundingLines;
        }

        private async Task<BuildProject> GenerateBuildProject(string specificationId, IEnumerable<(Reference, Reference)> fundingStreamAndPeriods, IDictionary<string, string> templateIds)
        {
            BuildProject buildproject = new BuildProject
            {
                SpecificationId = specificationId,
                Id = Guid.NewGuid().ToString(),
                Name = specificationId,
                DatasetRelationships = new List<DatasetRelationshipSummary>(),
                Build = new Build()
            };

            Dictionary<string, Funding> funding = new Dictionary<string, Funding>();

            Dictionary<string, Task<TemplateMapping>> templateMappingForFundingStreams = new Dictionary<string, Task<TemplateMapping>>();

            Dictionary<string, Task<ApiResponse<TemplateMetadataContents>>> templateMetadataContentsResponses = new Dictionary<string, Task<ApiResponse<TemplateMetadataContents>>>();

            foreach ((Reference FundingStream, Reference FundingPeriod) in fundingStreamAndPeriods)
            {
                templateMappingForFundingStreams.Add(FundingStream.Id, _calculationsRepositoryPolicy.ExecuteAsync(
                             () => _calculationsRepository.GetTemplateMapping(specificationId, FundingStream.Id)));

                templateMetadataContentsResponses.Add(FundingStream.Id,
                     _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingTemplateContents(FundingStream.Id, FundingPeriod.Id, templateIds[FundingStream.Id])));

            }

            Task<ApiResponse<IEnumerable<Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel>>> datasetsRequest = _datasetsApiClientPolicy.ExecuteAsync(() => _datasetsApiClient.GetCurrentRelationshipsBySpecificationId(specificationId));

            await TaskHelper.WhenAllAndThrow(
                 Task.Run(() => templateMappingForFundingStreams.Values),
                 Task.Run(() => templateMetadataContentsResponses.Values),
                 Task.Run(() => datasetsRequest));

            foreach ((Reference fundingStream, Reference FundingPeriod) in fundingStreamAndPeriods)
            {

                TemplateMapping templateMapping = templateMappingForFundingStreams[fundingStream.Id].Result;

                if (templateMapping == null)
                {
                    LogAndThrowException<Exception>(
                        $"Did not locate Template Mapping for funding stream id {fundingStream.Id} and specification id {specificationId}");
                }



                if (templateMetadataContentsResponses[fundingStream.Id].Result?.Content == null)
                {
                    continue;
                }

                funding.Add(fundingStream.Id, new Funding
                {
                    Mappings = templateMapping.TemplateMappingItems.ToDictionary(_ => _.TemplateId, _ => _.CalculationId),
                    FundingLines = GetFundingLines(templateMetadataContentsResponses[fundingStream.Id].Result.Content, fundingStream.Id)
                });
            }

            buildproject.FundingLines = funding;

            ApiResponse<IEnumerable<Common.ApiClient.DataSets.Models.DatasetSpecificationRelationshipViewModel>> datasetsApiClientResponse = datasetsRequest.Result;

            if (!datasetsApiClientResponse.StatusCode.IsSuccess())
            {
                string message = $"No current dataset relationships found for specificationId '{specificationId}'.";
                LogAndThrowException<RetriableException>(message);
            }

            IEnumerable<DatasetSpecificationRelationshipViewModel> datasetRelationshipModels = _mapper.Map<IEnumerable<DatasetSpecificationRelationshipViewModel>>(datasetsApiClientResponse.Content);

            if (!datasetRelationshipModels.IsNullOrEmpty())
            {
                ConcurrentBag<DatasetDefinition> datasetDefinitions = new ConcurrentBag<DatasetDefinition>();

                IList<Task> definitionTasks = new List<Task>();

                IEnumerable<string> definitionIds = datasetRelationshipModels.Select(m => m.Definition?.Id);

                foreach (string definitionId in definitionIds)
                {
                    Task task = Task.Run(async () =>
                    {
                        ApiResponse<Common.ApiClient.DataSets.Models.DatasetDefinition> datasetDefinitionResponse = await _datasetsApiClientPolicy.ExecuteAsync(() => _datasetsApiClient.GetDatasetDefinitionById(definitionId));

                        if (!datasetDefinitionResponse.StatusCode.IsSuccess())
                        {
                            string message = $"No DatasetDefinition found for definition id '{definitionId}'.";
                            _logger.Error(message);
                            throw new RetriableException(message);
                        }

                        DatasetDefinition datasetDefinition = _mapper.Map<DatasetDefinition>(datasetDefinitionResponse.Content);
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
                        DatasetName = datasetRelationshipModel.DatasetName,
                        Relationship = new Reference(datasetRelationshipModel.Id, datasetRelationshipModel.Name),
                        DefinesScope = datasetRelationshipModel.IsProviderData,
                        Id = datasetRelationshipModel.Id,
                        Name = datasetRelationshipModel.Name,
                        DatasetDefinition = datasetDefinitions.FirstOrDefault(m => m.Id == datasetRelationshipModel.Definition.Id)
                    });
                }
            }

            return buildproject;
        }

        private void LogAndThrowException<T>(string message) where T : Exception
        {
            _logger.Error(message);
            throw (T)Activator.CreateInstance(typeof(T), message);
        }
    }
}
