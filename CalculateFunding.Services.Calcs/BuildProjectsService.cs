using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Calcs.Interfaces.CodeGen;
using CalculateFunding.Services.CodeGeneration;
using CalculateFunding.Services.Compiler;
using CalculateFunding.Services.Compiler.Interfaces;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Core.Interfaces.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Serilog;

namespace CalculateFunding.Services.Calcs
{
    public class BuildProjectsService : IBuildProjectsService, IHealthChecker
    {
        const int MaxPartitionSize = 1000;

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
        private readonly IJobsRepository _jobsRepository;
        private readonly Polly.Policy _jobsRepositoryPolicy;

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
            IJobsRepository jobsRepository,
            ICalcsResilliencePolicies resilliencePolicies)
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
            Guard.ArgumentNotNull(jobsRepository, nameof(jobsRepository));
            Guard.ArgumentNotNull(resilliencePolicies, nameof(resilliencePolicies));

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
            _jobsRepository = jobsRepository;
            _jobsRepositoryPolicy = resilliencePolicies.JobsRepository;
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

            if (_featureToggle.IsJobServiceEnabled() && !message.UserProperties.ContainsKey("jobId"))
            {
                _logger.Error("Missing parent job id to instruct generating allocations");

                return;
            }

            if (_featureToggle.IsJobServiceEnabled())
            {
                string jobId = message.UserProperties["jobId"].ToString();

                await _jobsRepositoryPolicy.ExecuteAsync(() => _jobsRepository.AddJobLog(jobId, new JobLogUpdateModel()));
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

            if (!summariesExist)
            {
                await _providerResultsRepository.PopulateProviderSummariesForSpecification(specificationId);
            }

            int totalCount = (int)(await _cacheProvider.ListLengthAsync<ProviderSummary>(cacheKey));

            const string providerSummariesPartitionIndex = "provider-summaries-partition-index";

            const string providerSummariesPartitionSize = "provider-summaries-partition-size";

            properties.Add(providerSummariesPartitionSize, MaxPartitionSize.ToString());

            properties.Add("provider-cache-key", cacheKey);

            properties.Add("specification-id", specificationId);

            IList<IDictionary<string, string>> allJobProperties = new List<IDictionary<string, string>>();

            for (int partitionIndex = 0; partitionIndex < totalCount; partitionIndex += MaxPartitionSize)
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
                string parentJobId = message.UserProperties["jobId"].ToString();

                JobViewModel parentJob = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobsRepository.GetJobById(parentJobId));

                if(parentJob == null)
                {
                    _logger.Error($"Could not find the parent job with job id: '{parentJobId}'");

                    throw new Exception($"Could not find the parent job with job id: '{parentJobId}'");
                }

                try
                {
                    Trigger trigger = new Trigger
                    {
                        EntityId = specificationId,
                        EntityType = "Specification",
                        Message = $"Instructing generate allocations for specification: '{specificationId}'"
                    };

                    IEnumerable<Job> newJobs = await CreateGenerateAllocationJobs(parentJob, allJobProperties, trigger);

                    int newJobsCount = newJobs.Count();
                    int batchCount = allJobProperties.Count();

                    if(newJobsCount != batchCount)
                    {
                        throw new Exception($"Only {newJobsCount} child jobs from {batchCount} were created with parent id: '{parentJob.Id}'");
                    }
                    else
                    {
                        _logger.Information($"{newJobsCount} child jobs were created for parent id: '{parentJob.Id}'");
                    }
                }
                catch(Exception ex)
                {
                    _logger.Error(ex.Message);

                    throw new Exception($"Failed to create child jobs for parent job: '{parentJob.Id}'");
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

            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = $"The job has exceeded its maximum retry count and failed to complete successfully"
            };

            try
            {
                JobLog jobLog = await _jobsRepositoryPolicy.ExecuteAsync(() => _jobsRepository.AddJobLog(jobId, jobLogUpdateModel));

                _logger.Information($"A new job log was added to inform of a dead lettered message with job log id '{jobLog.Id}' on job with id '{jobId}' while attempting to instruct allocations");
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

        private async Task<IEnumerable<Job>> CreateGenerateAllocationJobs(JobViewModel parentJob, IEnumerable<IDictionary<string,string>> jobProperties, Trigger trigger)
        {
            IList<JobCreateModel> jobCreateModels = new List<JobCreateModel>();

            foreach(IDictionary<string, string> properties in jobProperties)
            {
                JobCreateModel jobCreateModel = new JobCreateModel
                {
                    InvokerUserDisplayName = parentJob.InvokerUserDisplayName,
                    InvokerUserId = parentJob.InvokerUserId,
                    JobDefinitionId = JobConstants.DefinitionNames.CreateAllocationJob,
                    SpecificationId = parentJob.SpecificationId,
                    Properties = properties,
                    ParentJobId = parentJob.Id,
                    Trigger = trigger,
                    CorrelationId = parentJob.CorrelationId
                };

                jobCreateModels.Add(jobCreateModel);
            }

            return await _jobsRepositoryPolicy.ExecuteAsync(() => _jobsRepository.CreateJobs(jobCreateModels));
        }
    }
}
