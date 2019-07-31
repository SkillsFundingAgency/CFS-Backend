using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using Microsoft.Azure.ServiceBus;
using Microsoft.Azure.EventHubs;
using Polly;
using Serilog;

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
        private readonly ICosmosDbThrottledEventsFilter _cosmosDbThrottledEventsFilter;

        private const int scaleUpIncrementValue = 10000;
        private const int previousMinutestoCheckForScaledCollections = 45;

        public CosmosDbScalingService(
            ILogger logger,
            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider,
            IJobsApiClient jobsApiClient,
            ICacheProvider cacheProvider,
            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository,
            ICosmosDbScalingResiliencePolicies cosmosDbScalingResiliencePolicies,
            ICosmosDbScalingRequestModelBuilder cosmosDbScalingRequestModelBuilder,
            ICosmosDbThrottledEventsFilter cosmosDbThrottledEventsFilter)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(cosmosDbScalingRepositoryProvider, nameof(cosmosDbScalingRepositoryProvider));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(cosmosDbScalingRepositoryProvider, nameof(cosmosDbScalingRepositoryProvider));
            Guard.ArgumentNotNull(cosmosDbScalingResiliencePolicies, nameof(cosmosDbScalingResiliencePolicies));
            Guard.ArgumentNotNull(cosmosDbScalingRequestModelBuilder, nameof(cosmosDbScalingRequestModelBuilder));
            Guard.ArgumentNotNull(cosmosDbThrottledEventsFilter, nameof(cosmosDbThrottledEventsFilter));

            _logger = logger;
            _cosmosDbScalingRepositoryProvider = cosmosDbScalingRepositoryProvider;
            _cacheProvider = cacheProvider;
            _cosmosDbScalingConfigRepository = cosmosDbScalingConfigRepository;
            _cosmosDbScalingRequestModelBuilder = cosmosDbScalingRequestModelBuilder;
            _jobsApiClient = jobsApiClient;
            _scalingRepositoryPolicy = cosmosDbScalingResiliencePolicies.ScalingRepository;
            _cacheProviderPolicy = cosmosDbScalingResiliencePolicies.CacheProvider;
            _scalingConfigRepositoryPolicy = cosmosDbScalingResiliencePolicies.ScalingConfigRepository;
            _jobsApiClientPolicy = cosmosDbScalingResiliencePolicies.JobsApiClient;
            _cosmosDbThrottledEventsFilter = cosmosDbThrottledEventsFilter;
        }

        public async Task ScaleUp(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            JobNotification jobNotification = message.GetPayloadAsInstanceOf<JobNotification>();

            Guard.ArgumentNotNull(jobNotification, "Null message payload provided");

            if (jobNotification.RunningStatus == RunningStatus.Completed || jobNotification.RunningStatus == RunningStatus.InProgress)
            {
                return;
            }

            CosmosDbScalingRequestModel requestModel = _cosmosDbScalingRequestModelBuilder.BuildRequestModel(jobNotification);

            if (requestModel.RepositoryTypes.IsNullOrEmpty())
            {
                return;
            }

            foreach (CosmosCollectionType cosmosRepositoryType in requestModel.RepositoryTypes)
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

        public async Task ScaleUp(IEnumerable<EventData> events)
        {
            Guard.ArgumentNotNull(events, nameof(events));

            IEnumerable<string> collectionsToProcess = _cosmosDbThrottledEventsFilter.GetUniqueCosmosDbCollectionNamesFromEventData(events);

            if (collectionsToProcess.IsNullOrEmpty())
            {
                return;
            }

            _logger.Information($"Found {collectionsToProcess.Count()} collections to process");

            foreach(string collectionName in collectionsToProcess)
            {
                try
                {
                    CosmosCollectionType cosmosRepositoryType = collectionName.GetEnumValueFromDescription<CosmosCollectionType>();

                    CosmosDbScalingCollectionSettings settings = await _scalingConfigRepositoryPolicy.ExecuteAsync(() =>
                        _cosmosDbScalingConfigRepository.GetCollectionSettingsByRepositoryType(cosmosRepositoryType));

                    int currentThroughput = settings.CurrentRequestUnits;

                    if(settings.AvailableRequestUnits == 0)
                    {
                        string errorMessage = $"The collection '{collectionName}' throughput is already at the maximum of {settings.MaxRequestUnits} RU's";
                        _logger.Warning(errorMessage);
                        continue;
                    }

                    int incrementalRequestUnitsValue = scaleUpIncrementValue;

                    int increasedRequestUnits = currentThroughput + incrementalRequestUnitsValue;

                    if(incrementalRequestUnitsValue > settings.AvailableRequestUnits)
                    {
                        increasedRequestUnits = settings.MaxRequestUnits;

                        incrementalRequestUnitsValue = settings.AvailableRequestUnits;
                    }

                    settings.CurrentRequestUnits = increasedRequestUnits;

                    await ScaleCollection(cosmosRepositoryType, increasedRequestUnits);

                    await UpdateCollectionSettings(settings, CosmosDbScalingDirection.Up, incrementalRequestUnitsValue);
                }
                catch(NonRetriableException)
                {
                    throw;
                }
                catch(Exception ex)
                {
                    string errorMessage = $"Failed to increase cosmosdb request units on collection '{collectionName}'";

                    _logger.Error(ex, errorMessage);

                    throw new RetriableException(errorMessage, ex);
                }
            }
        }

        public async Task ScaleDownForJobConfiguration()
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

            IList<CosmosDbScalingCollectionSettings> settingsToUpdate = new List<CosmosDbScalingCollectionSettings>();

            foreach (CosmosDbScalingConfig cosmosDbScalingConfig in cosmosDbScalingConfigs)
            {
                CosmosDbScalingCollectionSettings settings = await _scalingConfigRepositoryPolicy.ExecuteAsync(() =>
                    _cosmosDbScalingConfigRepository.GetCollectionSettingsByRepositoryType(cosmosDbScalingConfig.RepositoryType));

                bool proceed = !settingsToUpdate.Any(m => m.Id == cosmosDbScalingConfig.Id);

                if (proceed)
                {
                    if (!settings.IsAtBaseLine)
                    {
                        settingsToUpdate.Add(settings);
                    }
                }
            }

            if (!settingsToUpdate.IsNullOrEmpty())
            {
                foreach (CosmosDbScalingCollectionSettings settings in settingsToUpdate)
                { 
                    try
                    {
                        await ScaleCollection(settings.CosmosCollectionType, settings.MinRequestUnits);

                        int decrementValue = settings.CurrentRequestUnits - settings.MinRequestUnits;

                        settings.CurrentRequestUnits = settings.MinRequestUnits;

                        await UpdateCollectionSettings(settings, CosmosDbScalingDirection.Down, decrementValue);
                    }
                    catch (Exception ex)
                    {
                        throw new RetriableException($"Failed to scale down collection for repository type '{settings.CosmosCollectionType}'", ex);
                    }
                }

                await _cacheProviderPolicy.ExecuteAsync(() => _cacheProvider.RemoveAsync<List<CosmosDbScalingConfig>>(CacheKeys.AllCosmosScalingConfigs));
            }
        }

        public async Task ScaleDownIncrementally()
        {
            IList<CosmosDbScalingCollectionSettings> settingsToUpdate = new List<CosmosDbScalingCollectionSettings>();

            IEnumerable<CosmosDbScalingCollectionSettings> collectionsToProcess = await _scalingConfigRepositoryPolicy.ExecuteAsync(() =>
                    _cosmosDbScalingConfigRepository.GetCollectionSettingsIncremented(previousMinutestoCheckForScaledCollections));

            if (collectionsToProcess.IsNullOrEmpty())
            {
                return;
            }

           _logger.Information($"Found {collectionsToProcess.Count()} collections to scale down");

            foreach (CosmosDbScalingCollectionSettings settings in collectionsToProcess)
            {
                try
                {
                    int requestUnitsToDecrement;

                    if((settings.CurrentRequestUnits - settings.LastScalingIncrementValue) < settings.MinRequestUnits)
                    {
                        requestUnitsToDecrement = (settings.CurrentRequestUnits - settings.MinRequestUnits);

                        if(requestUnitsToDecrement <= 0)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        requestUnitsToDecrement = settings.LastScalingIncrementValue;
                    }

                    int requestUnits = settings.CurrentRequestUnits - requestUnitsToDecrement;

                    settings.CurrentRequestUnits = requestUnits;

                    await ScaleCollection(settings.CosmosCollectionType, requestUnits);

                    await UpdateCollectionSettings(settings, CosmosDbScalingDirection.Down, requestUnitsToDecrement);
                }
                catch (Exception ex)
                {
                    throw new RetriableException($"Failed to scale down collection for repository type '{settings.CosmosCollectionType}'", ex);
                }
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

            if (cosmosDbScalingJobConfig == null)
            {
                string errorMessage = $"A job config does not exist for job definition id {jobDefinitionId}";

                _logger.Error(errorMessage);
                throw new NonRetriableException(errorMessage);
            }

            CosmosDbScalingCollectionSettings settings = await _scalingConfigRepositoryPolicy.ExecuteAsync(() => 
                _cosmosDbScalingConfigRepository.GetCollectionSettingsByRepositoryType(cosmosDbScalingConfig.RepositoryType));

            if (settings == null)
            {
                string errorMessage = $"A collections settings file does not exist for settings collection type: '{cosmosDbScalingConfig.RepositoryType}'";

                _logger.Error(errorMessage);
                throw new RetriableException(errorMessage);
            }

            int currentRequestUnits = settings.CurrentRequestUnits;

            if (settings.IsAtBaseLine)
            {
                settings.CurrentRequestUnits = cosmosDbScalingJobConfig.JobRequestUnits;
            }
            else
            {
                settings.CurrentRequestUnits =
                    settings.AvailableRequestUnits >= cosmosDbScalingJobConfig.JobRequestUnits ?
                    (settings.CurrentRequestUnits + cosmosDbScalingJobConfig.JobRequestUnits) :
                    settings.MaxRequestUnits;
            }

            await ScaleCollection(cosmosDbScalingConfig.RepositoryType, settings.CurrentRequestUnits);

            int incrementalRequestUnitsValue = currentRequestUnits - settings.CurrentRequestUnits;

            await UpdateCollectionSettings(settings, CosmosDbScalingDirection.Up, incrementalRequestUnitsValue);
        }

        private async Task UpdateCollectionSettings(CosmosDbScalingCollectionSettings settings, CosmosDbScalingDirection direction, int requestUnits)
        {
            if(direction == CosmosDbScalingDirection.Up)
            {
                settings.LastScalingIncrementDateTime = DateTimeOffset.Now;
                settings.LastScalingIncrementValue = requestUnits;
            }
            else
            {
                settings.LastScalingDecrementDateTime = DateTimeOffset.Now;
                settings.LastScalingDecrementValue = requestUnits;
            }

            HttpStatusCode statusCode = await _scalingConfigRepositoryPolicy.ExecuteAsync(
               () => _cosmosDbScalingConfigRepository.UpdateCollectionSettings(settings));

            if (!statusCode.IsSuccess())
            {
                string errorMessage = $"Failed to update cosmos scale config repository type: '{settings.CosmosCollectionType}' with new request units of '{settings.CurrentRequestUnits}' with status code: '{statusCode}'";
                _logger.Error(errorMessage);
                throw new RetriableException(errorMessage);
            }
        }

        private async Task ScaleCollection(CosmosCollectionType cosmosRepositoryType, int requestUnits)
        {
            ICosmosDbScalingRepository cosmosDbScalingRepository = _cosmosDbScalingRepositoryProvider.GetRepository(cosmosRepositoryType);

            try
            {
                await _scalingRepositoryPolicy.ExecuteAsync(() => cosmosDbScalingRepository.SetThroughput(requestUnits));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to set throughput on repository type '{cosmosRepositoryType}' with '{requestUnits}' request units");

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
