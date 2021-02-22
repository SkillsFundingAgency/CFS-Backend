using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.JobManagement;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.CosmosDbScaling;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.CosmosDbScaling.Interfaces;
using CalculateFunding.Services.Processing;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.ServiceBus;
using Polly;
using Serilog;

namespace CalculateFunding.Services.CosmosDbScaling
{
    public class CosmosDbScalingService : ProcessingService, ICosmosDbScalingService
    {
        private readonly ILogger _logger;
        private readonly ICosmosDbScalingRepositoryProvider _cosmosDbScalingRepositoryProvider;
        private readonly ICacheProvider _cacheProvider;
        private readonly ICosmosDbScalingConfigRepository _cosmosDbScalingConfigRepository;
        private readonly ICosmosDbScalingRequestModelBuilder _cosmosDbScalingRequestModelBuilder;
        private readonly IJobManagement _jobManagement;
        private readonly AsyncPolicy _scalingRepositoryPolicy;
        private readonly AsyncPolicy _cacheProviderPolicy;
        private readonly AsyncPolicy _scalingConfigRepositoryPolicy;
        private readonly ICosmosDbThrottledEventsFilter _cosmosDbThrottledEventsFilter;
        private readonly IValidator<ScalingConfigurationUpdateModel> _scalingConfigurationUpdateModelValidator;
        private readonly ITelemetry _telemetry;
        private const int scaleUpIncrementValue = 10000;
        private const int previousMinutestoCheckForScaledCollections = 45;

        public CosmosDbScalingService(
            ILogger logger,
            ICosmosDbScalingRepositoryProvider cosmosDbScalingRepositoryProvider,
            IJobManagement jobManagement,
            ICacheProvider cacheProvider,
            ICosmosDbScalingConfigRepository cosmosDbScalingConfigRepository,
            ICosmosDbScalingResiliencePolicies cosmosDbScalingResiliencePolicies,
            ICosmosDbScalingRequestModelBuilder cosmosDbScalingRequestModelBuilder,
            ICosmosDbThrottledEventsFilter cosmosDbThrottledEventsFilter,
            IValidator<ScalingConfigurationUpdateModel> scalingConfigurationUpdateModelValidator,
            ITelemetry telemetry)
        {
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(cosmosDbScalingRepositoryProvider, nameof(cosmosDbScalingRepositoryProvider));
            Guard.ArgumentNotNull(jobManagement, nameof(jobManagement));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(cosmosDbScalingRepositoryProvider, nameof(cosmosDbScalingRepositoryProvider));
            Guard.ArgumentNotNull(cosmosDbScalingResiliencePolicies, nameof(cosmosDbScalingResiliencePolicies));
            Guard.ArgumentNotNull(cosmosDbScalingRequestModelBuilder, nameof(cosmosDbScalingRequestModelBuilder));
            Guard.ArgumentNotNull(cosmosDbThrottledEventsFilter, nameof(cosmosDbThrottledEventsFilter));
            Guard.ArgumentNotNull(scalingConfigurationUpdateModelValidator, nameof(scalingConfigurationUpdateModelValidator));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(cosmosDbScalingResiliencePolicies?.ScalingRepository, nameof(cosmosDbScalingResiliencePolicies.ScalingRepository));
            Guard.ArgumentNotNull(cosmosDbScalingResiliencePolicies?.CacheProvider, nameof(cosmosDbScalingResiliencePolicies.CacheProvider));
            Guard.ArgumentNotNull(cosmosDbScalingResiliencePolicies?.ScalingConfigRepository, nameof(cosmosDbScalingResiliencePolicies.ScalingConfigRepository));

            _logger = logger;
            _cosmosDbScalingRepositoryProvider = cosmosDbScalingRepositoryProvider;
            _cacheProvider = cacheProvider;
            _cosmosDbScalingConfigRepository = cosmosDbScalingConfigRepository;
            _cosmosDbScalingRequestModelBuilder = cosmosDbScalingRequestModelBuilder;
            _jobManagement = jobManagement;
            _scalingRepositoryPolicy = cosmosDbScalingResiliencePolicies.ScalingRepository;
            _cacheProviderPolicy = cosmosDbScalingResiliencePolicies.CacheProvider;
            _scalingConfigRepositoryPolicy = cosmosDbScalingResiliencePolicies.ScalingConfigRepository;
            _cosmosDbThrottledEventsFilter = cosmosDbThrottledEventsFilter;
            _scalingConfigurationUpdateModelValidator = scalingConfigurationUpdateModelValidator;
            _telemetry = telemetry;
        }

