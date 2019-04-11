using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Jobs;
using CalculateFunding.Common.ApiClient.Jobs.Models;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Profiling;
using CalculateFunding.Common.ApiClient.Profiling.Models;
using CalculateFunding.Common.Caching;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Providers;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Messages;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.Repositories;
using CalculateFunding.Services.Results.ResultModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Serilog;
using Reference = CalculateFunding.Common.Models.Reference;

namespace CalculateFunding.Services.Results
{
    public class PublishedResultsService : IPublishedResultsService, IHealthChecker
    {
        private readonly ILogger _logger;
        private readonly ITelemetry _telemetry;
        private readonly ICalculationResultsRepository _resultsRepository;
        private readonly IMapper _mapper;
        private readonly Polly.Policy _resultsRepositoryPolicy;
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly Polly.Policy _specificationsRepositoryPolicy;
        private readonly IPublishedProviderResultsAssemblerService _publishedProviderResultsAssemblerService;
        private readonly IPublishedProviderResultsRepository _publishedProviderResultsRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISearchRepository<AllocationNotificationFeedIndex> _allocationNotificationsSearchRepository;
        private readonly Polly.Policy _allocationNotificationsSearchRepositoryPolicy;
        private readonly IProfilingApiClient _providerProfilingRepository;
        private readonly Polly.Policy _providerProfilingRepositoryPolicy;
        private readonly Polly.Policy _publishedProviderCalculationResultsRepositoryPolicy;
        private readonly Polly.Policy _publishedProviderResultsRepositoryPolicy;
        private readonly Polly.Policy _jobsApiClientPolicy;
        private readonly IMessengerService _messengerService;
        private readonly IVersionRepository<PublishedAllocationLineResultVersion> _publishedProviderResultsVersionRepository;
        private readonly IPublishedAllocationLineLogicalResultVersionService _publishedAllocationLineLogicalResultVersionService;
        private readonly IFeatureToggle _featureToggle;
        private readonly IJobsApiClient _jobsApiClient;
        private readonly IPublishedProviderResultsSettings _publishedProviderResultsSettings;
        private readonly IProviderChangesRepository _providerChangesRepository;
        private readonly Polly.Policy _providerChangesRepositoryPolicy;
        private readonly IProviderVariationsService _providerVariationsService;
        private readonly IProviderVariationsStorageRepository _providerVariationsStorageRepository;

        // This constructor is used from the Results API project
        public PublishedResultsService(
          ILogger logger,
          IMapper mapper,
          ITelemetry telemetry,
          ICalculationResultsRepository resultsRepository,
          ISpecificationsRepository specificationsRepository,
          IResultsResilliencePolicies resiliencePolicies,
          IPublishedProviderResultsAssemblerService publishedProviderResultsAssemblerService,
          IPublishedProviderResultsRepository publishedProviderResultsRepository,
          ICacheProvider cacheProvider,
          ISearchRepository<AllocationNotificationFeedIndex> allocationNotificationsSearchRepository,
          IMessengerService messengerService,
          IVersionRepository<PublishedAllocationLineResultVersion> publishedProviderResultsVersionRepository,
          IPublishedAllocationLineLogicalResultVersionService publishedAllocationLineLogicalResultVersionService,
          IFeatureToggle featureToggle,
          IJobsApiClient jobsApiClient,
          IPublishedProviderResultsSettings publishedProviderResultsSettings,
          IProviderChangesRepository providerChangesRepository)
        {
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(publishedProviderResultsAssemblerService, nameof(publishedProviderResultsAssemblerService));
            Guard.ArgumentNotNull(publishedProviderResultsRepository, nameof(publishedProviderResultsRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(allocationNotificationsSearchRepository, nameof(allocationNotificationsSearchRepository));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(publishedProviderResultsVersionRepository, nameof(publishedProviderResultsVersionRepository));
            Guard.ArgumentNotNull(publishedAllocationLineLogicalResultVersionService, nameof(publishedAllocationLineLogicalResultVersionService));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(publishedProviderResultsSettings, nameof(publishedProviderResultsSettings));
            Guard.ArgumentNotNull(providerChangesRepository, nameof(providerChangesRepository));

            _logger = logger;
            _mapper = mapper;
            _specificationsRepositoryPolicy = resiliencePolicies.SpecificationsRepository;
            _resultsRepository = resultsRepository;
            _telemetry = telemetry;
            _resultsRepositoryPolicy = resiliencePolicies.ResultsRepository;
            _specificationsRepository = specificationsRepository;
            _publishedProviderResultsAssemblerService = publishedProviderResultsAssemblerService;
            _publishedProviderResultsRepository = publishedProviderResultsRepository;
            _cacheProvider = cacheProvider;
            _allocationNotificationsSearchRepository = allocationNotificationsSearchRepository;
            _allocationNotificationsSearchRepositoryPolicy = resiliencePolicies.AllocationNotificationFeedSearchRepository;
            _publishedProviderCalculationResultsRepositoryPolicy = resiliencePolicies.PublishedProviderCalculationResultsRepository;
            _publishedProviderResultsRepositoryPolicy = resiliencePolicies.PublishedProviderResultsRepository;
            _messengerService = messengerService;
            _publishedProviderResultsVersionRepository = publishedProviderResultsVersionRepository;
            _publishedAllocationLineLogicalResultVersionService = publishedAllocationLineLogicalResultVersionService;
            _featureToggle = featureToggle;
            _jobsApiClient = jobsApiClient;
            _jobsApiClientPolicy = resiliencePolicies.JobsApiClient;
            _publishedProviderResultsSettings = publishedProviderResultsSettings;
            _providerChangesRepository = providerChangesRepository;
            _providerChangesRepositoryPolicy = resiliencePolicies.ProviderChangesRepository;
        }

        // This constructor is used from the Results Function project
        public PublishedResultsService(
            ILogger logger,
            IMapper mapper,
            ITelemetry telemetry,
            ICalculationResultsRepository resultsRepository,
            ISpecificationsRepository specificationsRepository,
            IResultsResilliencePolicies resiliencePolicies,
            IPublishedProviderResultsAssemblerService publishedProviderResultsAssemblerService,
            IPublishedProviderResultsRepository publishedProviderResultsRepository,
            ICacheProvider cacheProvider,
            ISearchRepository<AllocationNotificationFeedIndex> allocationNotificationsSearchRepository,
            IProfilingApiClient providerProfilingRepository,
            IMessengerService messengerService,
            IVersionRepository<PublishedAllocationLineResultVersion> publishedProviderResultsVersionRepository,
            IPublishedAllocationLineLogicalResultVersionService publishedAllocationLineLogicalResultVersionService,
            IFeatureToggle featureToggle,
            IJobsApiClient jobsApiClient,
            IPublishedProviderResultsSettings publishedProviderResultsSettings,
            IProviderChangesRepository providerChangesRepository,
            IProviderVariationsService providerVariationsService,
            IProviderVariationsStorageRepository providerVariationsStorageRepository)
        {
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(publishedProviderResultsAssemblerService, nameof(publishedProviderResultsAssemblerService));
            Guard.ArgumentNotNull(publishedProviderResultsRepository, nameof(publishedProviderResultsRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(allocationNotificationsSearchRepository, nameof(allocationNotificationsSearchRepository));
            Guard.ArgumentNotNull(providerProfilingRepository, nameof(providerProfilingRepository));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(publishedProviderResultsVersionRepository, nameof(publishedProviderResultsVersionRepository));
            Guard.ArgumentNotNull(publishedAllocationLineLogicalResultVersionService, nameof(publishedAllocationLineLogicalResultVersionService));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));
            Guard.ArgumentNotNull(jobsApiClient, nameof(jobsApiClient));
            Guard.ArgumentNotNull(publishedProviderResultsSettings, nameof(publishedProviderResultsSettings));
            Guard.ArgumentNotNull(providerVariationsService, nameof(providerVariationsService));
            Guard.ArgumentNotNull(providerVariationsStorageRepository, nameof(providerVariationsStorageRepository));
            Guard.ArgumentNotNull(providerChangesRepository, nameof(providerChangesRepository));

            _logger = logger;
            _mapper = mapper;
            _telemetry = telemetry;
            _resultsRepository = resultsRepository;
            _resultsRepositoryPolicy = resiliencePolicies.ResultsRepository;
            _specificationsRepository = specificationsRepository;
            _specificationsRepositoryPolicy = resiliencePolicies.SpecificationsRepository;
            _publishedProviderResultsAssemblerService = publishedProviderResultsAssemblerService;
            _publishedProviderResultsRepository = publishedProviderResultsRepository;
            _cacheProvider = cacheProvider;
            _allocationNotificationsSearchRepository = allocationNotificationsSearchRepository;
            _allocationNotificationsSearchRepositoryPolicy = resiliencePolicies.AllocationNotificationFeedSearchRepository;
            _publishedProviderCalculationResultsRepositoryPolicy = resiliencePolicies.PublishedProviderCalculationResultsRepository;
            _publishedProviderResultsRepositoryPolicy = resiliencePolicies.PublishedProviderResultsRepository;
            _providerProfilingRepositoryPolicy = resiliencePolicies.ProviderProfilingRepository;
            _providerProfilingRepository = providerProfilingRepository;
            _messengerService = messengerService;
            _publishedProviderResultsVersionRepository = publishedProviderResultsVersionRepository;
            _publishedAllocationLineLogicalResultVersionService = publishedAllocationLineLogicalResultVersionService;
            _featureToggle = featureToggle;
            _jobsApiClient = jobsApiClient;
            _jobsApiClientPolicy = resiliencePolicies.JobsApiClient;
            _publishedProviderResultsSettings = publishedProviderResultsSettings;
            _providerChangesRepository = providerChangesRepository;
            _providerChangesRepositoryPolicy = resiliencePolicies.ProviderChangesRepository;
            _providerVariationsService = providerVariationsService;
            _providerVariationsStorageRepository = providerVariationsStorageRepository;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            ServiceHealth providerRepoHealth = await ((IHealthChecker)_publishedProviderResultsRepository).IsHealthOk();
            ServiceHealth providerChangesRepoHealth = await ((IHealthChecker)_providerChangesRepository).IsHealthOk();
            (bool Ok, string Message) cacheHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ResultsService)
            };

            health.Dependencies.AddRange(providerRepoHealth.Dependencies);
            health.Dependencies.AddRange(providerChangesRepoHealth.Dependencies);
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheHealth.Message });

