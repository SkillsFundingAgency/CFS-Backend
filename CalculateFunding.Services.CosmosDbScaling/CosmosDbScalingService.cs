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
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;
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
        private readonly AsyncPolicy _jobsApiClientPolicy;
        private readonly AsyncPolicy _scalingRepositoryPolicy;
        private readonly AsyncPolicy _cacheProviderPolicy;
        private readonly AsyncPolicy _scalingConfigRepositoryPolicy;
        private readonly ICosmosDbThrottledEventsFilter _cosmosDbThrottledEventsFilter;
        private readonly IValidator<ScalingConfigurationUpdateModel> _scalingConfigurationUpdateModelValidator;

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
            ICosmosDbThrottledEventsFilter cosmosDbThrottledEventsFilter,
            IValidator<ScalingConfigurationUpdateModel> scalingConfigurationUpdateModelValidator)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(cosmosDbScalingRepositoryProvider, nameof(cosmosDbScalingRepositoryProvider));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(cosmosDbScalingRepositoryProvider, nameof(cosmosDbScalingRepositoryProvider));
            Guard.ArgumentNotNull(cosmosDbScalingResiliencePolicies, nameof(cosmosDbScalingResiliencePolicies));
            Guard.ArgumentNotNull(cosmosDbScalingRequestModelBuilder, nameof(cosmosDbScalingRequestModelBuilder));
            Guard.ArgumentNotNull(cosmosDbThrottledEventsFilter, nameof(cosmosDbThrottledEventsFilter));
            Guard.ArgumentNotNull(scalingConfigurationUpdateModelValidator, nameof(scalingConfigurationUpdateModelValidator));
            Guard.ArgumentNotNull(cosmosDbScalingResiliencePolicies?.ScalingRepository, nameof(cosmosDbScalingResiliencePolicies.ScalingRepository));
            Guard.ArgumentNotNull(cosmosDbScalingResiliencePolicies?.CacheProvider, nameof(cosmosDbScalingResiliencePolicies.CacheProvider));
            Guard.ArgumentNotNull(cosmosDbScalingResiliencePolicies?.ScalingConfigRepository, nameof(cosmosDbScalingResiliencePolicies.ScalingConfigRepository));
            Guard.ArgumentNotNull(cosmosDbScalingResiliencePolicies?.JobsApiClient, nameof(cosmosDbScalingResiliencePolicies.JobsApiClient));

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
            _scalingConfigurationUpdateModelValidator = scalingConfigurationUpdateModelValidator;
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

            IEnumerable<string> collectionsToProcess = _cosmosDbThrottledEventsFilter.GetUniqueCosmosDBContainerNamesFromEventData(events);

            if (collectionsToProcess.IsNullOrEmpty())
            {
                return;
            }

            _logger.Information($"Found {collectionsToProcess.Count()} collections to process");

            foreach (string containerName in collectionsToProcess)
            {
                try
                {
                    CosmosCollectionType cosmosRepositoryType = containerName.GetEnumValueFromDescription<CosmosCollectionType>();

                    CosmosDbScalingCollectionSettings settings = await _scalingConfigRepositoryPolicy.ExecuteAsync(() =>
                        _cosmosDbScalingConfigRepository.GetCollectionSettingsByRepositoryType(cosmosRepositoryType));

                    int currentThroughput = settings.CurrentRequestUnits;

                    if (settings.AvailableRequestUnits == 0)
                    {
                        string errorMessage = $"The collection '{containerName}' throughput is already at the maximum of {settings.MaxRequestUnits} RU's";
                        _logger.Warning(errorMessage);
                        continue;
                    }

                    int incrementalRequestUnitsValue = scaleUpIncrementValue;

                    int increasedRequestUnits = currentThroughput + incrementalRequestUnitsValue;

                    if (incrementalRequestUnitsValue > settings.AvailableRequestUnits)
                    {
                        increasedRequestUnits = settings.MaxRequestUnits;

                        incrementalRequestUnitsValue = settings.AvailableRequestUnits;
                    }

                    settings.CurrentRequestUnits = increasedRequestUnits;

                    await ScaleCollection(cosmosRepositoryType, increasedRequestUnits, settings.MaxRequestUnits);

                    await UpdateCollectionSettings(settings, CosmosDbScalingDirection.Up, incrementalRequestUnitsValue);
                }
                catch (NonRetriableException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    string errorMessage = $"Failed to increase cosmosdb request units on collection '{containerName}'";

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

            List<string> jobDefinitionIdsStillActive = jobSummariesResponse.Content?.Select(m => m.JobType).Distinct().ToList();

            IEnumerable<CosmosDbScalingConfig> cosmosDbScalingConfigs = await GetAllConfigs();

            IList<CosmosDbScalingCollectionSettings> settingsToUpdate = new List<CosmosDbScalingCollectionSettings>();

            foreach (CosmosDbScalingConfig cosmosDbScalingConfig in cosmosDbScalingConfigs)
            {
                CosmosDbScalingCollectionSettings settings = await _scalingConfigRepositoryPolicy.ExecuteAsync(() =>
                    _cosmosDbScalingConfigRepository.GetCollectionSettingsByRepositoryType(cosmosDbScalingConfig.RepositoryType));
               
                bool jobActive = cosmosDbScalingConfig.JobRequestUnitConfigs.Any(item => jobDefinitionIdsStillActive.Contains(item.JobDefinitionId));

                bool proceed = !settingsToUpdate.Any(m => m.Id == cosmosDbScalingConfig.Id);
                
                if (proceed && !jobActive)
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
                        await ScaleCollection(settings.CosmosCollectionType, settings.MinRequestUnits, settings.MaxRequestUnits);

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
                    int previousCurrentRequestUnits = settings.CurrentRequestUnits;

                    settings.CurrentRequestUnits =
                        Math.Max(previousCurrentRequestUnits - settings.LastScalingIncrementValue, settings.MinRequestUnits);

                    int requestUnitsToDecrement = previousCurrentRequestUnits - settings.CurrentRequestUnits;

                    if (requestUnitsToDecrement <= 0)
                    {
                        continue;
                    }

                    await ScaleCollection(settings.CosmosCollectionType, settings.CurrentRequestUnits, settings.MaxRequestUnits);

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
                string errorMessage = $"A job config does not exist for job definition id {jobDefinitionId} and repo type {cosmosDbScalingConfig.RepositoryType}";

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

            await ScaleCollection(cosmosDbScalingConfig.RepositoryType, settings.CurrentRequestUnits, settings.MaxRequestUnits);

            int incrementalRequestUnitsValue = settings.CurrentRequestUnits - currentRequestUnits;

            await UpdateCollectionSettings(settings, CosmosDbScalingDirection.Up, incrementalRequestUnitsValue);
        }

        private async Task UpdateCollectionSettings(CosmosDbScalingCollectionSettings settings, CosmosDbScalingDirection direction, int requestUnits)
        {
            if (direction == CosmosDbScalingDirection.Up)
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

        public async Task ScaleCollection(CosmosCollectionType cosmosRepositoryType, int requestUnits, int maxRequestUnits)
        {
            ICosmosDbScalingRepository cosmosDbScalingRepository = _cosmosDbScalingRepositoryProvider.GetRepository(cosmosRepositoryType);

            try
            {
                //added brute force guard to prevent scaling beyond the configured max permitted in the settings for this collection

                await _scalingRepositoryPolicy.ExecuteAsync(async () => await cosmosDbScalingRepository.SetThroughput(Math.Min(requestUnits, maxRequestUnits)));

                //HACK Couldn't find a way to make a mock ThroughputResponse, without which this code stops being testable
                //The below would be more robust, but without refactoring the service along SOLID principles to remove the dependencies
                //loads of methods aren't testable without that mock.
                //https://docs.microsoft.com/en-us/dotnet/api/microsoft.azure.cosmos.throughputresponse.-ctor?view=azure-dotnet#Microsoft_Azure_Cosmos_ThroughputResponse__ctor implies MS _think_ it's testable, so hopefully...

                //ThroughputResponse throughputResponse;
                //throughputResponse = await _scalingRepositoryPolicy.ExecuteAsync(async () => await cosmosDbScalingRepository.SetThroughput(Math.Min(requestUnits, maxRequestUnits)));

                //if (throughputResponse?.StatusCode != HttpStatusCode.OK)
                //{
                //    throw new Exception($"Unable to set throughput as requested: {throughputResponse?.StatusCode.ToString() ?? "No throughput response"}");
                //}
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

        public async Task<IActionResult> SaveConfiguration(ScalingConfigurationUpdateModel scalingConfigurationUpdate)
        {
            FluentValidation.Results.ValidationResult validationResult = await _scalingConfigurationUpdateModelValidator.ValidateAsync(scalingConfigurationUpdate);

            if (!validationResult.IsValid)
            {
                return validationResult.AsBadRequest();
            }

            CosmosDbScalingCollectionSettings cosmosDbScalingCollectionSettings = await _cosmosDbScalingConfigRepository.GetCollectionSettingsByRepositoryType(scalingConfigurationUpdate.RepositoryType);

            if (cosmosDbScalingCollectionSettings == null)
            {
                cosmosDbScalingCollectionSettings = new CosmosDbScalingCollectionSettings()
                {
                    CosmosCollectionType = scalingConfigurationUpdate.RepositoryType,
                    MaxRequestUnits = scalingConfigurationUpdate.MaxRequestUnits,
                    MinRequestUnits = scalingConfigurationUpdate.BaseRequestUnits,
                };
            }
            else
            {
                cosmosDbScalingCollectionSettings.MaxRequestUnits = scalingConfigurationUpdate.MaxRequestUnits;
                cosmosDbScalingCollectionSettings.MinRequestUnits = scalingConfigurationUpdate.BaseRequestUnits;
            }

            HttpStatusCode statusCode = await _scalingConfigRepositoryPolicy.ExecuteAsync(
                        () => _cosmosDbScalingConfigRepository.UpdateCollectionSettings(cosmosDbScalingCollectionSettings));

            if (!statusCode.IsSuccess())
            {
                string errorMessage = $"Failed to Insert or Update Scaling Collection Setting for repository type: '{scalingConfigurationUpdate.RepositoryType}'  with status code: '{statusCode}'";
                _logger.Error(errorMessage);
                throw new RetriableException(errorMessage);
            }

            await SaveScalingConfig(scalingConfigurationUpdate);
            return new OkObjectResult(scalingConfigurationUpdate);
        }

        private async Task SaveScalingConfig(ScalingConfigurationUpdateModel scalingConfigurationUpdate)
        {
            CosmosDbScalingConfig cosmosDbScalingConfig = await _cosmosDbScalingConfigRepository.GetConfigByRepositoryType(scalingConfigurationUpdate.RepositoryType);
            if (cosmosDbScalingConfig == null)
            {
                cosmosDbScalingConfig = new CosmosDbScalingConfig()
                {
                    Id = Guid.NewGuid().ToString(),
                    RepositoryType = scalingConfigurationUpdate.RepositoryType,

                };
            }
            cosmosDbScalingConfig.JobRequestUnitConfigs = scalingConfigurationUpdate.JobRequestUnitConfigs;

            HttpStatusCode statusCode = await _scalingConfigRepositoryPolicy.ExecuteAsync(
                    () => _cosmosDbScalingConfigRepository.UpdateConfigSettings(cosmosDbScalingConfig));

            if (!statusCode.IsSuccess())
            {
                string errorMessage = $"Failed to Insert or Update config setting repository type: '{scalingConfigurationUpdate.RepositoryType}'  with status code: '{statusCode}'";
                _logger.Error(errorMessage);
                throw new RetriableException(errorMessage);
            }
        }
    }
}