        public override async Task Process(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            JobSummary jobSummary = message.GetPayloadAsInstanceOf<JobSummary>();

            Guard.ArgumentNotNull(jobSummary, "Null message payload provided");

            if (jobSummary.RunningStatus == RunningStatus.Completed || jobSummary.RunningStatus == RunningStatus.InProgress)
            {
                return;
            }
            CosmosDbScalingRequestModel requestModel = _cosmosDbScalingRequestModelBuilder.BuildRequestModel(jobSummary);

            if (requestModel.RepositoryTypes.IsNullOrEmpty())
            {
                return;
            }

            foreach (CosmosCollectionType cosmosRepositoryType in requestModel.RepositoryTypes)
            {
                try
                {
                    CosmosScalingEvent cosmosScalingEvent = new CosmosScalingEvent()
                    {
                        JobId = jobSummary.JobId,
                        JobDefinitionId = jobSummary.JobType,
                        CollectionName = cosmosRepositoryType.GetDescription()
                    };

                    CosmosDbScalingConfig cosmosDbScalingConfig = await _scalingConfigRepositoryPolicy.ExecuteAsync(() => _cosmosDbScalingConfigRepository.GetConfigByRepositoryType(cosmosRepositoryType));

                    await ScaleUpCollection(cosmosDbScalingConfig, requestModel.JobDefinitionId, cosmosScalingEvent);
                    LogCosmosScalingEvent(cosmosScalingEvent);
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

                    CosmosDbScalingCollectionSettings currentSettings = settings.DeepCopy();

                    int currentThroughput = settings.CurrentRequestUnits;

                    if (settings.AvailableRequestUnits == 0)
                    {
                        string errorMessage = $"The collection '{containerName}' throughput is already at the maximum of {settings.MaxRequestUnits} RU's";
                        _logger.Warning(errorMessage);
                        continue;
                    }

                    CosmosScalingEvent cosmosScalingEvent = new CosmosScalingEvent()
                    {
                        CollectionName = cosmosRepositoryType.GetDescription(),
                        PreviousScaleValue = currentThroughput
                    };

                    int incrementalRequestUnitsValue = scaleUpIncrementValue;

                    int increasedRequestUnits = currentThroughput + incrementalRequestUnitsValue;

                    if (incrementalRequestUnitsValue > settings.AvailableRequestUnits)
                    {
                        increasedRequestUnits = settings.MaxRequestUnits;

                        incrementalRequestUnitsValue = settings.AvailableRequestUnits;
                    }

                    settings.CurrentRequestUnits = await ScaleCollection(cosmosRepositoryType, increasedRequestUnits, settings.MaxRequestUnits);

                    cosmosScalingEvent.ScaleEvent = CosmosDbScalingType.Incremental.ToString();
                    cosmosScalingEvent.ScaleValue = settings.CurrentRequestUnits;
                    cosmosScalingEvent.Direction = CosmosDbScalingDirection.Up.ToString();

                    await UpdateCollectionSettings(currentSettings, settings, CosmosDbScalingDirection.Up, incrementalRequestUnitsValue);
                    LogCosmosScalingEvent(cosmosScalingEvent);
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
            DateTimeOffset windowOfTime = now.AddHours(-2);

            IEnumerable<JobSummary> jobSummaries = await _jobManagement.GetNonCompletedJobsWithinTimeFrame(windowOfTime, now);

            if (jobSummaries == null)
            {
                string errorMessage = "Failed to fetch job summaries that are still running within the last hour";

                _logger.Error(errorMessage);

                throw new RetriableException(errorMessage);
            }

            List<string> jobDefinitionIdsStillActive = jobSummaries.Select(m => m.JobType).Distinct().ToList();

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
                        CosmosDbScalingCollectionSettings currentSettings = settings.DeepCopy();
                        CosmosScalingEvent cosmosScalingEvent = new CosmosScalingEvent()
                        {
                            CollectionName = settings.CosmosCollectionType.GetDescription(),
                            PreviousScaleValue = settings.CurrentRequestUnits,
                        };

                        int? minimumRequestUnitsAllowed = await GetMinimumThroughput(settings.CosmosCollectionType);

                        if (minimumRequestUnitsAllowed.HasValue && settings.MinRequestUnits < minimumRequestUnitsAllowed.Value)
                        {
                            settings.MinRequestUnits = minimumRequestUnitsAllowed.Value;
                        }

                        settings.CurrentRequestUnits = await ScaleCollection(settings.CosmosCollectionType, settings.MinRequestUnits, settings.MaxRequestUnits);

                        cosmosScalingEvent.ScaleEvent = CosmosDbScalingType.Job.ToString();
                        cosmosScalingEvent.ScaleValue = settings.CurrentRequestUnits;
                        cosmosScalingEvent.Direction = CosmosDbScalingDirection.Down.ToString();

                        await UpdateCollectionSettings(currentSettings, settings, CosmosDbScalingDirection.Down, CosmosDbScalingType.Job);
                        LogCosmosScalingEvent(cosmosScalingEvent);
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
                    CosmosDbScalingCollectionSettings currentSettings = settings.DeepCopy();
                    CosmosScalingEvent cosmosScalingEvent = new CosmosScalingEvent()
                    {
                        CollectionName = settings.CosmosCollectionType.GetDescription(),
                        PreviousScaleValue = settings.CurrentRequestUnits,
                    };

                    int previousCurrentRequestUnits = settings.CurrentRequestUnits;

                    settings.CurrentRequestUnits = Math.Max(previousCurrentRequestUnits - settings.LastScalingIncrementValue, settings.MinRequestUnits);

                    int? minimumRequestUnitsAllowed = await GetMinimumThroughput(settings.CosmosCollectionType);

                    if (minimumRequestUnitsAllowed.HasValue && settings.CurrentRequestUnits < minimumRequestUnitsAllowed.Value)
                    {
                        settings.CurrentRequestUnits = minimumRequestUnitsAllowed.Value;
                    }

                    int requestUnitsToDecrement = previousCurrentRequestUnits - settings.CurrentRequestUnits;

                    if (requestUnitsToDecrement <= 0)
                    {
                        continue;
                    }

                    settings.CurrentRequestUnits = await ScaleCollection(settings.CosmosCollectionType, settings.CurrentRequestUnits, settings.MaxRequestUnits);

                    cosmosScalingEvent.ScaleEvent = CosmosDbScalingType.Incremental.ToString();
                    cosmosScalingEvent.ScaleValue = settings.CurrentRequestUnits;
                    cosmosScalingEvent.Direction = CosmosDbScalingDirection.Down.ToString();
                    await UpdateCollectionSettings(currentSettings, settings, CosmosDbScalingDirection.Down, requestUnitsToDecrement);
                    LogCosmosScalingEvent(cosmosScalingEvent);
                }
                catch (Exception ex)
                {
                    throw new RetriableException($"Failed to scale down collection for repository type '{settings.CosmosCollectionType}'", ex);
                }
            }

        }

