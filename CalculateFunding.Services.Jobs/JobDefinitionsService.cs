using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Jobs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Common.Caching;
using CalculateFunding.Services.Jobs.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.Models.HealthCheck;

namespace CalculateFunding.Services.Jobs
{
    public class JobDefinitionsService : IJobDefinitionsService, IHealthChecker
    {
        private readonly IJobDefinitionsRepository _jobDefinitionsRepository;
        private readonly ILogger _logger;
        private readonly Polly.AsyncPolicy _jobDefinitionsRepositoryPolicy;
        private readonly ICacheProvider _cacheProvider;
        private readonly Polly.AsyncPolicy _cachePolicy;

        public JobDefinitionsService(IJobDefinitionsRepository jobDefinitionsRepository, 
            ILogger logger, IJobsResiliencePolicies resiliencePolicies, ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(jobDefinitionsRepository, nameof(jobDefinitionsRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(resiliencePolicies?.JobDefinitionsRepository, nameof(resiliencePolicies.JobDefinitionsRepository));
            Guard.ArgumentNotNull(resiliencePolicies?.CacheProviderPolicy, nameof(resiliencePolicies.CacheProviderPolicy));

            _jobDefinitionsRepository = jobDefinitionsRepository;
            _logger = logger;
            _jobDefinitionsRepositoryPolicy = resiliencePolicies.JobDefinitionsRepository;
            _cacheProvider = cacheProvider;
            _cachePolicy = resiliencePolicies.CacheProviderPolicy;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth jobsDefinitionsRepoHealth = await ((IHealthChecker)_jobDefinitionsRepository).IsHealthOk();
            (bool Ok, string Message) cacheHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth
            {
                Name = nameof(JobDefinitionsService)
            };
            
            health.Dependencies.AddRange(jobsDefinitionsRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth
            {
                HealthOk = cacheHealth.Ok, 
                DependencyName = _cacheProvider.GetType().GetFriendlyName(), 
                Message = cacheHealth.Message
            });
            
            return health;
        }

        public async Task<IActionResult> SaveDefinition(JobDefinition definition)
        {
            try
            {
                Guard.ArgumentNotNull(definition, nameof(definition));
                
                HttpStatusCode result = await _jobDefinitionsRepositoryPolicy.ExecuteAsync(() => _jobDefinitionsRepository.SaveJobDefinition(definition));

                if (!result.IsSuccess())
                {
                    int statusCode = (int)result;

                    _logger.Error($"Failed to save json file: {definition.Id} to cosmos db with status {statusCode}");

                    return new StatusCodeResult(statusCode);
                }
            }
            catch (Exception exception)
            {
                _logger.Error(exception, $"Exception occurred writing job definition {definition?.Id} to cosmos db");

                throw;
            }

            await _cachePolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<List<JobDefinition>>(CacheKeys.JobDefinitions));

            return new NoContentResult();
        }

        public async Task<IActionResult> GetJobDefinitions()
        {
            IEnumerable<JobDefinition> jobDefinitions = await GetAllJobDefinitions();

            if (jobDefinitions.IsNullOrEmpty())
            {
                return new NotFoundObjectResult("No job definitions were found");
            }

            return new OkObjectResult(jobDefinitions);
        }

        public async Task<IActionResult> GetJobDefinitionById(string jobDefinitionId)
        {
            if (string.IsNullOrWhiteSpace(jobDefinitionId))
            {
                return new BadRequestObjectResult("Job definition id was not provid");
            }

            IEnumerable<JobDefinition> jobDefinitions = await GetAllJobDefinitions();

            JobDefinition jobDefinition = jobDefinitions?.FirstOrDefault(m => m.Id == jobDefinitionId);

            if(jobDefinition == null)
            {
                return new NotFoundObjectResult($"No job definitions were found for id {jobDefinitionId}");
            }

            return new OkObjectResult(jobDefinition);
        }

        public async Task<IEnumerable<JobDefinition>> GetAllJobDefinitions()
        {
            IEnumerable<JobDefinition> jobDefinitions = await _cachePolicy.ExecuteAsync(() => _cacheProvider.GetAsync<List<JobDefinition>>(CacheKeys.JobDefinitions));

            if (!jobDefinitions.IsNullOrEmpty())
            {
                return jobDefinitions;
            }

            jobDefinitions = await _jobDefinitionsRepositoryPolicy.ExecuteAsync(() => _jobDefinitionsRepository.GetJobDefinitions());

            if (!jobDefinitions.IsNullOrEmpty())
            {
                await _cachePolicy.ExecuteAsync(() => _cacheProvider.SetAsync(CacheKeys.JobDefinitions, jobDefinitions.ToList()));
            }

            return jobDefinitions;
        }
    }
}