            return health;
        }

        public async Task<PublishedProviderResult> GetPublishedProviderResultByAllocationResultId(string allocationResultId, int? version = null)
        {
            Guard.IsNullOrWhiteSpace(allocationResultId, nameof(allocationResultId));

            PublishedProviderResult publishedProviderResult = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultForIdInPublishedState(allocationResultId));

            if (publishedProviderResult == null)
            {
                return null;
            }

            if (version.HasValue && version.Value != publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Version)
            {

                PublishedAllocationLineResultVersion resultVersion = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsVersionRepository.GetVersion(allocationResultId, version.Value));

                if (resultVersion == null)
                {
                    return null;
                }

                publishedProviderResult.FundingStreamResult.AllocationLineResult.Current = resultVersion;
            }

            return publishedProviderResult;
        }

        public async Task<PublishedProviderResultWithHistory> GetPublishedProviderResultWithHistoryByAllocationResultId(string allocationResultId)
        {
            Guard.IsNullOrWhiteSpace(allocationResultId, nameof(allocationResultId));

            PublishedProviderResult publishedProviderResult = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultForIdInPublishedState(allocationResultId));

            if (publishedProviderResult == null)
            {
                return null;
            }

            IEnumerable<PublishedAllocationLineResultVersion> history = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsVersionRepository.GetVersions(allocationResultId, publishedProviderResult.ProviderId));

            if (history.IsNullOrEmpty())
            {
                return null;
            }

            return new PublishedProviderResultWithHistory
            {
                PublishedProviderResult = publishedProviderResult,
                History = history
            };
        }

        public PublishedAllocationLineResultVersion GetPublishedProviderResultVersionById(string id)
        {
            Guard.IsNullOrWhiteSpace(id, nameof(id));

            PublishedAllocationLineResultVersion publishedProviderResultVersion = _publishedProviderResultsRepository.GetPublishedProviderResultVersionForFeedIndexId(id);

            if (publishedProviderResultVersion == null)
            {
                return null;
            }

            return publishedProviderResultVersion;
        }

        public PublishedProviderResult GetPublishedProviderResultByVersionId(string id)
        {
            PublishedAllocationLineResultVersion version = GetPublishedProviderResultVersionById(id);

            if (version == null)
            {
                return null;
            };

            string entityId = version.EntityId;

            PublishedProviderResult publishedProviderResult = _publishedProviderResultsRepository.GetPublishedProviderResultForId(entityId);

            if (publishedProviderResult == null)
            {
                return null;
            }

            publishedProviderResult.FundingStreamResult.AllocationLineResult.Current = version;

            return publishedProviderResult;
        }

        public async Task PublishProviderResults(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            string jobId = message.UserProperties["jobId"].ToString();

            if (await RetrieveJobAndCheckCanBeProcessed(jobId) == null)
            {
                return;
            }

            if (!message.UserProperties.ContainsKey("specification-id"))
            {
                _logger.Error("No specification Id was provided to PublishProviderResults");
                await UpdateJobStatus(jobId, 100, false, "Failed to Process - no specification id provided");
                return;
            }

            int calculationProgress = 0;

            string specificationId = message.UserProperties["specification-id"].ToString();
            UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.InProgress);
            await UpdateJobStatus(jobId, calculationProgress, null);

            Stopwatch getSpecificationStopwatch = Stopwatch.StartNew();
            SpecificationCurrentVersion specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetCurrentSpecificationById(specificationId));
            getSpecificationStopwatch.Stop();

            UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);
            await UpdateJobStatus(jobId, calculationProgress, null);

            if (specification == null)
            {
                _logger.Error($"Specification not found for specification id {specificationId}");
                UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error, "specification not found");
                await UpdateJobStatus(jobId, 100, false, "Failed to process - specification not found");
                return;
            }

            Stopwatch getCalculationResultsStopwatch = Stopwatch.StartNew();
            IEnumerable<ProviderResult> providerResults = await GetProviderResultsBySpecificationId(specificationId);
            getCalculationResultsStopwatch.Stop();
            UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);
            await UpdateJobStatus(jobId, calculationProgress, null);

            if (providerResults.IsNullOrEmpty())
            {
                _logger.Error($"Provider results not found for specification id {specificationId}");
                UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error, "Could not find any provider results");
                await UpdateJobStatus(jobId, 100, false, "Failed to process - could not find any provider results");
                return;
            }

            Reference author = message.GetUserDetails();

            Stopwatch assemblePublishedProviderResultsStopwatch = Stopwatch.StartNew();
            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsAssemblerService.AssemblePublishedProviderResults(providerResults, author, specification);
            assemblePublishedProviderResultsStopwatch.Stop();
            UpdateCacheForSegmentDone(specificationId, calculationProgress += 18, CalculationProgressStatus.InProgress);
            await UpdateJobStatus(jobId, calculationProgress, null);

            Stopwatch existingPublishedProviderResultsStopwatch = Stopwatch.StartNew();
            IEnumerable<PublishedProviderResultExisting> existingPublishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetExistingPublishedProviderResultsForSpecificationId(specificationId));
            existingPublishedProviderResultsStopwatch.Stop();

            Stopwatch assembleSaveAndExcludeStopwatch = Stopwatch.StartNew();
            (IEnumerable<PublishedProviderResult> newOrUpdatedPublishedProviderResults, IEnumerable<PublishedProviderResultExisting> existingRecordsToZero) =
                await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsAssemblerService.GeneratePublishedProviderResultsToSave(publishedProviderResults, existingPublishedProviderResults));
            assembleSaveAndExcludeStopwatch.Stop();

            if (newOrUpdatedPublishedProviderResults == null)
            {
                newOrUpdatedPublishedProviderResults = Enumerable.Empty<PublishedProviderResult>();
            }

            // When the assembly doesn't return an allocation line result for a provider and it already exists, set to value to 0 and update the status to Updated
            Stopwatch existingRecordsToZeroStopwatch = Stopwatch.StartNew();
            IEnumerable<PublishedProviderResult> recordsToZero;
            if (existingRecordsToZero.AnyWithNullCheck())
            {
                recordsToZero = await FetchAndCheckExistingRecordsToZero(existingRecordsToZero);
            }
            else
            {
                recordsToZero = Enumerable.Empty<PublishedProviderResult>();
            }
            existingPublishedProviderResultsStopwatch.Stop();

            List<PublishedProviderResult> publishedProviderResultsToSave = new List<PublishedProviderResult>(newOrUpdatedPublishedProviderResults.Count() + recordsToZero.Count());
            if (newOrUpdatedPublishedProviderResults.AnyWithNullCheck())
            {
                publishedProviderResultsToSave.AddRange(newOrUpdatedPublishedProviderResults);
            }
            if (recordsToZero.AnyWithNullCheck())
            {
                publishedProviderResultsToSave.AddRange(recordsToZero);
            }

            Stopwatch savePublishedResultsStopwatch = new Stopwatch();
            Stopwatch savePublishedResultsHistoryStopwatch = new Stopwatch();
            Stopwatch savePublishedResultsSearchStopwatch = new Stopwatch();
            Stopwatch sendToProfilingStopwatch = new Stopwatch();

            DateTimeOffset publishedResultsRefreshedAt = DateTimeOffset.Now;

            if (publishedProviderResultsToSave.Any())
            {
                publishedProviderResultsToSave.ForEach(m =>
                {
                    // Set versioning
                    _publishedAllocationLineLogicalResultVersionService.SetVersion(m.FundingStreamResult.AllocationLineResult.Current);

                    // Set feed index ID for search
                    SetFeedIndexId(m);

                    // Set job ID and correlation ID for current operation
                    m.FundingStreamResult.AllocationLineResult.Current.JobId = jobId;
                    m.FundingStreamResult.AllocationLineResult.Current.CorrelationId = jobId;
                });

                try
                {
                    savePublishedResultsStopwatch.Start();
                    await _publishedProviderResultsRepository.SavePublishedResults(publishedProviderResultsToSave);
                    savePublishedResultsStopwatch.Stop();
                    UpdateCacheForSegmentDone(specificationId, calculationProgress += 15, CalculationProgressStatus.InProgress);
                    await UpdateJobStatus(jobId, calculationProgress, null);

                    savePublishedResultsHistoryStopwatch.Start();
                    await SavePublishedAllocationLineResultVersionHistory(publishedProviderResultsToSave);
                    savePublishedResultsHistoryStopwatch.Stop();
                    UpdateCacheForSegmentDone(specificationId, calculationProgress += 15, CalculationProgressStatus.InProgress);
                    await UpdateJobStatus(jobId, calculationProgress, null);

                    savePublishedResultsSearchStopwatch.Start();
                    await UpdateAllocationNotificationsFeedIndex(publishedProviderResultsToSave, specification);
                    savePublishedResultsSearchStopwatch.Stop();
                    UpdateCacheForSegmentDone(specificationId, calculationProgress += 15, CalculationProgressStatus.InProgress);
                    await UpdateJobStatus(jobId, calculationProgress, null);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to create published provider results for specification: {specificationId}");
                    UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error, "Failed to create published provider results");
                    throw new Exception($"Failed to create published provider results for specification: {specificationId}", ex);
                }
            }

            bool canCompleteJob = true;

            if (publishedProviderResultsToSave.AnyWithNullCheck())
            {
                // Queue all published provider results to be profiled
                sendToProfilingStopwatch.Start();
                await GenerateProfilingPeriods(message, publishedProviderResultsToSave, specification.Id, jobId);
                sendToProfilingStopwatch.Stop();

                canCompleteJob = false;
            }

            UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);
            await UpdateJobStatus(jobId, calculationProgress, null);

            // Update the specification to store when this refresh happened
            HttpStatusCode updatePublishedRefreshedDateStatus = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.UpdatePublishedRefreshedDate(specification.Id, publishedResultsRefreshedAt));
            if (updatePublishedRefreshedDateStatus.IsSuccess())
            {
                _logger.Information($"Updated the published refresh date on the specification with id: {specificationId}");
            }
            else
            {
                _logger.Error($"Failed to update the published refresh date on the specification with id: {specificationId}. Failed with code: {updatePublishedRefreshedDateStatus}");
            }

            UpdateCacheForSegmentDone(specificationId, 100, CalculationProgressStatus.Finished, publishedResultsRefreshedAt: publishedResultsRefreshedAt, publishedProviderResults: publishedProviderResultsToSave);

            if (canCompleteJob)
            {
                await UpdateJobStatus(jobId, 100, true, "Published Provider Results Updated");
            }

            IDictionary<string, double> metrics = new Dictionary<string, double>()
            {
                { "publishproviderresults-getCalculationResultsMs", getCalculationResultsStopwatch.ElapsedMilliseconds },
                { "publishproviderresults-getSpecificationMs", getSpecificationStopwatch.ElapsedMilliseconds },
                { "publishproviderresults-assemblePublishedProviderResultsMs", assemblePublishedProviderResultsStopwatch.ElapsedMilliseconds },
                { "publishproviderresults-existingPublishedProviderResultsMs", existingPublishedProviderResultsStopwatch.ElapsedMilliseconds },
                { "publishproviderresults-assembleSaveAndExcludeMs", assembleSaveAndExcludeStopwatch.ElapsedMilliseconds },
                { "publishproviderresults-savePublishedResultsCount", publishedProviderResultsToSave.Count },
            };

            if (publishedProviderResultsToSave.Any())
            {
                metrics.Add("publishproviderresults-savePublishedResultsMs", savePublishedResultsStopwatch.ElapsedMilliseconds);
                metrics.Add("publishproviderresults-savePublishedResultsHistoryMs", savePublishedResultsHistoryStopwatch.ElapsedMilliseconds);
                metrics.Add("publishproviderresults-savePublishedResultsSearchMs", savePublishedResultsSearchStopwatch.ElapsedMilliseconds);
                metrics.Add("publishproviderresults-sendToProfilingMs", sendToProfilingStopwatch.ElapsedMilliseconds);
            }

            if (existingRecordsToZero.AnyWithNullCheck())
            {
                metrics.Add("publishproviderresults-existingRecordsToZeroMs", existingRecordsToZeroStopwatch.ElapsedMilliseconds);
                metrics.Add("publishproviderresults-existingRecordsToZeroTotal", existingRecordsToZero.Count());
            }

            _telemetry.TrackEvent("PublishProviderResults",
                new Dictionary<string, string>()
                {
                    { "specificationId" , specificationId },
                },
                metrics
            );
        }

        public async Task PublishProviderResultsWithVariations(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            if (!message.UserProperties.ContainsKey("jobId"))
            {
                throw new NonRetriableException("No job id was provided to the PublishProviderResultsWithVariations");
            }

            string jobId = message.UserProperties["jobId"].ToString();

            JobViewModel triggeringJob = await RetrieveJobAndCheckCanBeProcessed(jobId);
            if (triggeringJob == null)
            {
                return;
            }

            if (!message.UserProperties.ContainsKey("specification-id"))
            {
                _logger.Error("No specification Id was provided to PublishProviderResultsWithVariations");
                await UpdateJobStatus(jobId, 100, false, "Failed to Process - no specification id provided");
                return;
            }

            int calculationProgress = 0;

            string specificationId = message.UserProperties["specification-id"].ToString();
            UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.InProgress);
            await UpdateJobStatus(jobId, calculationProgress, null);

            // Fetch Specification
            Stopwatch getSpecificationStopwatch = Stopwatch.StartNew();
            SpecificationCurrentVersion specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetCurrentSpecificationById(specificationId));
            getSpecificationStopwatch.Stop();

            UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);
            await UpdateJobStatus(jobId, calculationProgress, null);

            if (specification == null)
            {
                _logger.Error($"Specification not found for specification id {specificationId}");
                UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error, "specification not found");
                await UpdateJobStatus(jobId, 100, false, "Failed to process - specification not found");
                return;
            }

            // Fetch current provider results for specification
            Stopwatch getCalculationResultsStopwatch = Stopwatch.StartNew();
            IEnumerable<ProviderResult> providerResults = await GetProviderResultsBySpecificationId(specificationId);
            getCalculationResultsStopwatch.Stop();
            UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);
            await UpdateJobStatus(jobId, calculationProgress, null);

            if (providerResults.IsNullOrEmpty())
            {
                _logger.Error($"Provider results not found for specification id {specificationId}");
                UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error, "Could not find any provider results");
                await UpdateJobStatus(jobId, 100, false, "Failed to process - could not find any provider results");
                return;
            }

            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = new List<PublishedProviderCalculationResult>();
            List<PublishedProviderResult> publishedProviderResultsToSave = new List<PublishedProviderResult>();
            int numberOfRecordsToZero = 0;

            Stopwatch saveCalculationResultsStopwatch = new Stopwatch();
            Stopwatch saveCalculationResultsHistoryStopwatch = new Stopwatch();
            Stopwatch assemblePublishedProviderResultsStopwatch = new Stopwatch();
            Stopwatch existingRecordsToZeroStopwatch = new Stopwatch();
            Stopwatch existingPublishedProviderResultsStopwatch = new Stopwatch();
            Stopwatch assembleSaveAndExcludeStopwatch = new Stopwatch();

            Reference author = message.GetUserDetails();

            // Fetch existing published provider results
            existingPublishedProviderResultsStopwatch.Start();
            IEnumerable<PublishedProviderResultExisting> existingPublishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetExistingPublishedProviderResultsForSpecificationId(specificationId));
            existingPublishedProviderResultsStopwatch.Stop();

            // Assemble published provider results
            assemblePublishedProviderResultsStopwatch.Start();
            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsAssemblerService.AssemblePublishedProviderResults(providerResults, author, specification);
            assemblePublishedProviderResultsStopwatch.Stop();
            UpdateCacheForSegmentDone(specificationId, calculationProgress += 13, CalculationProgressStatus.InProgress);
            await UpdateJobStatus(jobId, calculationProgress, null);

            // Can we shortcut the processing time if the results haven't been updated since the last time we did this
            if (specification.ShouldRefresh)
            {
                // Compare newly generated and existing published provider results to get a delta
                assembleSaveAndExcludeStopwatch.Start();
                (IEnumerable<PublishedProviderResult> newOrUpdatedPublishedProviderResults, IEnumerable<PublishedProviderResultExisting> existingRecordsToZero) =
                    await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsAssemblerService.GeneratePublishedProviderResultsToSave(publishedProviderResults, existingPublishedProviderResults));
                assembleSaveAndExcludeStopwatch.Stop();

                if (newOrUpdatedPublishedProviderResults.AnyWithNullCheck())
                {
                    publishedProviderResultsToSave.AddRange(newOrUpdatedPublishedProviderResults);
                }

                // When the assembly doesn't return an allocation line result for a provider and it already exists, set to value to 0 and update the status to Updated
                existingRecordsToZeroStopwatch.Start();
                if (existingRecordsToZero.AnyWithNullCheck())
                {
                    IEnumerable<PublishedProviderResult> recordsToZero = await FetchAndCheckExistingRecordsToZero(existingRecordsToZero);
                    numberOfRecordsToZero = recordsToZero.Count();
                    if (recordsToZero.AnyWithNullCheck())
                    {
                        publishedProviderResultsToSave.AddRange(recordsToZero);
                    }
                }
                existingPublishedProviderResultsStopwatch.Stop();
            }

            Stopwatch savePublishedResultsStopwatch = new Stopwatch();
            Stopwatch savePublishedResultsHistoryStopwatch = new Stopwatch();
            Stopwatch savePublishedResultsSearchStopwatch = new Stopwatch();
            Stopwatch sendToProfilingStopwatch = new Stopwatch();
            long saveProviderChangesMs = 0;

            // Process Provider Variations
            Stopwatch processVariationsStopwatch = Stopwatch.StartNew();
            ProcessProviderVariationsResult providerChanges = await _providerVariationsService.ProcessProviderVariations(triggeringJob, specification, providerResults, existingPublishedProviderResults, publishedProviderResults, publishedProviderResultsToSave, author);
            processVariationsStopwatch.Stop();

            // This is a good place to put a breakpoint for checking variation status and then diverting into failure code to avoid saving incorrect results
            if (providerChanges == null)
            {
                _logger.Error("Provider changes returned null for specification '{specificationId}'", specificationId);
                UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error, "Provider changes returned null");
                await UpdateJobStatus(jobId, 100, false, "Failed to process - provider changes returned null");
                return;
            }

            IEnumerable<ProviderVariationError> variationErrors = providerChanges.Errors;

            if (variationErrors.AnyWithNullCheck())
            {
                _logger.Error($"Failed to process provider variations for specification: {specificationId}");

                try
                {
                    string errorFileLocation = await _providerVariationsStorageRepository.SaveErrors(specificationId, jobId, variationErrors);
                    _logger.Information($"Provider variation errors for specification '{specificationId}'  and job '{jobId}' have been saved to '{errorFileLocation}'");

                    IDictionary<string, string> errorProperties = new Dictionary<string, string>
                    {
                        { "specificationId", specificationId },
                        { "jobId", jobId },
                        { "errorFile", errorFileLocation },
                        { "numberOfErrors", variationErrors.Count().ToString() }
                    };
                    _telemetry.TrackEvent("VariationsEvent", errorProperties);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to save provider errors for specification '{specificationId}'  and job '{jobId}'");
                }

                UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error, "Failed to process provider variations");
                await UpdateJobStatus(jobId, 100, false, "Failed to process provider variations");
                return;
            }

            DateTimeOffset publishedResultsRefreshedAt = DateTimeOffset.Now;

            if (publishedProviderResultsToSave.Any())
            {
                publishedProviderResultsToSave.ForEach(m =>
                {
                    // Set versioning
                    _publishedAllocationLineLogicalResultVersionService.SetVersion(m.FundingStreamResult.AllocationLineResult.Current);

                    // Set feed index ID for search
                    SetFeedIndexId(m);

                    // Set job ID and correlation ID for current operation
                    m.FundingStreamResult.AllocationLineResult.Current.JobId = jobId;
                    m.FundingStreamResult.AllocationLineResult.Current.CorrelationId = jobId;
                });

                try
                {
                    // Save delta of published provider results
                    savePublishedResultsStopwatch.Start();
                    await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.SavePublishedResults(publishedProviderResultsToSave));
                    savePublishedResultsStopwatch.Stop();
                    UpdateCacheForSegmentDone(specificationId, calculationProgress += 15, CalculationProgressStatus.InProgress);
                    await UpdateJobStatus(jobId, calculationProgress, null);

                    // Save history of delta of published provider results
                    savePublishedResultsHistoryStopwatch.Start();
                    await SavePublishedAllocationLineResultVersionHistory(publishedProviderResultsToSave);
                    savePublishedResultsHistoryStopwatch.Stop();
                    UpdateCacheForSegmentDone(specificationId, calculationProgress += 15, CalculationProgressStatus.InProgress);
                    await UpdateJobStatus(jobId, calculationProgress, null);

                    // Update the search index for the delta of published provider results
                    savePublishedResultsSearchStopwatch.Start();
                    await UpdateAllocationNotificationsFeedIndex(publishedProviderResultsToSave, specification);
                    savePublishedResultsSearchStopwatch.Stop();
                    UpdateCacheForSegmentDone(specificationId, calculationProgress += 10, CalculationProgressStatus.InProgress);
                    await UpdateJobStatus(jobId, calculationProgress, null);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, $"Failed to create published provider results for specification: {specificationId}");
                    UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error, "Failed to create published provider results");
                    throw new RetriableException($"Failed to create published provider results for specification: {specificationId}", ex);
                }
            }

            saveProviderChangesMs = await SaveProviderChanges(providerChanges.ProviderChanges, specification.Id, jobId, publishedResultsRefreshedAt, calculationProgress += 5);

            bool canCompleteJob = true;

            // Profile the delta of the published provider results
            if (publishedProviderResultsToSave.AnyWithNullCheck(r => !r.FundingStreamResult.AllocationLineResult.HasResultBeenVaried))
            {
                // Queue all published provider results to be profiled
                sendToProfilingStopwatch.Start();
                await GenerateProfilingPeriods(message, publishedProviderResultsToSave.Where(r => !r.FundingStreamResult.AllocationLineResult.HasResultBeenVaried), specification.Id, jobId);
                sendToProfilingStopwatch.Stop();
                UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);
                await UpdateJobStatus(jobId, calculationProgress, null);
                canCompleteJob = false;

                // Update the specification to store when this refresh happened
                HttpStatusCode updatePublishedRefreshedDateStatus = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.UpdatePublishedRefreshedDate(specification.Id, publishedResultsRefreshedAt));
                if (updatePublishedRefreshedDateStatus.IsSuccess())
                {
                    _logger.Information($"Updated the published refresh date on the specification with id: {specificationId}");
                }
                else
                {
                    _logger.Error($"Failed to update the published refresh date on the specification with id: {specificationId}. Failed with code: {updatePublishedRefreshedDateStatus}");
                }

                UpdateCacheForSegmentDone(specificationId, 100, CalculationProgressStatus.Finished, publishedResultsRefreshedAt: publishedResultsRefreshedAt, publishedProviderResults: publishedProviderResultsToSave);
            }
            else
            {
                _logger.Information($"Finished processing PublishProviderResult message for specification '{specificationId}' and job '{jobId}'");
                UpdateCacheForSegmentDone(specificationId, 100, CalculationProgressStatus.Finished, publishedProviderResults: publishedProviderResultsToSave);
            }

            if (canCompleteJob)
            {
                await UpdateJobStatus(jobId, 100, true, "Published Provider Results Updated");
            }

            IDictionary<string, double> metrics = new Dictionary<string, double>()
            {
                { "publishproviderresults-getCalculationResultsMs", getCalculationResultsStopwatch.ElapsedMilliseconds },
                { "publishproviderresults-getSpecificationMs", getSpecificationStopwatch.ElapsedMilliseconds },
                { "publishproviderresults-assemblePublishedCalculationResultsTotal", publishedProviderCalculationResults.Count() },
                { "publishproviderresults-saveCalculationResultsMs", saveCalculationResultsStopwatch.ElapsedMilliseconds },
                { "publishproviderresults-saveCalculationResultsHistoryMs", saveCalculationResultsHistoryStopwatch.ElapsedMilliseconds },
                { "publishproviderresults-assemblePublishedProviderResultsMs", assemblePublishedProviderResultsStopwatch.ElapsedMilliseconds },
                { "publishproviderresults-existingPublishedProviderResultsMs", existingPublishedProviderResultsStopwatch.ElapsedMilliseconds },
                { "publishproviderresults-assembleSaveAndExcludeMs", assembleSaveAndExcludeStopwatch.ElapsedMilliseconds },
                { "publishproviderresults-savePublishedResultsCount", publishedProviderResultsToSave.Count },
                { "publishproviderresults-savePublishedCalculationsResultsCount", publishedProviderCalculationResults.Count() },
                { "publishproviderresults-processProviderVariationsMs", processVariationsStopwatch.ElapsedMilliseconds },
            };

            if (publishedProviderResultsToSave.Any())
            {
                metrics.Add("publishproviderresults-savePublishedResultsMs", savePublishedResultsStopwatch.ElapsedMilliseconds);
                metrics.Add("publishproviderresults-savePublishedResultsHistoryMs", savePublishedResultsHistoryStopwatch.ElapsedMilliseconds);
                metrics.Add("publishproviderresults-savePublishedResultsSearchMs", savePublishedResultsSearchStopwatch.ElapsedMilliseconds);
                metrics.Add("publishproviderresults-sendToProfilingMs", sendToProfilingStopwatch.ElapsedMilliseconds);
            }

            if (numberOfRecordsToZero > 0)
            {
                metrics.Add("publishproviderresults-existingRecordsToZeroMs", existingRecordsToZeroStopwatch.ElapsedMilliseconds);
                metrics.Add("publishproviderresults-existingRecordsToZeroTotal", numberOfRecordsToZero);
            }

            if (providerChanges.ProviderChanges.AnyWithNullCheck())
            {
                metrics.Add("publishproviderresults-saveProviderChangesMs", saveProviderChangesMs);
            }

            _telemetry.TrackEvent("PublishProviderResults",
                new Dictionary<string, string>()
                {
                    { "specificationId" , specificationId },
                },
                metrics
            );
        }

        private async Task<long> SaveProviderChanges(IEnumerable<ProviderChangeItem> providerChanges, string specificationId, string jobId, DateTimeOffset publishedResultsRefreshedAt, int calculationProgress)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            if (providerChanges.AnyWithNullCheck())
            {
                List<ProviderChangeRecord> providerChangeRecords = new List<ProviderChangeRecord>(providerChanges.Count());
                foreach (ProviderChangeItem changeItem in providerChanges)
                {
                    providerChangeRecords.Add(new ProviderChangeRecord()
                    {
                        ChangeItem = changeItem,
                        Id = Guid.NewGuid().ToString(),
                        JobId = jobId,
                        CorrelationId = jobId,
                        ProcessedDate = publishedResultsRefreshedAt,
                        ProviderId = changeItem.UpdatedProvider.Id,
                        SpecificationId = specificationId,
                    });
                }

                await _providerChangesRepositoryPolicy.ExecuteAsync(() => _providerChangesRepository.AddProviderChanges(providerChangeRecords));
            }

            stopwatch.Stop();

            UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.InProgress);
            await UpdateJobStatus(jobId, calculationProgress, null);

            return stopwatch.ElapsedMilliseconds;
        }

        private async Task<IEnumerable<PublishedProviderResult>> FetchAndCheckExistingRecordsToZero(IEnumerable<PublishedProviderResultExisting> existingRecordsToZero)
        {
            // Don't set records to zero if they already have that value
            existingRecordsToZero = existingRecordsToZero.Where(r => r.Value != 0);

            ConcurrentBag<PublishedProviderResult> results = new ConcurrentBag<PublishedProviderResult>();

            List<Task> allTasks = new List<Task>();
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 30);
            foreach (PublishedProviderResultExisting existing in existingRecordsToZero)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            PublishedProviderResult existingResult = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultForId(existing.Id, existing.ProviderId));
                            if (existingResult != null)
                            {
                                existingResult.FundingStreamResult.AllocationLineResult.Current.Value = 0;
                                if (existingResult.FundingStreamResult.AllocationLineResult.Current.Status != AllocationLineStatus.Held)
                                {
                                    existingResult.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Updated;
                                }

                                existingResult.FundingStreamResult.AllocationLineResult.Current.Version =
                                    await _publishedProviderResultsVersionRepository.GetNextVersionNumber(existingResult.FundingStreamResult.AllocationLineResult.Current, existing.Version, incrementFromCurrentVersion: true);

                                results.Add(existingResult);
                            }
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            return results;
        }

        public async Task<IActionResult> GetPublishedProviderResultsBySpecificationId(HttpRequest request)
        {
            string specificationId = GetParameter(request, "specificationId");

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetPublishedProviderResultsBySpecificationId");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            IEnumerable<PublishedProviderResultByAllocationLineViewModel> publishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultSummaryForSpecificationId(specificationId));

            if (publishedProviderResults.IsNullOrEmpty())
            {
                return new OkObjectResult(Enumerable.Empty<PublishedProviderResultModel>());
            }

            IEnumerable<PublishedProviderResultModel> publishedProviderResultModels = MapPublishedProviderResultModels(publishedProviderResults);

            return new OkObjectResult(publishedProviderResultModels);
        }

        public async Task<IActionResult> GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(HttpRequest request)
        {

            string specificationId = GetParameter(request, "specificationId");
            string fundingPeriodId = GetParameter(request, "fundingPeriodId");
            string fundingStreamId = GetParameter(request, "fundingStreamId");

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            if (string.IsNullOrWhiteSpace(fundingPeriodId))
            {
                _logger.Error("No fundingPeriod Id was provided to GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId");

                return new BadRequestObjectResult("Null or empty fundingPeriod Id provided");
            }

            if (string.IsNullOrWhiteSpace(fundingStreamId))
            {
                _logger.Error("No fundingStream Id was provided to GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId");

                return new BadRequestObjectResult("Null or empty fundingStream Id provided");
            }

            IEnumerable<PublishedProviderResultByAllocationLineViewModel> publishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultsSummaryByFundingPeriodIdAndSpecificationIdAndFundingStreamId(fundingPeriodId, specificationId, fundingStreamId));

            if (publishedProviderResults.IsNullOrEmpty())
            {
                return new OkObjectResult(Enumerable.Empty<PublishedProviderResultModel>());
            }

            IEnumerable<PublishedProviderResultModel> publishedProviderResultModels = MapPublishedProviderResultModels(publishedProviderResults);

            return new OkObjectResult(publishedProviderResultModels);
        }

        public async Task<IActionResult> GetConfirmationDetailsForApprovePublishProviderResults(HttpRequest request)
        {
            string specificationId = GetParameter(request, "specificationId");

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to GetConfirmationDetailsForApprovePublishProviderResults");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            string json = await request.GetRawBodyStringAsync();

            UpdatePublishedAllocationLineResultStatusModel filterCriteria = JsonConvert.DeserializeObject<UpdatePublishedAllocationLineResultStatusModel>(json);

            if (filterCriteria == null)
            {
                _logger.Error("Null filterCriteria was provided to GetConfirmationDetailsForApprovePublishProviderResults");

                return new BadRequestObjectResult("Null filterCriteria was provided");
            }

            if (filterCriteria.Providers.IsNullOrEmpty())
            {
                _logger.Error("Null or empty providers was provided to GetConfirmationDetailsForApprovePublishProviderResults");

                return new BadRequestObjectResult("Null or empty providers was provided");
            }

            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultsForSpecificationAndStatus(specificationId, filterCriteria));

            ConfirmPublishApproveModel confirmationDetails = new ConfirmPublishApproveModel
            {
                NumberOfProviders = publishedProviderResults.Select(r => r.FundingStreamResult.AllocationLineResult.Current.Provider.Id).Distinct().Count(),
                ProviderTypes = publishedProviderResults.Select(r => r.FundingStreamResult.AllocationLineResult.Current.Provider.ProviderType).Distinct().OrderBy(t => t).ToArray(),
                LocalAuthorities = publishedProviderResults.Select(r => r.FundingStreamResult.AllocationLineResult.Current.Provider.Authority).Distinct().OrderBy(a => a).ToArray(),
                FundingPeriod = publishedProviderResults.Select(r => r.FundingPeriod.Name).FirstOrDefault()
            };

            IEnumerable<IGrouping<string, PublishedFundingStreamResult>> fundingStreams = publishedProviderResults.Select(r => r.FundingStreamResult).GroupBy(r => r.FundingStream.Name);
            decimal totalFundingAmount = 0;
            foreach (IGrouping<string, PublishedFundingStreamResult> fundingStream in fundingStreams)
            {
                FundingStreamSummaryModel summary = new FundingStreamSummaryModel
                {
                    Name = fundingStream.Key,
                };

                foreach (PublishedFundingStreamResult result in fundingStream)
                {
                    AllocationLineSummaryModel existingAL = summary.AllocationLines.FirstOrDefault(a => a.Name == result.AllocationLineResult.AllocationLine.Name);

                    if (existingAL == null)
                    {
                        summary.AllocationLines.Add(new AllocationLineSummaryModel { Name = result.AllocationLineResult.AllocationLine.Name, Value = result.AllocationLineResult.Current.Value });
                    }
                    else
                    {
                        existingAL.Value += result.AllocationLineResult.Current.Value;
                    }

                    totalFundingAmount += result.AllocationLineResult.Current.Value ?? 0;
                }

                confirmationDetails.FundingStreams.Add(summary);
            }

            confirmationDetails.TotalFundingApproved = totalFundingAmount;
            return new OkObjectResult(confirmationDetails);
        }

        public async Task<IActionResult> UpdatePublishedAllocationLineResultsStatus(HttpRequest request)
        {
            string specificationId = GetParameter(request, "specificationId");

            if (string.IsNullOrWhiteSpace(specificationId))
            {
                _logger.Error("No specification Id was provided to UpdateAllocationLineResultStatus");
                return new BadRequestObjectResult("Null or empty specification Id provided");
            }

            string json = await request.GetRawBodyStringAsync();

            UpdatePublishedAllocationLineResultStatusModel updateStatusModel = JsonConvert.DeserializeObject<UpdatePublishedAllocationLineResultStatusModel>(json);

            if (updateStatusModel == null)
            {
                _logger.Error("Null updateStatusModel was provided to UpdateAllocationLineResultStatus");

                return new BadRequestObjectResult("Null updateStatusModel was provided");
            }

            if (updateStatusModel.Providers.IsNullOrEmpty())
            {
                _logger.Error("Null or empty providers was provided to UpdateAllocationLineResultStatus");

                return new BadRequestObjectResult("Null or empty providers was provided");
            }

            string cacheKey = $"{CacheKeys.AllocationLineResultStatusUpdates}{Guid.NewGuid().ToString()}";

            await _cacheProvider.SetAsync<UpdatePublishedAllocationLineResultStatusModel>(cacheKey, updateStatusModel);

            Reference user = request.GetUserOrDefault();

            JobCreateModel job = new JobCreateModel
            {
                InvokerUserDisplayName = user.Name,
                InvokerUserId = user.Id,
                JobDefinitionId = JobConstants.DefinitionNames.CreateInstructAllocationLineResultStatusUpdateJob,
                SpecificationId = specificationId,
                Properties = new Dictionary<string, string>
                {
                    { "specification-id", specificationId },
                    { "cache-key", cacheKey }
                },
                Trigger = new Trigger
                {
                    EntityId = specificationId,
                    EntityType = nameof(Specification),
                    Message = $"Updating allocation line results status"
                },
                CorrelationId = request.GetCorrelationId()
            };

            Job newJob = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.CreateJob(job));

            _logger.Information($"New job: '{JobConstants.DefinitionNames.CreateInstructAllocationLineResultStatusUpdateJob}' created with id: '{newJob.Id}'");

            return new OkResult();
        }

        public async Task CreateAllocationLineResultStatusUpdateJobs(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            if (!message.UserProperties.ContainsKey("jobId"))
            {
                _logger.Error("Missing parent job id to instruct allocation line status updates");

                return;
            }

            string jobId = message.UserProperties["jobId"].ToString();

            JobViewModel job = await RetrieveJobAndCheckCanBeProcessed(jobId);
            if (job == null)
            {
                return;
            }

            string cacheKey = job.Properties["cache-key"];

            await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(jobId, new JobLogUpdateModel()));

            IDictionary<string, string> properties = message.BuildMessageProperties();

            UpdatePublishedAllocationLineResultStatusModel publishedAllocationLineResultStatusUpdateModel = await _cacheProvider.GetAsync<UpdatePublishedAllocationLineResultStatusModel>(cacheKey);

            if (publishedAllocationLineResultStatusUpdateModel == null)
            {
                _logger.Error($"Could not find the update model in cache with cache key: '{cacheKey}'");

                throw new Exception($"Could not find the update model in cache with cache key: '{cacheKey}'");
            }

            int totalCount = publishedAllocationLineResultStatusUpdateModel.Providers.Count();

            IList<JobCreateModel> childJobs = new List<JobCreateModel>();

            int maxPartitionSize = _publishedProviderResultsSettings.UpdateAllocationLineResultStatusBatchCount;

            for (int partitionIndex = 0; partitionIndex < totalCount; partitionIndex += maxPartitionSize)
            {
                UpdatePublishedAllocationLineResultStatusModel partitionedModel = new UpdatePublishedAllocationLineResultStatusModel
                {
                    Status = publishedAllocationLineResultStatusUpdateModel.Status,
                    Providers = publishedAllocationLineResultStatusUpdateModel.Providers.Skip(partitionIndex).Take(maxPartitionSize)
                };

                IDictionary<string, string> jobProperties = new Dictionary<string, string>();

                foreach (KeyValuePair<string, string> item in properties)
                {
                    jobProperties.Add(item.Key, item.Value);
                }

                jobProperties.Add("specification-id", job.SpecificationId);

                JobCreateModel newJob = CreateGenerateAllocationLineResultStatusUpdateJob(job, jobProperties, partitionedModel);

                childJobs.Add(newJob);
            }


            IEnumerable<Job> newJobs = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.CreateJobs(childJobs));

            int newJobsCount = newJobs.Count();
            int childJobsCount = childJobs.Count();

            if (newJobsCount < childJobsCount)
            {
                _logger.Error($"Only {newJobsCount} jobs were created from {childJobsCount} childJobs for parent job: '{job.Id}'");

                throw new Exception($"Only {newJobsCount} jobs were created from {childJobsCount} childJobs for parent job: '{job.Id}'");
            }

            await _cacheProvider.RemoveAsync<UpdatePublishedAllocationLineResultStatusModel>(cacheKey);
        }

        public async Task UpdateAllocationLineResultStatus(Message message)
        {
            Guard.ArgumentNotNull(message, nameof(message));

            if (!message.UserProperties.ContainsKey("jobId"))
            {
                _logger.Error("Missing parent job id to update allocation line result status");

                throw new Exception("Missing parent job id to update allocation line result status");
            }

            string jobId = message.UserProperties["jobId"].ToString();

            JobViewModel job = await RetrieveJobAndCheckCanBeProcessed(jobId);
            if (job == null)
            {
                return;
            }

            await UpdateJobStatus(jobId);

            UpdatePublishedAllocationLineResultStatusModel publishedAllocationLineResultStatusUpdateModel = message.GetPayloadAsInstanceOf<UpdatePublishedAllocationLineResultStatusModel>();

            if (publishedAllocationLineResultStatusUpdateModel == null)
            {
                _logger.Error($"A null allocation line result status update model was provided for job id  '{jobId}'");

                await CreateFailedJobStatus(jobId, "Failed to update allocation line result status - null update model provided");

                return;
            }

            IEnumerable<string> providerIds = publishedAllocationLineResultStatusUpdateModel.Providers.Select(p => p.ProviderId);

            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultsForSpecificationIdAndProviderId(job.SpecificationId, providerIds));

            if (publishedProviderResults.IsNullOrEmpty())
            {
                _logger.Error($"No provider results to update for specification id: {job.SpecificationId}");

                await CompleteBatch(job.Id, "No provider results found to update");

                return;
            }

            try
            {
                await UpdateAllocationLineResultsStatus(publishedProviderResults.ToList(), publishedAllocationLineResultStatusUpdateModel, job);

                await CompleteBatch(jobId);
            }
            catch (RetriableException ex)
            {
                _logger.Error(ex, $"Failed to update result status's for specification: '{job.SpecificationId}'");

                throw;
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"Failed to update allocation line result status for job id: '{job.Id}'");

                await CreateFailedJobStatus(jobId, "Failed to update allocation line result status - with unknown exception");
            }
        }

        private async Task CompleteBatch(string jobId, string outcome = "Allocation line results were successfully updated")
        {
            await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(jobId, new JobLogUpdateModel
            {
                CompletedSuccessfully = true,
                Outcome = outcome
            }));
        }

        private JobCreateModel CreateGenerateAllocationLineResultStatusUpdateJob(JobViewModel parentJob, IDictionary<string, string> jobProperties, UpdatePublishedAllocationLineResultStatusModel partitionedModel)
        {
            IList<JobCreateModel> jobCreateModels = new List<JobCreateModel>();

            Trigger trigger = new Trigger
            {
                EntityId = parentJob.Id,
                EntityType = nameof(Job),
                Message = $"Triggered by parent job"
            };

            return new JobCreateModel
            {
                InvokerUserDisplayName = parentJob.InvokerUserDisplayName,
                InvokerUserId = parentJob.InvokerUserId,
                JobDefinitionId = JobConstants.DefinitionNames.CreateAllocationLineResultStatusUpdateJob,
                SpecificationId = parentJob.SpecificationId,
                Properties = jobProperties,
                ParentJobId = parentJob.Id,
                Trigger = trigger,
                CorrelationId = parentJob.CorrelationId,
                MessageBody = JsonConvert.SerializeObject(partitionedModel)
            };
        }

        private async Task CreateFailedJobStatus(string jobId, string outcome = null)
        {
            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = false,
                Outcome = outcome
            };

            ApiResponse<JobLog> jobLogResponse = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(jobId, jobLogUpdateModel));

            if (jobLogResponse == null || jobLogResponse.Content == null)
            {
                _logger.Error($"Failed to add a job log for job id '{jobId}'");
            }
        }

        private static string GetParameter(HttpRequest request, string name)
        {
            if (request.Query.TryGetValue(name, out Microsoft.Extensions.Primitives.StringValues parameter))
            {
                return parameter.FirstOrDefault();
            }
            return null;
        }

        public Task<IEnumerable<ProviderResult>> GetProviderResultsBySpecificationId(string specificationId, int maxResults = -1)
        {
            return _resultsRepositoryPolicy.ExecuteAsync(() => _resultsRepository.GetProviderResultsBySpecificationId(specificationId, maxResults));
        }

        public async Task<IActionResult> ReIndexAllocationNotificationFeeds()
        {
            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetAllNonHeldPublishedProviderResults());

            if (publishedProviderResults.IsNullOrEmpty())
            {
                _logger.Warning("No published provider results were found to index.");
            }
            else
            {
                IEnumerable<PublishedProviderResult> publishedProviderResultsToIndex = await GetAllPublishedResultVersionsExcludingHeld(publishedProviderResults);

                IEnumerable<string> specificationIds = publishedProviderResults.DistinctBy(m => m.SpecificationId).Select(m => m.SpecificationId);

                foreach (string specificationId in specificationIds)
                {
                    SpecificationCurrentVersion specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetCurrentSpecificationById(specificationId));

                    try
                    {
                        IEnumerable<PublishedProviderResult> publishedProviderResultsForSpecification = publishedProviderResultsToIndex.Where(m => m.SpecificationId == specification.Id);

                        await UpdateAllocationNotificationsFeedIndex(publishedProviderResultsForSpecification, specification);
                    }
                    catch (Exception ex)
                    {
                        _logger.Error(ex, "Failed to index allocation feeds");

                        return new InternalServerErrorResult(ex.Message);
                    }
                }

            }

            return new NoContentResult();
        }

        private async Task<IEnumerable<PublishedProviderResult>> GetAllPublishedResultVersionsExcludingHeld(IEnumerable<PublishedProviderResult> publishedProviderResults)
        {
            ConcurrentBag<PublishedProviderResult> updatedResultsToIndex = new ConcurrentBag<PublishedProviderResult>();
            List<Task> allTasks = new List<Task>(publishedProviderResults.Count());
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 50);
            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            IEnumerable<PublishedAllocationLineResultVersion> versions = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetAllNonHeldPublishedProviderResultVersions(publishedProviderResult.Id, publishedProviderResult.ProviderId));

                            foreach (PublishedAllocationLineResultVersion version in versions)
                            {
                                updatedResultsToIndex.Add(GetPublishedProviderResultToIndex(publishedProviderResult, version));
                            }
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await Task.WhenAll(allTasks.ToArray());

            foreach (Task task in allTasks)
            {
                if (task.Exception != null)
                {
                    throw task.Exception;
                }
            }

            return updatedResultsToIndex;
        }

        private IEnumerable<PublishedProviderResultModel> MapPublishedProviderResultModels(IEnumerable<PublishedProviderResultByAllocationLineViewModel> publishedProviderResults)
        {
            if (publishedProviderResults.IsNullOrEmpty())
            {
                return Enumerable.Empty<PublishedProviderResultModel>();
            }

            ConcurrentDictionary<string, PublishedProviderResultModel> results = new ConcurrentDictionary<string, PublishedProviderResultModel>(5, publishedProviderResults.Count());
            ConcurrentDictionary<string, ConcurrentDictionary<string, PublishedFundingStreamResultModel>> fundingStreams = new ConcurrentDictionary<string, ConcurrentDictionary<string, PublishedFundingStreamResultModel>>(5, publishedProviderResults.Count());
            ConcurrentDictionary<string, ConcurrentDictionary<string, List<PublishedAllocationLineResultModel>>> allocationLines = new ConcurrentDictionary<string, ConcurrentDictionary<string, List<PublishedAllocationLineResultModel>>>(5, publishedProviderResults.Count());

            Parallel.ForEach(publishedProviderResults, (providerResult) =>
            {
                if (!results.ContainsKey(providerResult.ProviderId))
                {
                    PublishedProviderResultModel publishedProviderResultModel = new PublishedProviderResultModel
                    {
                        ProviderId = providerResult.ProviderId,
                        ProviderName = providerResult.ProviderName,
                        ProviderType = providerResult.ProviderType,
                    };

                    // Add funding stream container
                    fundingStreams.TryAdd(providerResult.ProviderId, new ConcurrentDictionary<string, PublishedFundingStreamResultModel>());

                    // Add allocation line container
                    allocationLines.TryAdd(providerResult.ProviderId, new ConcurrentDictionary<string, List<PublishedAllocationLineResultModel>>());

                    // Add provider to results
                    results.TryAdd(providerResult.ProviderId, publishedProviderResultModel);
                }

                if (!fundingStreams[providerResult.ProviderId].ContainsKey(providerResult.FundingStreamId))
                {
                    PublishedFundingStreamResultModel publishedFundingStreamResult = new PublishedFundingStreamResultModel()
                    {
                        FundingStreamId = providerResult.FundingStreamId,
                        FundingStreamName = providerResult.FundingStreamName,
                    };

                    fundingStreams[providerResult.ProviderId].TryAdd(providerResult.FundingStreamId, publishedFundingStreamResult);
                }

                PublishedAllocationLineResultModel allocationLine = new PublishedAllocationLineResultModel
                {
                    AllocationLineId = providerResult.AllocationLineId,
                    AllocationLineName = providerResult.AllocationLineName,
                    FundingAmount = providerResult.FundingAmount,
                    Status = providerResult.Status,
                    LastUpdated = providerResult.LastUpdated,
                    Authority = providerResult.Authority,
                    Version = string.IsNullOrWhiteSpace(providerResult.VersionNumber) ? "n/a" : providerResult.VersionNumber,
                };

                if (!allocationLines[providerResult.ProviderId].ContainsKey(providerResult.FundingStreamId))
                {
                    allocationLines[providerResult.ProviderId].TryAdd(providerResult.FundingStreamId, new List<PublishedAllocationLineResultModel>());
                }

                allocationLines[providerResult.ProviderId][providerResult.FundingStreamId].Add(allocationLine);
            });

            // Sort funding streams and allocation lines by name for each provider
            foreach (KeyValuePair<string, PublishedProviderResultModel> provider in results)
            {
                foreach (KeyValuePair<string, PublishedFundingStreamResultModel> fundingStream in fundingStreams[provider.Key])
                {
                    fundingStream.Value.AllocationLineResults = allocationLines[provider.Key][fundingStream.Key].OrderBy(a => a.AllocationLineName);
                }

                provider.Value.FundingStreamResults = fundingStreams[provider.Key].Values.OrderBy(f => f.FundingStreamName);
            }

            // Order providers by Provider name
            return results.Values.OrderBy(r => r.ProviderName);
        }

        private async Task UpdateAllocationLineResultsStatus(IEnumerable<PublishedProviderResult> publishedProviderResults,
           UpdatePublishedAllocationLineResultStatusModel updateStatusModel, JobViewModel jobViewModel)
        {
            IList<string> updatedAllocationLineIds = new List<string>();
            IList<string> updatedProviderIds = new List<string>();
            IList<PublishedProviderResult> resultsToProfile = new List<PublishedProviderResult>();

            List<PublishedProviderResult> resultsToUpdate = new List<PublishedProviderResult>();
            List<PublishedAllocationLineResultVersion> historyToSave = new List<PublishedAllocationLineResultVersion>();

            Reference author = new Reference(jobViewModel.InvokerUserId, jobViewModel.InvokerUserDisplayName);

            foreach (UpdatePublishedAllocationLineResultStatusProviderModel providerstatusModel in updateStatusModel.Providers)
            {
                if (providerstatusModel.AllocationLineIds.IsNullOrEmpty())
                {
                    continue;
                }

                IEnumerable<PublishedProviderResult> results = publishedProviderResults.Where(m => m.ProviderId == providerstatusModel.ProviderId);

                if (results.IsNullOrEmpty())
                {
                    continue;
                }

                IEnumerable<PublishedAllocationLineResult> publishedAllocationLineResults = results.Select(m => m.FundingStreamResult.AllocationLineResult).ToArraySafe();

                if (publishedAllocationLineResults.IsNullOrEmpty())
                {
                    continue;
                }

                bool isUpdated = false;

                foreach (string allocationLineResultId in providerstatusModel.AllocationLineIds)
                {
                    IEnumerable<PublishedAllocationLineResult> allocationLineResults = publishedAllocationLineResults.Where(m => m.AllocationLine.Id == allocationLineResultId);

                    foreach (PublishedAllocationLineResult allocationLineResult in allocationLineResults)
                    {

                        if (CanUpdateAllocationLineResult(allocationLineResult, updateStatusModel.Status))
                        {
                            PublishedAllocationLineResultVersion newVersion = allocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
                            newVersion.Author = author;
                            newVersion.Date = DateTimeOffset.UtcNow;
                            newVersion.Status = updateStatusModel.Status;

                            newVersion = await _publishedProviderResultsVersionRepository.CreateVersion(newVersion, allocationLineResult.Current, providerstatusModel.ProviderId, true);

                            if (updateStatusModel.Status != AllocationLineStatus.Approved)
                            {
                                _publishedAllocationLineLogicalResultVersionService.SetVersion(newVersion);
                            }

                            allocationLineResult.Current = newVersion;

                            historyToSave.Add(newVersion);

                            if (!updatedAllocationLineIds.Contains(allocationLineResultId))
                            {
                                updatedAllocationLineIds.Add(allocationLineResultId);
                            }

                            if (!updatedProviderIds.Contains(providerstatusModel.ProviderId))
                            {
                                updatedProviderIds.Add(providerstatusModel.ProviderId);
                            }

                            isUpdated = true;
                        }
                    }
                }

                if (isUpdated)
                {
                    foreach (PublishedProviderResult result in results)
                    {
                        if (updateStatusModel.Status == AllocationLineStatus.Approved)
                        {
                            resultsToProfile.Add(result);
                        }
                    }

                    resultsToUpdate.AddRange(results);
                }
            }

            if (resultsToUpdate.Any())
            {
                SpecificationCurrentVersion specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetCurrentSpecificationById(jobViewModel.SpecificationId));

                try
                {
                    resultsToUpdate.ForEach(SetFeedIndexId);

                    resultsToUpdate.ForEach(SetPublishedField);

                    await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.SavePublishedResults(resultsToUpdate));

                    IEnumerable<KeyValuePair<string, PublishedAllocationLineResultVersion>> history = historyToSave.Select(m => new KeyValuePair<string, PublishedAllocationLineResultVersion>(m.ProviderId, m));

                    await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsVersionRepository.SaveVersions(history));

                    await UpdateAllocationNotificationsFeedIndex(resultsToUpdate, specification);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed when updating allocation line results");

                    throw new RetriableException("Failed when updating allocation line results", ex);
                }
            }
        }

        private async Task GenerateProfilingPeriods(Message message, IEnumerable<PublishedProviderResult> resultsToProfile, string specificationId, string parentJobId)
        {
            Dictionary<string, string> properties = new Dictionary<string, string>();
            properties.Add("specification-id", specificationId);

            properties.AddRange(message.BuildMessageProperties());
            await GenerateProfilingPeriods(resultsToProfile, properties, message.GetUserDetails(), specificationId, parentJobId);
        }

        private async Task GenerateProfilingPeriods(IEnumerable<PublishedProviderResult> resultsToProfile, Dictionary<string, string> properties, Reference userDetails, string specificationId, string parentJobId)
        {
            int batchSize = 100;
            int startPosition = 0;

            while (startPosition < resultsToProfile.Count())
            {
                IEnumerable<FetchProviderProfilingMessageItem> batchOfResultsToProfile = resultsToProfile.Skip(startPosition).Take(batchSize).Select(r => new FetchProviderProfilingMessageItem { AllocationLineResultId = r.Id, ProviderId = r.ProviderId });

                JobCreateModel jobCreateModel = new JobCreateModel
                {
                    InvokerUserDisplayName = userDetails.Name,
                    InvokerUserId = userDetails.Id,
                    ItemCount = batchOfResultsToProfile.Count(),
                    JobDefinitionId = JobConstants.DefinitionNames.FetchProviderProfileJob,
                    MessageBody = JsonConvert.SerializeObject(batchOfResultsToProfile),
                    ParentJobId = parentJobId,
                    Properties = properties,
                    SpecificationId = specificationId,
                    Trigger = new Trigger
                    {
                        EntityId = specificationId,
                        EntityType = nameof(Specification),
                        Message = "Fetching Profile Period for batch of providers"
                    }
                };

                Job newJob = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.CreateJob(jobCreateModel));

                _logger.Information($"New job: '{JobConstants.DefinitionNames.FetchProviderProfileJob}' created with id: '{newJob.Id}'");

                startPosition += batchOfResultsToProfile.Count();
            }
        }

        public async Task FetchProviderProfile(Message message)
        {
            Stopwatch profilingStopWatch = Stopwatch.StartNew();

            Guard.ArgumentNotNull(message, nameof(message));

            string jobId = string.Empty;

            if (!message.UserProperties.ContainsKey("jobId"))
            {
                _logger.Error("Missing parent job id to fetch provider profile periods");
                return;
            }

            jobId = message.UserProperties["jobId"].ToString();

            JobViewModel job = await RetrieveJobAndCheckCanBeProcessed(jobId);
            if (job == null)
            {
                return;
            }

            await UpdateJobStatus(jobId);

            if (!message.UserProperties.ContainsKey("specification-id"))
            {
                _logger.Error("No specification id was present on the message");
                return;
            }

            string specificationId = message.UserProperties["specification-id"].ToString();

            IEnumerable<FetchProviderProfilingMessageItem> data = message.GetPayloadAsInstanceOf<IEnumerable<FetchProviderProfilingMessageItem>>();

            if (data.IsNullOrEmpty())
            {
                _logger.Error("No allocation result profiling items were present in the message");
                return;
            }

            SpecificationCurrentVersion specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetCurrentSpecificationById(specificationId));

            if (specification == null)
            {
                _logger.Error($"A specification could not be found with id {specificationId}");
                return;
            }

            ConcurrentBag<PublishedProviderResult> publishedProviderResults = new ConcurrentBag<PublishedProviderResult>();

            IList<Task> profilingTasks = new List<Task>();

            long totalMsForProfilingApi = 0;
            int numFailed = 0;
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 15);
            foreach (FetchProviderProfilingMessageItem profilingItem in data)
            {
                await throttler.WaitAsync();
                profilingTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            (PublishedProviderResult publishedProviderResult, long timeInMs) profilingResult = await ProfileResult(profilingItem);

                            publishedProviderResults.Add(profilingResult.publishedProviderResult);

                            totalMsForProfilingApi += profilingResult.timeInMs;
                        }
                        catch (NonRetriableException)
                        {
                            Interlocked.Increment(ref numFailed);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }

            await TaskHelper.WhenAllAndThrow(profilingTasks.ToArray());

            if (!publishedProviderResults.IsNullOrEmpty())
            {
                await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.SavePublishedResults(publishedProviderResults));

                await UpdateAllocationNotificationsFeedIndex(publishedProviderResults, specification, true);
            }

            bool wasSuccessful = numFailed == 0;
            await UpdateJobStatus(jobId, data.Count(), numFailed, wasSuccessful, wasSuccessful ? "Profile periods fetched" : $"Failed to fetch {numFailed} profile periods");

            profilingStopWatch.Stop();

            int batchSize = data.Count();

            IDictionary<string, double> metrics = new Dictionary<string, double>()
                    {
                        { "profiling-batch-size-count", batchSize },
                        { "profiling-run-timeInMs", profilingStopWatch.ElapsedMilliseconds },
                        { "average-time-for-profiling-api", (long)(totalMsForProfilingApi / batchSize) }
                    };

            _telemetry.TrackEvent("ProviderProfilingProcessed",
                    new Dictionary<string, string>()
                    {
                        { "specificationId" , specificationId }
                    },
                    metrics
                );
        }

        /// <summary>
        /// This is to be removed once it has done its stuff
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task MigrateVersionNumbers(Message message)
        {
            if (!message.UserProperties.ContainsKey("specification-id"))
            {
                _logger.Error("No specification id was present on the message");
                throw new ArgumentException("Message must contain a specification id in user properties");
            }

            string specificationId = message.UserProperties["specification-id"].ToString();

            SpecificationCurrentVersion specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetCurrentSpecificationById(specificationId));

            if (specification == null)
            {
                _logger.Error($"Unable to locate a specification with id {specificationId}");
                return;
            }

            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultsForSpecificationId(specificationId));

            if (publishedProviderResults.IsNullOrEmpty())
            {
                _logger.Warning($"No published provider results found for specification id {specificationId}");
                return;
            }

            ConcurrentBag<PublishedAllocationLineResultVersion> updatedVersions = new ConcurrentBag<PublishedAllocationLineResultVersion>();
            ConcurrentBag<PublishedProviderResult> updatedResults = new ConcurrentBag<PublishedProviderResult>();

            List<Task> allTasks = new List<Task>(publishedProviderResults.Count());
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 5);
            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            IEnumerable<PublishedAllocationLineResultVersion> versions = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsVersionRepository.GetVersions(publishedProviderResult.Id, publishedProviderResult.ProviderId));

                            versions = versions.OrderBy(m => m.Version);

                            foreach (PublishedAllocationLineResultVersion version in versions)
                            {
                                if (version.Status == AllocationLineStatus.Held)
                                {
                                    version.Major = 0;
                                    version.Minor = version.Version <= 1 ? 1 : version.Version;

                                }
                                else if (version.Status == AllocationLineStatus.Approved)
                                {
                                    version.Major = 0;
                                    version.Minor = version.Version <= 1 ? 1 : version.Version - 1;
                                }
                                else
                                {
                                    version.Major = 1;
                                    version.Minor = 0;
                                }

                                updatedVersions.Add(version);
                            }

                            PublishedAllocationLineResultVersion mosetRecentVersion = versions.Last();

                            publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Major = mosetRecentVersion.Major;
                            publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Minor = mosetRecentVersion.Minor;

                            updatedResults.Add(publishedProviderResult);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await Task.WhenAll(allTasks.ToArray());

            foreach (Task task in allTasks)
            {
                if (task.Exception != null)
                {
                    throw task.Exception;
                }
            }

            try
            {
                await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsVersionRepository.SaveVersions(updatedVersions));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "MigrationError: Failed to index save updated versions");

                throw new Exception(ex.Message);
            }

            try
            {
                await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.SavePublishedResults(updatedResults));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "MigrationError: Failed to save updated published results");

                throw new Exception(ex.Message);
            }

            try
            {
                await UpdateAllocationNotificationsFeedIndex(updatedResults, specification);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "MigrationError: Failed to index allocation feeds with updated results");

                throw new Exception(ex.Message);
            }
        }

        /// <summary>
        /// This is to be removed once it has done its stuff
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task MigrateFeedIndexId(Message message)
        {
            if (!message.UserProperties.ContainsKey("specification-id"))
            {
                _logger.Error("No specification id was present on the message");
                throw new ArgumentException("Message must contain a specification id in user properties");
            }

            string specificationId = message.UserProperties["specification-id"].ToString();

            SpecificationCurrentVersion specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetCurrentSpecificationById(specificationId));

            if (specification == null)
            {
                _logger.Error($"Unable to locate a specification with id {specificationId}");
                return;
            }

            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultsForSpecificationId(specificationId));

            if (publishedProviderResults.IsNullOrEmpty())
            {
                _logger.Warning($"No published provider results found for specification id {specificationId}");
                return;
            }

            ConcurrentBag<PublishedAllocationLineResultVersion> updatedVersions = new ConcurrentBag<PublishedAllocationLineResultVersion>();
            ConcurrentBag<PublishedProviderResult> updatedResults = new ConcurrentBag<PublishedProviderResult>();

            ConcurrentBag<PublishedProviderResult> updatedResultsToIndex = new ConcurrentBag<PublishedProviderResult>();

            List<Task> allTasks = new List<Task>(publishedProviderResults.Count());
            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 5);
            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            IEnumerable<PublishedAllocationLineResultVersion> versions = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsVersionRepository.GetVersions(publishedProviderResult.Id, publishedProviderResult.ProviderId));

                            foreach (PublishedAllocationLineResultVersion version in versions)
                            {
                                SetFeedIndexId(publishedProviderResult, version);
                                updatedVersions.Add(version);

                                if (version.Status != AllocationLineStatus.Held)
                                {
                                    updatedResultsToIndex.Add(GetPublishedProviderResultToIndex(publishedProviderResult, version));
                                }
                            }

                            SetFeedIndexId(publishedProviderResult);

                            if (publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Status != AllocationLineStatus.Held && !updatedResultsToIndex.Any(m =>
                                        m.FundingStreamResult.AllocationLineResult.Current.FeedIndexId == publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.FeedIndexId))
                            {
                                updatedResultsToIndex.Add(publishedProviderResult);
                            }
                            updatedResults.Add(publishedProviderResult);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await Task.WhenAll(allTasks.ToArray());

            foreach (Task task in allTasks)
            {
                if (task.Exception != null)
                {
                    throw task.Exception;
                }
            }

            try
            {
                await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsVersionRepository.SaveVersions(updatedVersions));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "MigrationError: Failed to index save updated versions");

                throw new Exception(ex.Message);
            }

            try
            {
                await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.SavePublishedResults(updatedResults));
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "MigrationError: Failed to save updated published results");

                throw new Exception(ex.Message);
            }

            try
            {
                await UpdateAllocationNotificationsFeedIndex(updatedResultsToIndex, specification);
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "MigrationError: Failed to index allocation feeds with updated results");

                throw new Exception(ex.Message);
            }
        }

        public async Task<IActionResult> MigrateFeedIndexId(HttpRequest request)
        {
            IEnumerable<SpecificationSummary> specificationSummaries = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetSpecificationSummaries());

            foreach (SpecificationSummary specificationSummary in specificationSummaries)
            {
                IDictionary<string, string> properties = request.BuildMessageProperties();
                properties["specification-id"] = specificationSummary.Id;

                await _messengerService.SendToQueue(ServiceBusConstants.QueueNames.MigrateFeedIndexId, "", properties);
            }

            return new NoContentResult();
        }

        public async Task<IActionResult> MigrateVersionNumbers(HttpRequest request)
        {
            IEnumerable<SpecificationSummary> specificationSummaries = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetSpecificationSummaries());

            foreach (SpecificationSummary specificationSummary in specificationSummaries)
            {
                IDictionary<string, string> properties = request.BuildMessageProperties();
                properties["specification-id"] = specificationSummary.Id;

                await _messengerService.SendToQueue(ServiceBusConstants.QueueNames.MigrateResultVersions, "", properties);
            }

            return new NoContentResult();
        }

        public async Task<IActionResult> GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(
            string providerId,
            string specificationId,
            string fundingStreamId)
        {
            Guard.IsNullOrWhiteSpace(providerId, nameof(providerId), "No Provider ID provided", _logger);
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId), "No Specification ID provided", _logger);
            Guard.IsNullOrWhiteSpace(fundingStreamId, nameof(fundingStreamId), "No Funding Stream ID provided", _logger);

            var result = await _publishedProviderResultsRepository
                .GetPublishedProviderProfileForProviderIdAndSpecificationIdAndFundingStreamId(providerId, specificationId, fundingStreamId);

            if (result.Any()) return new OkObjectResult(result);

            return new NotFoundResult();
        }

        private PublishedProviderResult GetPublishedProviderResultToIndex(PublishedProviderResult publishedProviderResult, PublishedAllocationLineResultVersion version)
        {
            string json = JsonConvert.SerializeObject(publishedProviderResult);

            PublishedProviderResult clonedPublishedProviderResult = JsonConvert.DeserializeObject<PublishedProviderResult>(json);

            clonedPublishedProviderResult.FundingStreamResult.AllocationLineResult.Current = version;

            return clonedPublishedProviderResult;
        }

        private async Task<(PublishedProviderResult, long)> ProfileResult(FetchProviderProfilingMessageItem messageItem)
        {
            PublishedProviderResult result = await _publishedProviderResultsRepository.GetPublishedProviderResultForId(messageItem.AllocationLineResultId, messageItem.ProviderId);

            if (result == null)
            {
                _logger.Error("Could not find published provider result with id '{id}'", messageItem.AllocationLineResultId);
                throw new NonRetriableException($"Published provider result with id '{messageItem.AllocationLineResultId}' not found");
            }

            if (result.FundingPeriod.Id.Length != 4)
            {
                throw new NonRetriableException($"FundingPeriod.ID length is not 4 characters, this is unsupported by profiling as it breaks the convention of FundingStreamPeriod. Value is = '{result.FundingPeriod?.Id}'");
            }

            ProviderProfilingRequestModel providerProfilingRequestModel = new ProviderProfilingRequestModel
            {
                FundingStreamPeriod = result.FundingStreamResult.FundingStreamPeriod,
                AllocationValueByDistributionPeriod = new[]
                {
                        new Common.ApiClient.Profiling.Models.AllocationPeriodValue
                        {
                            DistributionPeriod =result.FundingStreamResult.DistributionPeriod,
                            AllocationValue = (decimal)result.FundingStreamResult.AllocationLineResult.Current.Value
                        }
                    }
            };

            Stopwatch profilingApiStopWatch = Stopwatch.StartNew();

            ValidatedApiResponse<ProviderProfilingResponseModel> responseModel = await _providerProfilingRepositoryPolicy.ExecuteAsync(() => _providerProfilingRepository.GetProviderProfilePeriods(providerProfilingRequestModel));

            profilingApiStopWatch.Stop();

            if (responseModel != null && responseModel.StatusCode == HttpStatusCode.OK && !responseModel.Content.DeliveryProfilePeriods.IsNullOrEmpty())
            {
                result.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods = _mapper.Map<Models.Results.ProfilingPeriod[]>(responseModel.Content.DeliveryProfilePeriods);
                result.FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes = _mapper.Map<Models.Results.FinancialEnvelope[]>(responseModel.Content.FinancialEnvelopes);

                return (result, profilingApiStopWatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.Error($"Failed to obtain profiling periods for provider: {result.ProviderId} and period: {result.FundingPeriod.Name}. Status Code = '{responseModel?.StatusCode}'");

                throw new RetriableException($"Failed to obtain profiling periods for provider: {result.ProviderId} and period: {result.FundingPeriod.Name}. Status Code = '{responseModel?.StatusCode}'");
            }
        }

        private bool CanUpdateAllocationLineResult(PublishedAllocationLineResult allocationLineResult, AllocationLineStatus newStatus)
        {
            if (allocationLineResult == null || allocationLineResult.Current.Status == newStatus
                || (allocationLineResult.Current.Status == AllocationLineStatus.Held && newStatus == AllocationLineStatus.Published)
                || (allocationLineResult.Current.Status == AllocationLineStatus.Approved && newStatus == AllocationLineStatus.Held)
                || (allocationLineResult.Current.Status == AllocationLineStatus.Published && newStatus == AllocationLineStatus.Held)
                || (allocationLineResult.Current.Status == AllocationLineStatus.Published && newStatus == AllocationLineStatus.Approved))
            {
                return false;
            }

            return true;
        }

        private async Task SavePublishedAllocationLineResultVersionHistory(IEnumerable<PublishedProviderResult> publishedProviderResults)
        {
            IEnumerable<PublishedAllocationLineResultVersion> historyResultsToSave = new List<PublishedAllocationLineResultVersion>();

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                PublishedAllocationLineResult publishedAllocationLineResult = publishedProviderResult.FundingStreamResult.AllocationLineResult;

                historyResultsToSave = historyResultsToSave.Concat(new[] { publishedAllocationLineResult.Current });
            }

            if (historyResultsToSave.Any())
            {
                IEnumerable<KeyValuePair<string, PublishedAllocationLineResultVersion>> history = historyResultsToSave.Select(m => new KeyValuePair<string, PublishedAllocationLineResultVersion>(m.ProviderId, m));

                await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsVersionRepository.SaveVersions(history));
            }
        }

        private async Task UpdateAllocationNotificationsFeedIndex(IEnumerable<PublishedProviderResult> publishedProviderResults, SpecificationCurrentVersion specification, bool checkProfiling = false)
        {
            IEnumerable<AllocationNotificationFeedIndex> notifications = BuildAllocationNotificationIndexItems(publishedProviderResults, specification, checkProfiling);

            if (notifications.Any())
            {
                IEnumerable<IndexError> errors = await _allocationNotificationsSearchRepositoryPolicy.ExecuteAsync(() => _allocationNotificationsSearchRepository.Index(notifications));

                if (errors.Any())
                {
                    string errorMessage = $"Failed to index allocation notification feed documents with errors: { string.Join(";", errors.Select(m => m.ErrorMessage)) }";
                    _logger.Error(errorMessage);

                    throw new NonRetriableException(errorMessage);
                }
            }
        }

        private IEnumerable<AllocationNotificationFeedIndex> BuildAllocationNotificationIndexItems(IEnumerable<PublishedProviderResult> publishedProviderResults, SpecificationCurrentVersion specification, bool checkProfiling = false)
        {
            Guard.ArgumentNotNull(publishedProviderResults, nameof(publishedProviderResults));

            Guard.ArgumentNotNull(specification, nameof(specification));

            IList<AllocationNotificationFeedIndex> notifications = new List<AllocationNotificationFeedIndex>();

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                if (publishedProviderResult.FundingStreamResult == null || publishedProviderResult.FundingStreamResult.AllocationLineResult == null || publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Status == AllocationLineStatus.Held)
                {
                    continue;
                }

                string providerProfiles = "[]";

                if (checkProfiling && publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.IsNullOrEmpty())
                {
                    string message = $"Provider result with id {publishedProviderResult.Id} and provider id {publishedProviderResult.ProviderId} contains no profiling periods";

                    _logger.Error(message);

                    throw new MissingProviderProfilesException(publishedProviderResult.Id, publishedProviderResult.ProviderId);
                }
                else
                {
                    if (!publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods.IsNullOrEmpty())
                    {
                        providerProfiles = JsonConvert.SerializeObject(publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.ProfilingPeriods);
                    }
                }

                AllocationNotificationFeedIndex feedIndex = new AllocationNotificationFeedIndex
                {
                    Title = $"Allocation {publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Name} was {publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Status}",
                    Summary = publishedProviderResult.Summary,
                    DatePublished = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Status == AllocationLineStatus.Published
                        ? publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Date : (DateTimeOffset?)null,
                    DateUpdated = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Date,
                    FundingStreamId = publishedProviderResult.FundingStreamResult.FundingStream.Id,
                    FundingStreamName = publishedProviderResult.FundingStreamResult.FundingStream.Name,
                    FundingStreamShortName = publishedProviderResult.FundingStreamResult.FundingStream.ShortName,
                    FundingStreamPeriodId = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType?.Id,
                    FundingStreamStartDay = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType.StartDay,
                    FundingStreamStartMonth = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType.StartMonth,
                    FundingStreamEndDay = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType.EndDay,
                    FundingStreamEndMonth = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType.EndMonth,
                    FundingStreamPeriodName = publishedProviderResult.FundingStreamResult.FundingStream.PeriodType?.Name,
                    FundingPeriodId = publishedProviderResult.FundingPeriod.Id,
                    FundingPeriodStartYear = publishedProviderResult.FundingPeriod.StartYear,
                    FundingPeriodEndYear = publishedProviderResult.FundingPeriod.EndYear,
                    ProviderId = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.Id,
                    ProviderUkPrn = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.UKPRN,
                    ProviderUpin = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.UPIN,
                    ProviderUrn = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.URN,
                    ProviderOpenDate = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.DateOpened,
                    ProviderClosedDate = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.DateClosed,
                    AllocationLineId = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Id,
                    AllocationLineName = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Name,
                    AllocationVersionNumber = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Version,
                    AllocationStatus = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Status.ToString(),
                    AllocationAmount = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Value.HasValue
                                            ? Convert.ToDouble(publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Value) : 0,
                    AllocationLineContractRequired = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.IsContractRequired,
                    AllocationLineFundingRoute = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.FundingRoute.ToString(),
                    AllocationLineShortName = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.ShortName,
                    ProviderProfiling = providerProfiles,
                    ProviderName = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.Name,
                    LaCode = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.LACode,
                    Authority = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.Authority,
                    ProviderType = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.ProviderType,
                    SubProviderType = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.ProviderSubType,
                    EstablishmentNumber = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.EstablishmentNumber,
                    CrmAccountId = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.CrmAccountId,
                    NavVendorNo = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.NavVendorNo,
                    DfeEstablishmentNumber = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.DfeEstablishmentNumber,
                    ProviderStatus = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.Status,
                    PolicySummaries = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Calculations != null ?
                        JsonConvert.SerializeObject(CreatePolicySummaries(publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Calculations, specification)) : "",
                    Calculations = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Calculations != null ?
                        JsonConvert.SerializeObject(publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Calculations.Select(m =>
                           new PublishedProviderCalculationResultSummary
                           {
                               CalculationName = m.CalculationSpecification.Name,
                               CalculationDisplayName = m.CalculationType == PublishedCalculationType.Baseline ? m.CalculationType.ToString() : m.CalculationSpecification.Name,
                               CalculationVersion = m.CalculationVersion,
                               CalculationType = m.CalculationType.ToString(),
                               CalculationAmount = m.Value,
                               AllocationLineId = publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Id,
                               PolicyId = m.Policy.Id,
                               PolicyName = m.Policy.Name
                           })) : "",
                    FinancialEnvelopes = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes != null ?
                        JsonConvert.SerializeObject(publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.FinancialEnvelopes) : ""
                };

                if (publishedProviderResult.FundingStreamResult.AllocationLineResult.HasResultBeenVaried)
                {
                    PublishedAllocationLineResultVersion publishedAllocationLineResultVersion = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current;
                    ProviderSummary currentProvider = publishedAllocationLineResultVersion.Provider;

                    feedIndex.Successors = !string.IsNullOrWhiteSpace(currentProvider.Successor) ? new[] { currentProvider.Successor } : null;
                    feedIndex.OpenReason = currentProvider.ReasonEstablishmentOpened;
                    feedIndex.CloseReason = currentProvider.ReasonEstablishmentClosed;
                    feedIndex.Predecessors = publishedAllocationLineResultVersion.Predecessors?.ToArray();
                }

                if (_featureToggle.IsProviderVariationsEnabled())
                {
                    PublishedAllocationLineResultVersion publishedAllocationLineResultVersion = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current;
                    feedIndex.VariationReasons = publishedAllocationLineResultVersion.VariationReasons?.Select(vr => vr.ToString()).ToArray();
                }

                if (_featureToggle.IsAllocationLineMajorMinorVersioningEnabled())
                {
                    feedIndex.MajorVersion = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Major;
                    feedIndex.MinorVersion = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Minor;
                }

                if (_featureToggle.IsAllAllocationResultsVersionsInFeedIndexEnabled())
                {
                    feedIndex.Id = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.FeedIndexId;
                    feedIndex.IsDeleted = false;
                }
                else
                {
                    feedIndex.Id = publishedProviderResult.Id;
                }

                notifications.Add(feedIndex);
            }

            return notifications;
        }

        private IEnumerable<PublishedProviderResultsPolicySummary> CreatePolicySummaries(IEnumerable<PublishedProviderCalculationResult> calculationResults, SpecificationCurrentVersion specification)
        {
            IList<PublishedProviderResultsPolicySummary> policySummaries = new List<PublishedProviderResultsPolicySummary>();

            foreach (Models.Specs.Policy policy in specification.Policies)
            {
                PublishedProviderResultsPolicySummary publishedProviderResultsPolicySummary = new PublishedProviderResultsPolicySummary
                {
                    Policy = new PolicySummary(policy.Id, policy.Name, policy.Description),
                    Calculations = AddCalculationSummaries(policy, calculationResults).ToArraySafe(),
                };

                foreach (Models.Specs.Policy subPolicy in policy.SubPolicies)
                {
                    PublishedProviderResultsPolicySummary publishedProviderResultsSubPolicySummary = new PublishedProviderResultsPolicySummary
                    {
                        Policy = new PolicySummary(subPolicy.Id, subPolicy.Name, subPolicy.Description),
                        Calculations = AddCalculationSummaries(subPolicy, calculationResults).ToArraySafe(),
                    };

                    publishedProviderResultsPolicySummary.Policies = publishedProviderResultsPolicySummary.Policies.Concat(new[] { publishedProviderResultsSubPolicySummary }).ToArraySafe();
                }

                policySummaries.Add(publishedProviderResultsPolicySummary);

            }

            return policySummaries;
        }

        private IEnumerable<PublishedProviderResultsCalculationSummary> AddCalculationSummaries(Policy policy, IEnumerable<PublishedProviderCalculationResult> calculationResults)
        {
            IEnumerable<PublishedProviderResultsCalculationSummary> calculationSummaries = Enumerable.Empty<PublishedProviderResultsCalculationSummary>();

            if (policy.Calculations.IsNullOrEmpty())
            {
                return calculationSummaries;
            }

            foreach (Calculation calculation in policy.Calculations)
            {
                if (calculation.CalculationType == CalculationType.Number && calculation.IsPublic == false)
                {
                    continue;
                }

                PublishedProviderCalculationResult publishedProviderCalculationResult = calculationResults.FirstOrDefault(m => m.CalculationSpecification.Id == calculation.Id);

                if (publishedProviderCalculationResult == null)
                {
                    continue;
                }

                PublishedProviderResultsCalculationSummary calculationSummary = new PublishedProviderResultsCalculationSummary
                {
                    Amount = publishedProviderCalculationResult.Value.HasValue ? publishedProviderCalculationResult.Value.Value : 0,
                    CalculationType = publishedProviderCalculationResult.CalculationType,
                    Name = publishedProviderCalculationResult.CalculationSpecification.Name,
                };

                calculationSummaries = calculationSummaries.Concat(new[] { calculationSummary });
            }

            return calculationSummaries;
        }

        private void UpdateCacheForSegmentDone(string specificationId, int percentageToSetTo, CalculationProgressStatus progressStatus, string message = null, DateTimeOffset? publishedResultsRefreshedAt = null, IEnumerable<PublishedProviderResult> publishedProviderResults = null)
        {
            // Only update the cache if not using the job service - UI will get job notifications instead to monitor progress
            SpecificationCalculationExecutionStatus calculationProgress = new SpecificationCalculationExecutionStatus(specificationId, percentageToSetTo, progressStatus)
            {
                ErrorMessage = message,
                PublishedResultsRefreshedAt = publishedResultsRefreshedAt
            };

            if (!publishedProviderResults.IsNullOrEmpty())
            {
                calculationProgress.NewCount = publishedProviderResults.Count(m => m.FundingStreamResult.AllocationLineResult.Current.Status == AllocationLineStatus.Held);
                calculationProgress.ApprovedCount = publishedProviderResults.Count(m => m.FundingStreamResult.AllocationLineResult.Current.Status == AllocationLineStatus.Approved);
                calculationProgress.UpdatedCount = publishedProviderResults.Count(m => m.FundingStreamResult.AllocationLineResult.Current.Status == AllocationLineStatus.Updated);
                calculationProgress.PublishedCount = publishedProviderResults.Count(m => m.FundingStreamResult.AllocationLineResult.Current.Status == AllocationLineStatus.Published);
            }

            CacheHelper.UpdateCacheForItem($"{CacheKeys.CalculationProgress}{calculationProgress.SpecificationId}", calculationProgress, _cacheProvider);
        }

        private void SetFeedIndexId(PublishedProviderResult publishedProviderResult)
        {
            publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.FeedIndexId
                = $"{publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Id}-{publishedProviderResult.FundingPeriod.Id}" +
                 $"-{publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.UKPRN}-v{publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Major}-{publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Minor}";
        }

        private void SetPublishedField(PublishedProviderResult publishedProviderResult)
        {
            if (publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Status == AllocationLineStatus.Published)
            {
                publishedProviderResult.FundingStreamResult.AllocationLineResult.Published = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Clone() as PublishedAllocationLineResultVersion;
            }
        }

        //temporary until migration work finished
        private void SetFeedIndexId(PublishedProviderResult publishedProviderResult, PublishedAllocationLineResultVersion version)
        {
            version.FeedIndexId
               = $"{publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Id}-{publishedProviderResult.FundingPeriod.Id}" +
                $"-{version.Provider.UKPRN}-v{version.Major}-{version.Minor}";
        }

        private async Task<JobViewModel> RetrieveJobAndCheckCanBeProcessed(string jobId)
        {
            ApiResponse<JobViewModel> response = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.GetJobById(jobId));

            if (response == null || response.Content == null)
            {
                _logger.Error($"Could not find the job with id: '{jobId}'");
                return null;
            }

            JobViewModel job = response.Content;

            if (job.CompletionStatus.HasValue)
            {
                _logger.Information($"Received job with id: '{jobId}' is already in a completed state with status {job.CompletionStatus.ToString()}");
                return null;
            }

            return job;
        }

        private async Task UpdateJobStatus(string jobId, int percentComplete = 0, bool? completedSuccessfully = null, string outcome = null)
        {
            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = completedSuccessfully,
                ItemsProcessed = percentComplete,
                Outcome = outcome
            };

            ApiResponse<JobLog> jobLogResponse = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(jobId, jobLogUpdateModel));

            if (jobLogResponse == null || jobLogResponse.Content == null)
            {
                _logger.Error($"Failed to add a job log for job id '{jobId}'");
            }
        }

        private async Task UpdateJobStatus(string jobId, int totalItemsCount, int failedItemsCount, bool? completedSuccessfully = null, string outcome = null)
        {
            JobLogUpdateModel jobLogUpdateModel = new JobLogUpdateModel
            {
                CompletedSuccessfully = completedSuccessfully,
                ItemsProcessed = totalItemsCount,
                ItemsFailed = failedItemsCount,
                ItemsSucceeded = totalItemsCount - failedItemsCount,
                Outcome = outcome
            };

            ApiResponse<JobLog> jobLogResponse = await _jobsApiClientPolicy.ExecuteAsync(() => _jobsApiClient.AddJobLog(jobId, jobLogUpdateModel));

            if (jobLogResponse == null || jobLogResponse.Content == null)
            {
                _logger.Error($"Failed to add a job log for job id '{jobId}'");
            }
        }
    }
}