        private async Task ScaleUpCollection(CosmosDbScalingConfig cosmosDbScalingConfig, string jobDefinitionId, CosmosScalingEvent cosmosScalingEvent)
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

            CosmosDbScalingCollectionSettings currentSettings = settings.DeepCopy();

            if (settings == null)
            {
                string errorMessage = $"A collections settings file does not exist for settings collection type: '{cosmosDbScalingConfig.RepositoryType}'";

                _logger.Error(errorMessage);
                throw new RetriableException(errorMessage);
            }

            cosmosScalingEvent.PreviousScaleValue = settings.CurrentRequestUnits;

            settings.CurrentRequestUnits =
                    settings.AvailableRequestUnits >= cosmosDbScalingJobConfig.JobRequestUnits ?
                        (settings.CurrentRequestUnits + cosmosDbScalingJobConfig.JobRequestUnits) :
                        settings.MaxRequestUnits;

            settings.CurrentRequestUnits = await ScaleCollection(cosmosDbScalingConfig.RepositoryType, settings.CurrentRequestUnits, settings.MaxRequestUnits);
            cosmosScalingEvent.ScaleValue = settings.CurrentRequestUnits;
            cosmosScalingEvent.Direction = CosmosDbScalingDirection.Up.ToString();
            cosmosScalingEvent.ScaleEvent = CosmosDbScalingType.Job.ToString();

