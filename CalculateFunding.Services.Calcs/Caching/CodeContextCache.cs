using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Code;
using CalculateFunding.Models.Graph;
using CalculateFunding.Services.Calcs.Interfaces;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Jobs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Calcs.Caching
{
    public class CodeContextCache : JobProcessingService, ICodeContextCache
    {
        private readonly ICacheProvider _cache;
        private readonly AsyncPolicy _cacheResilience;
        private readonly ICodeContextBuilder _codeContextBuilder;
        private readonly IJobManagement _jobs;
        private readonly ILogger _logger;

        public CodeContextCache(ICacheProvider cache,
            ICodeContextBuilder codeContextBuilder,
            IJobManagement jobs,
            ICalcsResiliencePolicies resiliencePolicies,
            ILogger logger) : base(jobs, logger)
        {
            Guard.ArgumentNotNull(cache, nameof(cache));
            Guard.ArgumentNotNull(codeContextBuilder, nameof(codeContextBuilder));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(resiliencePolicies.CacheProviderPolicy, nameof(resiliencePolicies.CacheProviderPolicy));
            Guard.ArgumentNotNull(jobs, nameof(jobs));

            _cache = cache;
            _codeContextBuilder = codeContextBuilder;
            _cacheResilience = resiliencePolicies.CacheProviderPolicy;
            _logger = logger;
            _jobs = jobs;
        }

        public async Task<IActionResult> QueueCodeContextCacheUpdate(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            Job job = await _jobs.QueueJob(new JobCreateModel
            {
                SpecificationId = specificationId,
                JobDefinitionId = JobConstants.DefinitionNames.UpdateCodeContextJob,
                Properties = new Dictionary<string, string>
                {
                    {"specification-id", specificationId}
                },
                Trigger = new Trigger
                {
                    EntityId = specificationId,
                    EntityType = nameof(Specification),
                    Message = "Specification change requires code context update"
                }
            });
            
            return new ObjectResult(job);
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            JobParameters parameters = message;

            string parametersSpecificationId = parameters.SpecificationId;
            string cacheKey = GetCacheKey(parametersSpecificationId);

            await UpdateCache(parametersSpecificationId, cacheKey);
        }

        public async Task<IEnumerable<TypeInformation>> GetCodeContext(string specificationId)
        {
            string cacheKey = GetCacheKey(specificationId);

            if (!await IsCached(cacheKey))
            {
                return await UpdateCache(specificationId,
                    cacheKey);
            }

            return await GetFromCache(cacheKey);
        }

        private async Task<TypeInformation[]> UpdateCache(string specificationId, 
            string cacheKey)
        {
            try
            {
                TypeInformation[] codeContext = (await _codeContextBuilder.BuildCodeContextForSpecification(specificationId))
                    .ToArray();

                await _cacheResilience.ExecuteAsync(() => _cache.SetAsync(cacheKey, codeContext));
                
                LogInformation($"Updated code context cache entry for specification {specificationId}");

                return codeContext;
            }
            catch (Exception e)
            {
                LogError(e, $"Unable to update code context cache for specification id {specificationId}");

                throw;
            }
        }

        private async Task<TypeInformation[]> GetFromCache(string cacheKey)
            => await _cacheResilience.ExecuteAsync(() => _cache.GetAsync<TypeInformation[]>(cacheKey));

        private async Task<bool> IsCached(string cacheKey)
            => await _cacheResilience.ExecuteAsync(() => _cache.KeyExists<TypeInformation[]>(cacheKey));

        private async Task StartJob(string jobId)
            => await AddJobLog(jobId);

        private async Task CompleteJob(string jobId)
            => await AddJobLog(jobId, true);

        private Task<JobLog> AddJobLog(string jobId,
            bool? completedSuccessfully = null,
            string outcome = null) 
            => _jobs.AddJobLog(jobId, new JobLogUpdateModel
            {
                CompletedSuccessfully = completedSuccessfully,
                Outcome = outcome
            });

        private void LogInformation(string message) => _logger.Information(FormatMessage(message));

        private void LogError(Exception exception,
            string message)
            => _logger.Error(exception, FormatMessage(message));

        private static string FormatMessage(string message) => $"{nameof(CodeContextCache)}: {message}";

        private static string GetCacheKey(string specificationId)
        {
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));
            
            return $"{CacheKeys.CodeContext}{specificationId}";
        }

        private class JobParameters
        {
            private JobParameters(Message message)
            {
                SpecificationId = MessageProperty(message, "specification-id");
                JobId = MessageProperty(message, "jobId");
            }

            private static string MessageProperty(Message message,
                string key)
                => message?.GetUserProperty<string>(key) ?? 
                   throw new ArgumentOutOfRangeException(key, $"No message property {key}");
            
            public string SpecificationId { get; }
            
            public string JobId { get; }

            public static implicit operator JobParameters(Message message)
            {
                return new JobParameters(message);
            }
        }
    }
}