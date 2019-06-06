using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using CalculateFunding.Services.CosmosDbScaling.Repositories;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Polly;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace CalculateFunding.Services.CosmosDbScaling
{
    public class CosmosDbScalingService : ICosmosDbScalingService
    {
        private readonly ILogger _logger;
        private readonly ICosmosDbScalingRepositoryProvider _cosmosDbScalingRepositoryProvider;
        private readonly ICacheProvider _cacheProvider;
        private readonly ICosmosDbScalingConfigRepository _cosmosDbScalingConfigRepository;
        private readonly ICosmosDbScalingRequestModelBuilder _cosmosDbScalingRequestModelBuilder;
        private readonly IJobsApiClient _jobsApiClient;
        private readonly Policy _jobsApiClientPolicy;
        private readonly Policy _scalingRepositoryPolicy;
        private readonly Policy _cacheProviderPolicy;
        private readonly Policy _scalingConfigRepositoryPolicy;

        public CosmosDbScalingService(
            ILogger logger, 
            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider, 
            IJobsApiClient jobsApiClient,
            ICacheProvider cacheProvider,
            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository,
            ICosmosDbScallingResilliencePolicies cosmosDbScallingResilliencePolicies,
            ICosmosDbScalingRequestModelBuilder cosmosDbScalingRequestModelBuilder)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(cosmosDbScalingRepositoryProvider, nameof(cosmosDbScalingRepositoryProvider));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(cosmosDbScalingRepositoryProvider, nameof(cosmosDbScalingRepositoryProvider));
            Guard.ArgumentNotNull(cosmosDbScallingResilliencePolicies, nameof(cosmosDbScallingResilliencePolicies));
            Guard.ArgumentNotNull(cosmosDbScalingRequestModelBuilder, nameof(cosmosDbScalingRequestModelBuilder));

            _logger = logger;
            _cosmosDbScalingRepositoryProvider = cosmosDbScalingRepositoryProvider;
            _cacheProvider = cacheProvider;
            _cosmosDbScalingConfigRepository = cosmosDbScalingConfigRepository;
            _cosmosDbScalingRequestModelBuilder = cosmosDbScalingRequestModelBuilder;
            _jobsApiClient = jobsApiClient;
            _scalingRepositoryPolicy = cosmosDbScallingResilliencePolicies.ScalingRepository;
            _cacheProviderPolicy = cosmosDbScallingResilliencePolicies.CacheProvider;
            _scalingConfigRepositoryPolicy = cosmosDbScallingResilliencePolicies.ScalingConfigRepository;
            _jobsApiClientPolicy = cosmosDbScallingResilliencePolicies.JobsApiClient;
        }

        public async Task ScaleUp(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            JobNotification jobNotification = message.GetPayloadAsInstanceOf<JobNotification>();

            Guard.ArgumentNotNull(jobNotification, "Null message payload provided");

            if(jobNotification.RunningStatus == RunningStatus.Completed || jobNotification.RunningStatus == RunningStatus.InProgress)
            {
                return;
            }

            CosmosDbScalingRequestModel requestModel = _cosmosDbScalingRequestModelBuilder.BuildRequestModel(jobNotification);

            if (requestModel.RepositoryTypes.IsNullOrEmpty())
            {
                return;
            }

            foreach (CosmosRepositoryType cosmosRepositoryType in requestModel.RepositoryTypes)
            {
                try
                {
                    CosmosDbScalingConfig cosmosDbScalingConfig = await _scalingConfigRepositoryPolicy.ExecuteAsync(() => _cosmosDbScalingConfigRepository.GetConfigByRepositoryType(cosmosRepositoryType));

                    await ScaleUpCollection(cosmosDbScalingConfig, requestModel.JobDefinitionId);
                }
                catch (Exception ex)
                {
                    string errorMessage = "Failed to increase cosmosdb request units";
                    _logger.Error(ex, errorMessage);

                    throw new RetriableException(errorMessage, ex);
                }
            }

            await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<List<CosmosDbScalingConfig>>(CacheKeys.AllCosmosScalingConfigs));
        }

        public async Task ScaleDown()
        {
            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset hourAgo = now.AddHours(-1);

            ApiResponse<IEnumerable<JobSummary>> jobSummariesResponse = await _jobsApiClientPolicy.ExecuteAsync(
                () => _jobsApiClient.GetNonCompletedJobsWithinTimeFrame(hourAgo, now));

            if (!jobSummariesResponse.StatusCode.IsSuccess())
            {
                string errorMessage = "Failed to fetch job summaries that are still running within the last hour";

                _logger.Error(errorMessage);

                throw new RetriableException(errorMessage);
            }

            IEnumerable<string> jobDefinitionIdsStillActive = jobSummariesResponse.Content?.Select(m => m.JobType).Distinct();

            IEnumerable<CosmosDbScalingConfig> cosmosDbScalingConfigs = await GetAllConfigs();

            IList<CosmosDbScalingConfig> configsToUpdate = new List<CosmosDbScalingConfig>();

            foreach (CosmosDbScalingConfig cosmosDbScalingConfig in cosmosDbScalingConfigs)
            {
                bool proceed = !configsToUpdate.Any(m => m.Id == cosmosDbScalingConfig.Id);

                if (proceed)
                {
                    if(!cosmosDbScalingConfig.IsAtBaseLine)
                    {
                        cosmosDbScalingConfig.CurrentRequestUnits = cosmosDbScalingConfig.BaseRequestUnits;

                        configsToUpdate.Add(cosmosDbScalingConfig);
                    }
                }
            }

            if (!configsToUpdate.IsNullOrEmpty())
            {
                foreach(CosmosDbScalingConfig cosmosDbScalingConfig in configsToUpdate)
                {
                    try
                    {
                        await ScaleCollection(cosmosDbScalingConfig);

                        await UpdateScaleConfig(cosmosDbScalingConfig);
                    }
                    catch(Exception ex)
                    {
                        throw new RetriableException($"Failed to scale down collection for repository type '{cosmosDbScalingConfig.RepositoryType}'", ex);
                    }
                }

                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<List<CosmosDbScalingConfig>>(CacheKeys.AllCosmosScalingConfigs));
            }
        }

        private async Task ScaleUpCollection(CosmosDbScalingConfig cosmosDbScalingConfig, string jobDefinitionId)
        {
            Guard.ArgumentNotNull(cosmosDbScalingConfig, nameof(cosmosDbScalingConfig));
            Guard.IsNullOrWhiteSpace(jobDefinitionId, nameof(jobDefinitionId));

            CosmosDbScalingJobConfig cosmosDbScalingJobConfig = cosmosDbScalingConfig.JobRequestUnitConfigs
                    .FirstOrDefault(m => string.Equals(
                        m.JobDefinitionId, 
                        jobDefinitionId, 
                        StringComparison.InvariantCultureIgnoreCase));

            if(cosmosDbScalingJobConfig == null)
            {
                string errorMessage = $"A job config does not exist for job definition id {jobDefinitionId}";

                _logger.Error(errorMessage);
                throw new NonRetriableException(errorMessage);
            }

            if (cosmosDbScalingConfig.IsAtBaseLine)
            {
                cosmosDbScalingConfig.CurrentRequestUnits = cosmosDbScalingJobConfig.JobRequestUnits;
            }
            else
            {
                cosmosDbScalingConfig.CurrentRequestUnits =
                    cosmosDbScalingConfig.AvailableRequestUnits >= cosmosDbScalingJobConfig.JobRequestUnits ?
                    (cosmosDbScalingConfig.CurrentRequestUnits + cosmosDbScalingJobConfig.JobRequestUnits) :
                    cosmosDbScalingConfig.MaxRequestUnits;
            }

            await ScaleCollection(cosmosDbScalingConfig);

            await UpdateScaleConfig(cosmosDbScalingConfig);
        }

        private async Task UpdateScaleConfig(CosmosDbScalingConfig cosmosDbScalingConfig)
        {
            HttpStatusCode statusCode = await _scalingConfigRepositoryPolicy.ExecuteAsync(
               () => _cosmosDbScalingConfigRepository.UpdateCurrentRequestUnits(cosmosDbScalingConfig));

            if (!statusCode.IsSuccess())
            {
                string errorMessage = $"Failed to update cosmos scale config repository type: '{cosmosDbScalingConfig.RepositoryType}' with new request units of '{cosmosDbScalingConfig.CurrentRequestUnits}' with status code: '{statusCode}'";
                _logger.Error(errorMessage);
                throw new RetriableException(errorMessage);
            }
        }

        private async Task ScaleCollection(CosmosDbScalingConfig cosmosDbScalingConfig)
        {
            ICosmosDbScalingRepository cosmosDbScalingRepository = _cosmosDbScalingRepositoryProvider.GetRepository(cosmosDbScalingConfig.RepositoryType);

            try
            {
                await _scalingRepositoryPolicy.ExecuteAsync(() => cosmosDbScalingRepository.SetThroughput(cosmosDbScalingConfig.CurrentRequestUnits));
            }
            catch(Exception ex)
            {
                _logger.Error(ex, $"Failed to set throughput on repository type '{cosmosDbScalingConfig.RepositoryType}' with '{cosmosDbScalingConfig.CurrentRequestUnits}' request units");

                throw;
            }
        }

        private async Task<IEnumerable<CosmosDbScalingConfig>> GetAllConfigs()
        {
            IEnumerable<CosmosDbScalingConfig> configs = await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.GetAsync<List<CosmosDbScalingConfig>>(CacheKeys.AllCosmosScalingConfigs));

            if (!configs.IsNullOrEmpty())
            {
                return configs;
            }

            configs = await _scalingConfigRepositoryPolicy.ExecuteAsync(() => _cosmosDbScalingConfigRepository.GetAllConfigs());

            await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.SetAsync(CacheKeys.AllCosmosScalingConfigs, configs.ToList()));

            return configs;
        }
    }
}