            await UpdateCollectionSettings(currentSettings, settings, CosmosDbScalingDirection.Up, CosmosDbScalingType.Job);
        }

        private async Task UpdateCollectionSettings(CosmosDbScalingCollectionSettings currentSettings, CosmosDbScalingCollectionSettings settings, CosmosDbScalingDirection direction, int requestUnits)
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

            await UpdateCollectionSettings(currentSettings, settings, direction, CosmosDbScalingType.Incremental);
        }

        private async Task UpdateCollectionSettings(CosmosDbScalingCollectionSettings currentSettings, CosmosDbScalingCollectionSettings settings, CosmosDbScalingDirection direction, CosmosDbScalingType type)
        {
            _logger.Information($"Current settings: {currentSettings.AsJson()} has been scaled with settings: {settings.AsJson()} scaling direction: {direction} and type: {type}");

            HttpStatusCode statusCode = await _scalingConfigRepositoryPolicy.ExecuteAsync(
               () => _cosmosDbScalingConfigRepository.UpdateCollectionSettings(settings));

            if (!statusCode.IsSuccess())
            {
                string errorMessage = $"Failed to update cosmos scale config repository type: '{settings.CosmosCollectionType}' with new request units of '{settings.CurrentRequestUnits}' with status code: '{statusCode}'";
                _logger.Error(errorMessage);
                throw new RetriableException(errorMessage);
            }
        }

        public async Task<int> ScaleCollection(CosmosCollectionType cosmosRepositoryType, int requestUnits, int maxRequestUnits)
        {
            ICosmosDbScalingRepository cosmosDbScalingRepository = _cosmosDbScalingRepositoryProvider.GetRepository(cosmosRepositoryType);

            int throughPutRequestUnits = Math.Min(requestUnits, maxRequestUnits);

            try
            {
                await _scalingRepositoryPolicy.ExecuteAsync(async () => await cosmosDbScalingRepository.SetThroughput(throughPutRequestUnits));

                return throughPutRequestUnits;
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

        private async Task<int?> GetMinimumThroughput(CosmosCollectionType collectionType)
        {
            ICosmosDbScalingRepository cosmosDbScalingRepository = _cosmosDbScalingRepositoryProvider.GetRepository(collectionType);
            return await _scalingRepositoryPolicy.ExecuteAsync(async () => await cosmosDbScalingRepository.GetMinimumThroughput());
        }

        private void LogCosmosScalingEvent(CosmosScalingEvent cosmosScalingEvent)
        {
            _telemetry.TrackEvent("CosmosScalingCompleted",
                    new Dictionary<string, string>()
                    {
                        { "collectionName" , cosmosScalingEvent.CollectionName },
                        { "scaleEvent" , cosmosScalingEvent.ScaleEvent },
                        { "direction" , cosmosScalingEvent.Direction },
                        { "jobDefinitionId" , cosmosScalingEvent.JobDefinitionId ?? "NA" },
                        { "jobId" , cosmosScalingEvent.JobId ?? "NA" },
                    },
                    new Dictionary<string, double>()
                    {
                        {"scaleValue", cosmosScalingEvent.ScaleValue },
                        {"previousScaleValue", cosmosScalingEvent.PreviousScaleValue },
                        {"scaleDifference", cosmosScalingEvent.ScaleDifference },
                    }
                );
        }

        private class CosmosScalingEvent
        {
            public string CollectionName { get; set; }
            public string ScaleEvent { get; set; }
            public string Direction { get; set; }
            public string JobDefinitionId { get; set; }
            public string JobId { get; set; }
            public int ScaleValue { get; set; }
            public int PreviousScaleValue { get; set; }
            public int ScaleDifference => ScaleValue - PreviousScaleValue;
        }
    }
}
