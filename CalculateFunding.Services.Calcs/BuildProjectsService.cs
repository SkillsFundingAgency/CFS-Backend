using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.CodeGeneration.VisualBasic;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
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
        private readonly IBuildProjectsRepository _buildProjectsRepository;
        private readonly IMessengerService _messengerService;
        private readonly ILogger _logger;
        private readonly ITelemetry _telemetry;
        private readonly IProviderResultsRepository _providerResultsRepository;
        private readonly ISpecificationRepository _specificationsRepository;
        private readonly ICompilerFactory _compilerFactory;
        private readonly ISourceFileGeneratorProvider _sourceFileGeneratorProvider;
        private readonly ISourceFileGenerator _sourceFileGenerator;
        private readonly ICacheProvider _cacheProvider;
        private readonly ICalculationService _calculationService;
        private readonly ICalculationsRepository _calculationsRepository;
        private readonly IFeatureToggle _featureToggle;
        private readonly IJobsApiClient _jobsApiClient;
        private readonly Polly.Policy _jobsApiClientPolicy;
        private readonly EngineSettings _engineSettings;

        public BuildProjectsService(
            IBuildProjectsRepository buildProjectsRepository,
            IMessengerService messengerService,
            ILogger logger,
            ITelemetry telemetry,
            IProviderResultsRepository providerResultsRepository,
            ISpecificationRepository specificationsRepository,
            ISourceFileGeneratorProvider sourceFileGeneratorProvider,
            ICompilerFactory compilerFactory,
            ICacheProvider cacheProvider,
            ICalculationService calculationService,
            ICalculationsRepository calculationsRepository,
            IFeatureToggle featureToggle,
            IJobsApiClient jobsApiClient,
            ICalcsResilliencePolicies resilliencePolicies,
            EngineSettings engineSettings)
        {
            Guard.ArgumentNotNull(buildProjectsRepository, nameof(buildProjectsRepository));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(providerResultsRepository, nameof(providerResultsRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(sourceFileGeneratorProvider, nameof(sourceFileGeneratorProvider));
            Guard.ArgumentNotNull(compilerFactory, nameof(compilerFactory));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(calculationService, nameof(calculationService));
            Guard.ArgumentNotNull(calculationsRepository, nameof(calculationsRepository));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(resilliencePolicies, nameof(resilliencePolicies));
            Guard.ArgumentNotNull(engineSettings, nameof(engineSettings));

            _buildProjectsRepository = buildProjectsRepository;
            _messengerService = messengerService;
            _logger = logger;
            _telemetry = telemetry;
            _providerResultsRepository = providerResultsRepository;
            _specificationsRepository = specificationsRepository;
            _compilerFactory = compilerFactory;
            _sourceFileGeneratorProvider = sourceFileGeneratorProvider;
            _sourceFileGenerator = sourceFileGeneratorProvider.CreateSourceFileGenerator(TargetLanguage.VisualBasic);
            _cacheProvider = cacheProvider;
            _calculationService = calculationService;
            _calculationsRepository = calculationsRepository;
            _featureToggle = featureToggle;
            _jobsApiClient = jobsApiClient;
            _jobsApiClientPolicy = resilliencePolicies.JobsApiClient;
            _engineSettings = engineSettings;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth calcsRepoHealth = await ((IHealthChecker)_calculationsRepository).IsHealthOk();
            var cacheRepoHealth = await _cacheProvider.IsHealthOk();
            string queueName = ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResults;
            var messengerServiceHealth = await _messengerService.IsHealthOk(queueName);

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(BuildProjectsService)
            };
            health.Dependencies.AddRange(calcsRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheRepoHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheRepoHealth.Message });
            health.Dependencies.Add(new DependencyHealth { HealthOk = messengerServiceHealth.Ok, DependencyName = $"{_messengerService.GetType().GetFriendlyName()} for queue: {queueName}", Message = messengerServiceHealth.Message });

            return health;
        }

        public async Task UpdateAllocations(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            JobViewModel job = null;

            if (_featureToggle.IsJobServiceEnabled() && !message.UserProperties.ContainsKey("jobId"))
            {
                _logger.Error("Missing parent job id to instruct generating allocations");

                return;
            }

            if (_featureToggle.IsJobServiceEnabled())
            {
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
            }

            IDictionary<string, string> properties = message.BuildMessageProperties();

            string specificationId = message.UserProperties["specification-id"].ToString();

            BuildProject buildProject = await _buildProjectsRepository.GetBuildProjectBySpecificationId(specificationId);

            if (buildProject == null)
            {
                _logger.Error("A null build project was provided to UpdateAllocations");

                throw new ArgumentNullException(nameof(buildProject), $"A null build project was provided to UpdateAllocations for specification Id {specificationId}");
            }

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


                if (!_featureToggle.IsJobServiceEnabled())
                {
                    await _messengerService.SendToQueue<string>(ServiceBusConstants.QueueNames.CalcEngineGenerateAllocationResults, null, properties);
                }
                else
                {
                    IDictionary<string, string> jobProperties = new Dictionary<string, string>();

                    foreach (KeyValuePair<string, string> item in properties)
                    {
                        jobProperties.Add(item.Key, item.Value);

                    }
                    allJobProperties.Add(jobProperties);
                }
            }

            if (_featureToggle.IsAllocationLineMajorMinorVersioningEnabled())
            {
                await _specificationsRepository.UpdateCalculationLastUpdatedDate(specificationId);
            }

            if (_featureToggle.IsJobServiceEnabled())
            {
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
        }

        public async Task<IActionResult> UpdateBuildProjectRelationships(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

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

            if (buildProject == null)
            {
                return new StatusCodeResult(412);
            }

            if (buildProject.DatasetRelationships == null)
            {
                buildProject.DatasetRelationships = new List<DatasetRelationshipSummary>();
            }

            if (!buildProject.DatasetRelationships.Any(m => m.Name == relationship.Name))
            {
                buildProject.DatasetRelationships.Add(relationship);

                await CompileBuildProject(buildProject);
            }

            return new OkObjectResult(buildProject);
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

            if (buildProject == null)
            {
                SpecificationSummary specification = await _specificationsRepository.GetSpecificationSummaryById(specificationId);

                if (specification == null)
                {
                    throw new Exception($"Unable to find specification for specification id: {specificationId}");
                }

                buildProject = await _calculationService.CreateBuildProject(specificationId, Enumerable.Empty<Models.Calcs.Calculation>());

                if (buildProject == null)
                {
                    throw new Exception($"Unable to find create build project for specification id: {specificationId}");
                }
            }

            if (buildProject.DatasetRelationships == null)
            {
                buildProject.DatasetRelationships = new List<DatasetRelationshipSummary>();
            }

            if (!buildProject.DatasetRelationships.Any(m => m.Name == relationship.Name))
            {
                buildProject.DatasetRelationships.Add(relationship);

                await CompileBuildProject(buildProject);
            }
        }

        public async Task<IActionResult> GetBuildProjectBySpecificationId(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetBuildProjectBySpecificationId");

                return new BadRequestObjectResult("Null or empty specificationId Id provided");
            }

            BuildProject buildProject = await GetBuildProjectForSpecificationId(specificationId);

            if (buildProject == null)
            {
                return new NotFoundResult();
            }

            return new OkObjectResult(buildProject);
        }

        async Task<BuildProject> GetBuildProjectForSpecificationId(string specificationId)
        {
            BuildProject buildProject = await _buildProjectsRepository.GetBuildProjectBySpecificationId(specificationId);

            if (buildProject == null)
            {
                SpecificationSummary specificationSummary = await _specificationsRepository.GetSpecificationSummaryById(specificationId);
                if (specificationSummary == null)
                {
                    _logger.Error($"Failed to find build project for specification id: {specificationId}");

                    return null;
                }
                else
                {
                    buildProject = await _calculationService.CreateBuildProject(specificationSummary.Id, Enumerable.Empty<Models.Calcs.Calculation>());
                }
            }

            if (buildProject.Build == null)
            {
                _logger.Error($"Failed to find build project assembly for build project id: {buildProject.Id}");

                return null;
            }

            return buildProject;
        }

        public async Task CompileBuildProject(BuildProject buildProject)
        {
            IEnumerable<Models.Calcs.Calculation> calculations = await _calculationsRepository.GetCalculationsBySpecificationId(buildProject.SpecificationId);

            buildProject.Build = Compile(buildProject, calculations);

            HttpStatusCode statusCode = await _buildProjectsRepository.UpdateBuildProject(buildProject);

            if (!statusCode.IsSuccess())
            {
                throw new Exception($"Failed to update build project for id: {buildProject.Id} with status code {statusCode.ToString()}");
            }
        }

        public async Task<IActionResult> OutputBuildProjectToFilesystem(HttpRequest request)
        {
            request.Query.TryGetValue("specificationId", out var specId);

            var specificationId = specId.FirstOrDefault();

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetBuildProjectBySpecificationId");

                return new BadRequestObjectResult("Null or empty specificationId Id provided");
            }

            BuildProject buildProject = await GetBuildProjectForSpecificationId(specificationId);

            if (buildProject == null)
            {
                return new NotFoundResult();
            }

            //IEnumerable<Models.Calcs.Calculation> calculations = await _calculationsRepository.GetCalculationsBySpecificationId(specificationId);

            //IEnumerable<SourceFile> sourceFiles = _sourceFileGenerator.GenerateCode(buildProject, calculations);

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


            return new OkObjectResult(buildProject);

        }

        public async Task UpdateDeadLetteredJobLog(Message message)
        {
            if (!_featureToggle.IsJobServiceEnabled())
            {
                return;
            }

            Guard.ArgumentNotNull(message, nameof(message));

            if (!message.UserProperties.ContainsKey("jobId"))
            {
                _logger.Error("Missing job id from dead lettered message");
                return;
            }

            string jobId = message.UserProperties["jobId"].ToString();

            Common.ApiClient.Jobs.Models.JobLogUpdateModel jobLogUpdateModel = new Common.ApiClient.Jobs.Models.JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = $"The job has exceeded its maximum retry count and failed to complete successfully"
            };

            try
            {
                ApiResponse<Common.ApiClient.Jobs.Models.JobLog> jobLogResponse = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(jobId, jobLogUpdateModel));

                if (jobLogResponse == null || jobLogResponse.Content == null)
                {
                    _logger.Error($"Failed to add a job log for job id '{jobId}'");
                }
                else
                {
                    _logger.Information($"A new job log was added to inform of a dead lettered message with job log id '{jobLogResponse.Content.Id}' on job with id '{jobId}' while attempting to instruct allocations");
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Failed to add a job log for job id '{jobId}'");
            }
        }

        Task<HttpStatusCode> UpdateBuildProject(BuildProject buildProject)
        {
            return _buildProjectsRepository.UpdateBuildProject(buildProject);
        }

        Build Compile(BuildProject buildProject, IEnumerable<Models.Calcs.Calculation> calculations)
        {
            IEnumerable<SourceFile> sourceFiles = _sourceFileGenerator.GenerateCode(buildProject, calculations);

            ICompiler compiler = _compilerFactory.GetCompiler(sourceFiles);

            return compiler.GenerateCode(sourceFiles?.ToList());
        }

        private async Task<IEnumerable<Job>> CreateGenerateAllocationJobs(JobViewModel parentJob, IEnumerable<IDictionary<string, string>> jobProperties)
        {
            HashSet<string> calculationsToAggregate = new HashSet<string>();

            if (parentJob.JobDefinitionId == JobConstants.DefinitionNames.CreateInstructGenerateAggregationsAllocationJob)
            {

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
    }

}
