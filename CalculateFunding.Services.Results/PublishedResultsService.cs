using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.FeatureToggles;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Models.HealthCheck;
using CalculateFunding.Models;
using CalculateFunding.Models.Exceptions;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Results.Messages;
using CalculateFunding.Models.Results.Search;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Constants;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Logging;
using CalculateFunding.Services.Core.Interfaces.ServiceBus;
using CalculateFunding.Services.Results.Interfaces;
using CalculateFunding.Services.Results.ResultModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.ServiceBus;
using Newtonsoft.Json;
using Serilog;

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
        private readonly IPublishedProviderCalculationResultsRepository _publishedProviderCalculationResultsRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly ISearchRepository<AllocationNotificationFeedIndex> _allocationNotificationsSearchRepository;
        private readonly Polly.Policy _allocationNotificationsSearchRepositoryPolicy;
        private readonly IProviderProfilingRepository _providerProfilingRepository;
        private readonly Polly.Policy _providerProfilingRepositoryPolicy;
        private readonly Polly.Policy _publishedProviderCalculationResultsRepositoryPolicy;
        private readonly Polly.Policy _publishedProviderResultsRepositoryPolicy;
        private readonly IMessengerService _messengerService;
        private readonly IVersionRepository<PublishedAllocationLineResultVersion> _publishedProviderResultsVersionRepository;
        private readonly IVersionRepository<PublishedProviderCalculationResultVersion> _publishedProviderCalcResultsVersionRepository;
        private readonly IPublishedAllocationLineLogicalResultVersionService _publishedAllocationLineLogicalResultVersionService;
        private readonly IFeatureToggle _featureToggle;

        public PublishedResultsService(ILogger logger,
          IMapper mapper,
          ITelemetry telemetry,
          ICalculationResultsRepository resultsRepository,
          ISpecificationsRepository specificationsRepository,
          IResultsResilliencePolicies resiliencePolicies,
          IPublishedProviderResultsAssemblerService publishedProviderResultsAssemblerService,
          IPublishedProviderResultsRepository publishedProviderResultsRepository,
          IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository,
          ICacheProvider cacheProvider,
          ISearchRepository<AllocationNotificationFeedIndex> allocationNotificationsSearchRepository,
          IMessengerService messengerService,
          IVersionRepository<PublishedAllocationLineResultVersion> publishedProviderResultsVersionRepository,
          IVersionRepository<PublishedProviderCalculationResultVersion> publishedProviderCalcResultsVersionRepository,
          IPublishedAllocationLineLogicalResultVersionService publishedAllocationLineLogicalResultVersionService,
          IFeatureToggle featureToggle)
        {
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(publishedProviderResultsAssemblerService, nameof(publishedProviderResultsAssemblerService));
            Guard.ArgumentNotNull(publishedProviderResultsRepository, nameof(publishedProviderResultsRepository));
            Guard.ArgumentNotNull(publishedProviderCalculationResultsRepository, nameof(publishedProviderCalculationResultsRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(allocationNotificationsSearchRepository, nameof(allocationNotificationsSearchRepository));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(publishedProviderResultsVersionRepository, nameof(publishedProviderResultsVersionRepository));
            Guard.ArgumentNotNull(publishedProviderCalcResultsVersionRepository, nameof(publishedProviderCalcResultsVersionRepository));
            Guard.ArgumentNotNull(publishedAllocationLineLogicalResultVersionService, nameof(publishedAllocationLineLogicalResultVersionService));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

            _logger = logger;
            _mapper = mapper;
            _specificationsRepositoryPolicy = resiliencePolicies.SpecificationsRepository;
            _resultsRepository = resultsRepository;
            _telemetry = telemetry;
            _resultsRepositoryPolicy = resiliencePolicies.ResultsRepository;
            _specificationsRepository = specificationsRepository;
            _publishedProviderResultsAssemblerService = publishedProviderResultsAssemblerService;
            _publishedProviderResultsRepository = publishedProviderResultsRepository;
            _publishedProviderCalculationResultsRepository = publishedProviderCalculationResultsRepository;
            _cacheProvider = cacheProvider;
            _allocationNotificationsSearchRepository = allocationNotificationsSearchRepository;
            _allocationNotificationsSearchRepositoryPolicy = resiliencePolicies.AllocationNotificationFeedSearchRepository;
            _publishedProviderCalculationResultsRepositoryPolicy = resiliencePolicies.PublishedProviderCalculationResultsRepository;
            _publishedProviderResultsRepositoryPolicy = resiliencePolicies.PublishedProviderResultsRepository;
            _messengerService = messengerService;
            _publishedProviderResultsVersionRepository = publishedProviderResultsVersionRepository;
            _publishedProviderCalcResultsVersionRepository = publishedProviderCalcResultsVersionRepository;
            _publishedAllocationLineLogicalResultVersionService = publishedAllocationLineLogicalResultVersionService;
            _featureToggle = featureToggle;
        }

        public PublishedResultsService(ILogger logger,
            IMapper mapper,
            ITelemetry telemetry,
            ICalculationResultsRepository resultsRepository,
            ISpecificationsRepository specificationsRepository,
            IResultsResilliencePolicies resiliencePolicies,
            IPublishedProviderResultsAssemblerService publishedProviderResultsAssemblerService,
            IPublishedProviderResultsRepository publishedProviderResultsRepository,
            IPublishedProviderCalculationResultsRepository publishedProviderCalculationResultsRepository,
            ICacheProvider cacheProvider,
            ISearchRepository<AllocationNotificationFeedIndex> allocationNotificationsSearchRepository,
            IProviderProfilingRepository providerProfilingRepository,
            IMessengerService messengerService,
            IVersionRepository<PublishedAllocationLineResultVersion> publishedProviderResultsVersionRepository,
            IVersionRepository<PublishedProviderCalculationResultVersion> publishedProviderCalcResultsVersionRepository,
            IPublishedAllocationLineLogicalResultVersionService publishedAllocationLineLogicalResultVersionService,
            IFeatureToggle featureToggle)
        {
            Guard.ArgumentNotNull(mapper, nameof(mapper));
            Guard.ArgumentNotNull(telemetry, nameof(telemetry));
            Guard.ArgumentNotNull(resultsRepository, nameof(resultsRepository));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));
            Guard.ArgumentNotNull(resiliencePolicies, nameof(resiliencePolicies));
            Guard.ArgumentNotNull(publishedProviderResultsAssemblerService, nameof(publishedProviderResultsAssemblerService));
            Guard.ArgumentNotNull(publishedProviderResultsRepository, nameof(publishedProviderResultsRepository));
            Guard.ArgumentNotNull(publishedProviderCalculationResultsRepository, nameof(publishedProviderCalculationResultsRepository));
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(allocationNotificationsSearchRepository, nameof(allocationNotificationsSearchRepository));
            Guard.ArgumentNotNull(providerProfilingRepository, nameof(providerProfilingRepository));
            Guard.ArgumentNotNull(messengerService, nameof(messengerService));
            Guard.ArgumentNotNull(publishedProviderResultsVersionRepository, nameof(publishedProviderResultsVersionRepository));
            Guard.ArgumentNotNull(publishedProviderCalcResultsVersionRepository, nameof(publishedProviderCalcResultsVersionRepository));
            Guard.ArgumentNotNull(publishedAllocationLineLogicalResultVersionService, nameof(publishedAllocationLineLogicalResultVersionService));
            Guard.ArgumentNotNull(featureToggle, nameof(featureToggle));

            _logger = logger;
            _mapper = mapper;
            _telemetry = telemetry;
            _resultsRepository = resultsRepository;
            _resultsRepositoryPolicy = resiliencePolicies.ResultsRepository;
            _specificationsRepository = specificationsRepository;
            _specificationsRepositoryPolicy = resiliencePolicies.SpecificationsRepository;
            _publishedProviderResultsAssemblerService = publishedProviderResultsAssemblerService;
            _publishedProviderResultsRepository = publishedProviderResultsRepository;
            _publishedProviderCalculationResultsRepository = publishedProviderCalculationResultsRepository;
            _cacheProvider = cacheProvider;
            _allocationNotificationsSearchRepository = allocationNotificationsSearchRepository;
            _allocationNotificationsSearchRepositoryPolicy = resiliencePolicies.AllocationNotificationFeedSearchRepository;
            _publishedProviderCalculationResultsRepositoryPolicy = resiliencePolicies.PublishedProviderCalculationResultsRepository;
            _publishedProviderResultsRepositoryPolicy = resiliencePolicies.PublishedProviderResultsRepository;
            _providerProfilingRepositoryPolicy = resiliencePolicies.ProviderProfilingRepository;
            _providerProfilingRepository = providerProfilingRepository;
            _messengerService = messengerService;
            _publishedProviderResultsVersionRepository = publishedProviderResultsVersionRepository;
            _publishedProviderCalcResultsVersionRepository = publishedProviderCalcResultsVersionRepository;
            _publishedAllocationLineLogicalResultVersionService = publishedAllocationLineLogicalResultVersionService;
            _featureToggle = featureToggle;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {				  
            ServiceHealth providerRepoHealth = await ((IHealthChecker)_publishedProviderResultsRepository).IsHealthOk();
            (bool Ok, string Message) cacheHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(ResultsService)
            };

            health.Dependencies.AddRange(providerRepoHealth.Dependencies);
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

            if(version == null)
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

            if (!message.UserProperties.ContainsKey("specification-id"))
            {
                _logger.Error("No specification Id was provided to PublishProviderResults");
                throw new ArgumentException("Message must contain a specification id");
            }

            int calculationProgress = 0;

            string specificationId = message.UserProperties["specification-id"].ToString();
            UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.InProgress);

            Stopwatch getSpecificationStopwatch = Stopwatch.StartNew();
            SpecificationCurrentVersion specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetCurrentSpecificationById(specificationId));
            getSpecificationStopwatch.Stop();

            UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);

            if (specification == null)
            {
                UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error, "specification not found");
                _logger.Error($"Specification not found for specification id {specificationId}");
                throw new ArgumentException($"Specification not found for specification id {specificationId}");
            }

            Stopwatch getCalculationResultsStopwatch = Stopwatch.StartNew();
            IEnumerable<ProviderResult> providerResults = await GetProviderResultsBySpecificationId(specificationId);
            getCalculationResultsStopwatch.Stop();
            UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);

            if (providerResults.IsNullOrEmpty())
            {
                UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error, "Could not find any provider results");
                _logger.Error($"Provider results not found for specification id {specificationId}");
                throw new ArgumentException("Could not find any provider results for specification");
            }

            Reference author = message.GetUserDetails();

            Stopwatch assemblePublishedCalculationResults = Stopwatch.StartNew();
            IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults = _publishedProviderResultsAssemblerService.AssemblePublishedCalculationResults(providerResults, author, specification);
            assemblePublishedCalculationResults.Stop();

            UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);

            Stopwatch saveCalculationResultsStopwatch = new Stopwatch();
            Stopwatch saveCalculationResultsHistoryStopwatch = new Stopwatch();

            try
            {
                saveCalculationResultsStopwatch.Start();
                await _publishedProviderCalculationResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderCalculationResultsRepository.CreatePublishedCalculationResults(publishedProviderCalculationResults.ToList()));
                saveCalculationResultsStopwatch.Stop();
                UpdateCacheForSegmentDone(specificationId, calculationProgress += 7, CalculationProgressStatus.InProgress);

                saveCalculationResultsHistoryStopwatch.Start();
                await SavePublishedCalculationResultVersionHistory(publishedProviderCalculationResults);
                saveCalculationResultsHistoryStopwatch.Stop();
                UpdateCacheForSegmentDone(specificationId, calculationProgress += 10, CalculationProgressStatus.InProgress);
            }
            catch (Exception ex)
            {
                UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error, "Failed to create published provider calculation results");
                _logger.Error(ex, $"Failed to create published provider calculation results for specification: {specificationId}");
                throw new Exception($"Failed to create published provider calculation results for specification: {specificationId}", ex);
            }

            Stopwatch assemblePublishedProviderResultsStopwatch = Stopwatch.StartNew();
            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsAssemblerService.AssemblePublishedProviderResults(providerResults, author, specification);
            assemblePublishedProviderResultsStopwatch.Stop();
            UpdateCacheForSegmentDone(specificationId, calculationProgress += 53, CalculationProgressStatus.InProgress);

            Stopwatch existingPublishedProviderResultsStopwatch = Stopwatch.StartNew();
            IEnumerable<PublishedProviderResultExisting> existingPublishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetExistingPublishedProviderResultsForSpecificationId(specificationId));
            existingPublishedProviderResultsStopwatch.Stop();

            List<PublishedProviderResult> publishedProviderResultsToSave = new List<PublishedProviderResult>();

            Stopwatch assembleSaveAndExcludeStopwatch = Stopwatch.StartNew();
            (IEnumerable<PublishedProviderResult> newOrUpdatedPublishedProviderResults, IEnumerable<PublishedProviderResultExisting> existingRecordsToZero) =
                await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsAssemblerService.GeneratePublishedProviderResultsToSave(publishedProviderResults, existingPublishedProviderResults));
            assembleSaveAndExcludeStopwatch.Stop();

            if (newOrUpdatedPublishedProviderResults.AnyWithNullCheck())
            {
                publishedProviderResultsToSave.AddRange(newOrUpdatedPublishedProviderResults);
            }

            // When the assembly doesn't return an allocation line result for a provider and it already exists, set to value to 0 and update the status to Updated
            Stopwatch existingRecordsToZeroStopwatch = Stopwatch.StartNew();
            if (existingRecordsToZero.AnyWithNullCheck())
            {
                IEnumerable<PublishedProviderResult> recordsToZero = await FetchAndCheckExistingRecordsToZero(existingRecordsToZero);
                if (recordsToZero.AnyWithNullCheck())
                {
                    publishedProviderResultsToSave.AddRange(recordsToZero);
                }
            }
            existingPublishedProviderResultsStopwatch.Stop();

            Stopwatch savePublishedResultsStopwatch = new Stopwatch();
            Stopwatch savePublishedResultsHistoryStopwatch = new Stopwatch();
            Stopwatch savePublishedResultsSearchStopwatch = new Stopwatch();

            if (publishedProviderResultsToSave.Any())
            {
                publishedProviderResultsToSave.ForEach(m => _publishedAllocationLineLogicalResultVersionService.SetVersion(m.FundingStreamResult.AllocationLineResult.Current));

                publishedProviderResultsToSave.ForEach(SetFeedIndexId);
                
                try
                {
                    savePublishedResultsStopwatch.Start();
                    await _publishedProviderResultsRepository.SavePublishedResults(publishedProviderResultsToSave);
                    savePublishedResultsStopwatch.Stop();
                    UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);

                    savePublishedResultsHistoryStopwatch.Start();
                    await SavePublishedAllocationLineResultVersionHistory(publishedProviderResultsToSave);
                    savePublishedResultsHistoryStopwatch.Stop();
                    UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);

                    savePublishedResultsSearchStopwatch.Start();
                    await UpdateAllocationNotificationsFeedIndex(publishedProviderResultsToSave, specification);
                    savePublishedResultsSearchStopwatch.Stop();
                    UpdateCacheForSegmentDone(specificationId, calculationProgress += 5, CalculationProgressStatus.InProgress);
                }
                catch (Exception ex)
                {
                    UpdateCacheForSegmentDone(specificationId, calculationProgress, CalculationProgressStatus.Error, "Failed to create published provider results");
                    _logger.Error(ex, $"Failed to create published provider results for specification: {specificationId}");
                    throw new Exception($"Failed to create published provider results for specification: {specificationId}", ex);
                }
            }

            // Update the specification to store when this refresh happened
            DateTimeOffset publishedResultsRefreshedAt = DateTimeOffset.Now;
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

            IDictionary<string, double> metrics = new Dictionary<string, double>()
                    {
                        { "publishproviderresults-getCalculationResultsMs", getCalculationResultsStopwatch.ElapsedMilliseconds },
                        { "publishproviderresults-getSpecificationMs", getSpecificationStopwatch.ElapsedMilliseconds },
                        { "publishproviderresults-assemblePublishedCalculationResultsMs", assemblePublishedCalculationResults.ElapsedMilliseconds },
                        { "publishproviderresults-assemblePublishedCalculationResultsTotal", publishedProviderCalculationResults.Count() },
                        { "publishproviderresults-saveCalculationResultsMs", saveCalculationResultsStopwatch.ElapsedMilliseconds },
                        { "publishproviderresults-saveCalculationResultsHistoryMs", saveCalculationResultsHistoryStopwatch.ElapsedMilliseconds },
                        { "publishproviderresults-assemblePublishedProviderResultsMs", assemblePublishedProviderResultsStopwatch.ElapsedMilliseconds },
                        { "publishproviderresults-existingPublishedProviderResultsMs", existingPublishedProviderResultsStopwatch.ElapsedMilliseconds },
                        { "publishproviderresults-assembleSaveAndExcludeMs", assembleSaveAndExcludeStopwatch.ElapsedMilliseconds },
                        { "publishproviderresults-savePublishedResultsCount", publishedProviderResultsToSave.Count },
                        { "publishproviderresults-savePublishedCalculationsResultsCount", publishedProviderCalculationResults.Count() },
                    };

            if (publishedProviderResultsToSave.Any())
            {
                metrics.Add("publishproviderresults-savePublishedResultsMs", savePublishedResultsStopwatch.ElapsedMilliseconds);
                metrics.Add("publishproviderresults-savePublishedResultsHistoryMs", savePublishedResultsHistoryStopwatch.ElapsedMilliseconds);
                metrics.Add("publishproviderresults-savePublishedResultsSearchMs", savePublishedResultsSearchStopwatch.ElapsedMilliseconds);
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

            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultsForSpecificationId(specificationId));

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

            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultsByFundingPeriodIdAndSpecificationIdAndFundingStreamId(fundingPeriodId, specificationId, fundingStreamId));

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

            IEnumerable<string> providerIds = updateStatusModel.Providers.Select(p => p.ProviderId).Distinct();
            IEnumerable<PublishedProviderResult> publishedProviderResults = await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.GetPublishedProviderResultsForSpecificationIdAndProviderId(specificationId, providerIds));

            if (publishedProviderResults.IsNullOrEmpty())
            {
                return new NotFoundObjectResult($"No provider results to update for specification id: {specificationId}");
            }

            try
            {
                Tuple<int, int> updateCounts = await UpdateAllocationLineResultsStatus(publishedProviderResults.ToList(), updateStatusModel, request, specificationId);

                return new OkObjectResult(new UpdateAllocationResultsStatusCounts { UpdatedAllocationLines = updateCounts.Item1, UpdatedProviderIds = updateCounts.Item2 });
            }
            catch (Exception ex)
            {
                _logger.Error(ex, "Failed to update result status's");
                return new InternalServerErrorResult(ex.Message);
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
                IEnumerable<string> specificationIds = publishedProviderResults.DistinctBy(m => m.SpecificationId).Select(m => m.SpecificationId);

                foreach (string specificationId in specificationIds)
                {
                    SpecificationCurrentVersion specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetCurrentSpecificationById(specificationId));

                    try
                    {
                        await UpdateAllocationNotificationsFeedIndex(publishedProviderResults, specification);
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

        IEnumerable<PublishedProviderResultModel> MapPublishedProviderResultModels(IEnumerable<PublishedProviderResult> publishedProviderResults)
        {
            if (publishedProviderResults.IsNullOrEmpty())
            {
                return Enumerable.Empty<PublishedProviderResultModel>();
            }

            IList<PublishedProviderResultModel> results = new List<PublishedProviderResultModel>();

            IEnumerable<IGrouping<string, PublishedProviderResult>> providerResultsGroups = publishedProviderResults.GroupBy(m => m.ProviderId);

            foreach (IGrouping<string, PublishedProviderResult> providerResultGroup in providerResultsGroups)
            {
                IEnumerable<PublishedFundingStreamResult> fundingStreamResults = providerResultGroup.Select(m => m.FundingStreamResult);

                PublishedProviderResult providerResult = providerResultGroup.First();

                PublishedProviderResultModel publishedProviderResultModel = new PublishedProviderResultModel
                {
                    ProviderId = providerResult.ProviderId,
                    ProviderName = providerResult.FundingStreamResult.AllocationLineResult.Current.Provider?.Name,
                    ProviderType = providerResult.FundingStreamResult.AllocationLineResult.Current.Provider?.ProviderType
                };

                if (!results.Any(m => m.ProviderId == providerResultGroup.Key))
                {
                    results.Add(publishedProviderResultModel);
                }

                IEnumerable<IGrouping<string, PublishedFundingStreamResult>> fundingStreamResultsGroups = fundingStreamResults.GroupBy(m => m.FundingStream.Id);

                foreach (IGrouping<string, PublishedFundingStreamResult> fundingStreamResultsGroup in fundingStreamResultsGroups)
                {
                    IEnumerable<PublishedAllocationLineResult> allocationLineResults = fundingStreamResultsGroup.Select(m => m.AllocationLineResult);

                    PublishedFundingStreamResult fundingStreamResult = fundingStreamResultsGroup.First();

                    if (!publishedProviderResultModel.FundingStreamResults.Any(m => m.FundingStreamId == fundingStreamResultsGroup.Key))
                    {
                        publishedProviderResultModel.FundingStreamResults = publishedProviderResultModel.FundingStreamResults.Concat(new[] { new PublishedFundingStreamResultModel
                        {
                            FundingStreamId = fundingStreamResult.FundingStream.Id,
                            FundingStreamName = fundingStreamResult.FundingStream.Name,
                            AllocationLineResults = allocationLineResults.IsNullOrEmpty() ? Enumerable.Empty<PublishedAllocationLineResultModel>() : allocationLineResults.Select(
                                        alr => new PublishedAllocationLineResultModel
                                        {
                                            AllocationLineId = alr.AllocationLine.Id,
                                            AllocationLineName = alr.AllocationLine.Name,
                                            FundingAmount = alr.Current.Value,
                                            Status = alr.Current.Status,
                                            LastUpdated = alr.Current.Date,
                                            Authority = alr.Current.Provider.Authority,
                                            Version = string.IsNullOrWhiteSpace(alr.Current.VersionNumber) ? "n/a" : alr.Current.VersionNumber
                                        }
                                    ).OrderBy(r => r.AllocationLineName)
                        } });
                    }

                }
            }

            return results.OrderBy(r => r.ProviderName);
        }

        private async Task<Tuple<int, int>> UpdateAllocationLineResultsStatus(IEnumerable<PublishedProviderResult> publishedProviderResults,
            UpdatePublishedAllocationLineResultStatusModel updateStatusModel, HttpRequest request, string specificationId)
        {
            IList<string> updatedAllocationLineIds = new List<string>();
            IList<string> updatedProviderIds = new List<string>();
            IList<PublishedProviderResult> resultsToProfile = new List<PublishedProviderResult>();

            List<PublishedProviderResult> resultsToUpdate = new List<PublishedProviderResult>();
            List<PublishedAllocationLineResultVersion> historyToSave = new List<PublishedAllocationLineResultVersion>();

            Reference author = request.GetUser();

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
                            newVersion.Date = DateTimeOffset.Now.ToLocalTime();
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
                        result.Title = $"Allocation {result.FundingStreamResult.AllocationLineResult.AllocationLine.Name} was {result.FundingStreamResult.AllocationLineResult.Current.Status.ToString()}";

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
                SpecificationCurrentVersion specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetCurrentSpecificationById(specificationId));

                try
                {
                    resultsToUpdate.ForEach(SetFeedIndexId);

                    await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsRepository.SavePublishedResults(resultsToUpdate));

                    IEnumerable<KeyValuePair<string, PublishedAllocationLineResultVersion>> history = historyToSave.Select(m => new KeyValuePair<string, PublishedAllocationLineResultVersion>(m.ProviderId, m));

                    await _publishedProviderResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderResultsVersionRepository.SaveVersions(history));

                    await UpdateAllocationNotificationsFeedIndex(resultsToUpdate, specification);
                }
                catch (Exception ex)
                {
                    _logger.Error(ex, "Failed when updating allocation line results");

                    throw new Exception("Failed when updating allocation line results");
                }
            }

            await GetProfilingPeriods(request, resultsToProfile, specificationId);

            return new Tuple<int, int>(updatedAllocationLineIds.Count, updatedProviderIds.Count);
        }

        private async Task GetProfilingPeriods(HttpRequest request, IEnumerable<PublishedProviderResult> resultsToProfile, string specificationId)
        {
            int batchSize = 100;
            int startPosition = 0;
            while (startPosition < resultsToProfile.Count())
            {
                IEnumerable<FetchProviderProfilingMessageItem> batchOfResultsToProfile = resultsToProfile.Skip(startPosition).Take(batchSize).Select(r => new FetchProviderProfilingMessageItem { AllocationLineResultId = r.Id, ProviderId = r.ProviderId });

                _logger.Information($"Sending new provider profiling message for {batchOfResultsToProfile.Count()} results");

                IDictionary<string, string> properties = request.BuildMessageProperties();
                properties["specification-id"] = specificationId;

                await _messengerService.SendToQueue(ServiceBusConstants.QueueNames.FetchProviderProfile, batchOfResultsToProfile, properties);

                _logger.Information($"Sent new provider profiling message for {batchOfResultsToProfile.Count()} results");

                startPosition += batchOfResultsToProfile.Count();
            }
        }

        public async Task FetchProviderProfile(Message message)
        {
            Stopwatch profilingStopWatch = Stopwatch.StartNew();

            Guard.ArgumentNotNull(message, nameof(message));

            if (!message.UserProperties.ContainsKey("specification-id"))
            {
                _logger.Error("No specification id was present on the message");
                throw new ArgumentException("Message must contain a specification id in user properties");
            }

            string specificationId = message.UserProperties["specification-id"].ToString();

            IEnumerable<FetchProviderProfilingMessageItem> data = message.GetPayloadAsInstanceOf<IEnumerable<FetchProviderProfilingMessageItem>>();

            if (data.IsNullOrEmpty())
            {
                _logger.Error("No allocation result profiling items were present in the message");
                throw new ArgumentException("Message must contain a collection of allocation results profiling items");
            }

            SpecificationCurrentVersion specification = await _specificationsRepositoryPolicy.ExecuteAsync(() => _specificationsRepository.GetCurrentSpecificationById(specificationId));

            if (specification == null)
            {
                _logger.Error($"A specification could not be found with id {specificationId}");

                throw new ArgumentException($"Could not find a specification with id {specificationId}");
            }

            ConcurrentBag<PublishedProviderResult> publishedProviderResults = new ConcurrentBag<PublishedProviderResult>();

            IList<Task> profilingTasks = new List<Task>();

            long totalMsForProfilingApi = 0;

            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 15);
            foreach (FetchProviderProfilingMessageItem profilingItem in data)
            {
                await throttler.WaitAsync();
                profilingTasks.Add(
                    Task.Run(() =>
                    {
                        try
                        {
                            (PublishedProviderResult publishedProviderResult, long timeInMs) profilingResult = ProfileResult(profilingItem).Result;

                            publishedProviderResults.Add(profilingResult.publishedProviderResult);

                            totalMsForProfilingApi += profilingResult.timeInMs;
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
                                version.Title = $"Allocation {publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Name} was {version.Status.ToString()}";
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

            foreach(SpecificationSummary specificationSummary in specificationSummaries)
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
                throw new ArgumentException($"Published provider result with id '{messageItem.AllocationLineResultId}' not found");
            }

            ProviderProfilingRequestModel providerProfilingRequestModel = new ProviderProfilingRequestModel
            {
                FundingStreamPeriod = result.FundingStreamResult.FundingStreamPeriod,
                AllocationValueByDistributionPeriod = new[]
                {
                        new AllocationPeriodValue
                        {
                            DistributionPeriod =result.FundingStreamResult.DistributionPeriod,
                            AllocationValue = (decimal)result.FundingStreamResult.AllocationLineResult.Current.Value
                        }
                    }
            };

            Stopwatch profilingApiStopWatch = Stopwatch.StartNew();

            ProviderProfilingResponseModel responseModel = await _providerProfilingRepositoryPolicy.ExecuteAsync(() => _providerProfilingRepository.GetProviderProfilePeriods(providerProfilingRequestModel));

            profilingApiStopWatch.Stop();

            if (responseModel != null && !responseModel.DeliveryProfilePeriods.IsNullOrEmpty())
            {
                result.ProfilingPeriods = responseModel.DeliveryProfilePeriods.ToArraySafe();

                return (result, profilingApiStopWatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.Error($"Failed to obtain profiling periods for provider: {result.ProviderId} and period: {result.FundingPeriod.Name}");

                throw new Exception($"Failed to obtain profiling periods for provider: {result.ProviderId} and period: {result.FundingPeriod.Name}");
            }
        }

        bool CanUpdateAllocationLineResult(PublishedAllocationLineResult allocationLineResult, AllocationLineStatus newStatus)
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

        async Task SavePublishedAllocationLineResultVersionHistory(IEnumerable<PublishedProviderResult> publishedProviderResults)
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

        async Task SavePublishedCalculationResultVersionHistory(IEnumerable<PublishedProviderCalculationResult> publishedProviderCalculationResults)
        {
            IList<PublishedProviderCalculationResultVersion> historyResultsToSave = new List<PublishedProviderCalculationResultVersion>();

            foreach (PublishedProviderCalculationResult publishedProviderCalculationResult in publishedProviderCalculationResults)
            {
                if (publishedProviderCalculationResult.Current != null)
                {
                    historyResultsToSave.Add(publishedProviderCalculationResult.Current);
                }
            }

            if (!historyResultsToSave.IsNullOrEmpty())
            {
                IEnumerable<KeyValuePair<string, PublishedProviderCalculationResultVersion>> history = historyResultsToSave.Select(m => new KeyValuePair<string, PublishedProviderCalculationResultVersion>(m.ProviderId, m));

                await _publishedProviderCalculationResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderCalcResultsVersionRepository.SaveVersions(history));
            }
        }

        async Task
            UpdateAllocationNotificationsFeedIndex(IEnumerable<PublishedProviderResult> publishedProviderResults, SpecificationCurrentVersion specification, bool checkProfiling = false)
        {
            IEnumerable<AllocationNotificationFeedIndex> notifications = await BuildAllocationNotificationIndexItems(publishedProviderResults, specification, checkProfiling);

            if (notifications.Any())
            {
                IEnumerable<IndexError> errors = await _allocationNotificationsSearchRepositoryPolicy.ExecuteAsync(() => _allocationNotificationsSearchRepository.Index(notifications));

                if (errors.Any())
                {
                    string errorMessage = $"Failed to index allocation notification feed documents with errors: { string.Join(";", errors.Select(m => m.ErrorMessage)) }";
                    _logger.Error(errorMessage);

                    throw new Exception(errorMessage);
                }
            }
        }

        async Task<IEnumerable<AllocationNotificationFeedIndex>> BuildAllocationNotificationIndexItems(IEnumerable<PublishedProviderResult> publishedProviderResults, SpecificationCurrentVersion specification, bool checkProfiling = false)
        {
            Guard.ArgumentNotNull(publishedProviderResults, nameof(publishedProviderResults));

            Guard.ArgumentNotNull(specification, nameof(specification));

            IList<AllocationNotificationFeedIndex> notifications = new List<AllocationNotificationFeedIndex>();

            Stopwatch fetchCalcResultsStopwatch = new Stopwatch();
            fetchCalcResultsStopwatch.Start();
            IEnumerable<string> providerIds = publishedProviderResults.Select(r => r.ProviderId).Distinct();
            IEnumerable<PublishedProviderCalculationResult> calculationResults = (await _publishedProviderCalculationResultsRepositoryPolicy.ExecuteAsync(() => _publishedProviderCalculationResultsRepository.GetFundingOrPublicPublishedProviderCalculationResultsBySpecificationIdAndProviderId(specification.Id, providerIds))).ToList();
            fetchCalcResultsStopwatch.Stop();
            _telemetry.TrackEvent("Fetching Published Calculation Results",
                new Dictionary<string, string>
                {
                    { "specificationId", specification.Id }
                },
                new Dictionary<string, double>
                {
                    { "provider-batch-size", providerIds.Count() },
                    { "calculation-results-found", calculationResults.Count() },
                    { "duration", fetchCalcResultsStopwatch.ElapsedMilliseconds }
                });

            foreach (PublishedProviderResult publishedProviderResult in publishedProviderResults)
            {
                if (publishedProviderResult.FundingStreamResult == null || publishedProviderResult.FundingStreamResult.AllocationLineResult == null || publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Status == AllocationLineStatus.Held)
                {
                    continue;
                }

                string providerProfiles = "[]";

                if (checkProfiling && publishedProviderResult.ProfilingPeriods.IsNullOrEmpty())
                {
                    string message = $"Provider result with id {publishedProviderResult.Id} and provider id {publishedProviderResult.ProviderId} contains no profiling periods";

                    _logger.Error(message);

                    throw new MissingProviderProfilesException(publishedProviderResult.Id, publishedProviderResult.ProviderId);
                }
                else
                {
                    providerProfiles = JsonConvert.SerializeObject(publishedProviderResult.ProfilingPeriods);
                }

                IEnumerable<PublishedProviderCalculationResult> providerCalculationResults = calculationResults.Where(m => m.ProviderId == publishedProviderResult.ProviderId);

                AllocationNotificationFeedIndex feedIndex = new AllocationNotificationFeedIndex
                {
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
                    ProviderClosedDate = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Provider.DateOpened,
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
                    PolicySummaries = JsonConvert.SerializeObject(CreatePolicySummaries(providerCalculationResults, specification))

                };

                if (_featureToggle.IsAllocationLineMajorMinorVersioningEnabled())
                {
                    feedIndex.MajorVersion = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Major;
                    feedIndex.MinorVersion = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Minor;
                }

                if (_featureToggle.IsAllAllocationResultsVersionsInFeedIndexEnabled())
                {
                    feedIndex.Id = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.FeedIndexId;
                    feedIndex.IsDeleted = false;
                    feedIndex.Title = publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.Title;
                }
                else
                {
                    feedIndex.Id = publishedProviderResult.Id;
                    feedIndex.Title = publishedProviderResult.Title;
                }

                notifications.Add(feedIndex);
            }

            return notifications;
        }

        IEnumerable<PublishedProviderResultsPolicySummary> CreatePolicySummaries(IEnumerable<PublishedProviderCalculationResult> calculationResults, SpecificationCurrentVersion specification)
        {
            IList<PublishedProviderResultsPolicySummary> policySummaries = new List<PublishedProviderResultsPolicySummary>();

            foreach (Policy policy in specification.Policies)
            {
                PublishedProviderResultsPolicySummary publishedProviderResultsPolicySummary = new PublishedProviderResultsPolicySummary
                {
                    Policy = new PolicySummary(policy.Id, policy.Name, policy.Description),
                    Calculations = AddCalculationSummaries(policy, calculationResults).ToArraySafe(),
                };

                foreach (Policy subPolicy in policy.SubPolicies)
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

        IEnumerable<PublishedProviderResultsCalculationSummary> AddCalculationSummaries(Policy policy, IEnumerable<PublishedProviderCalculationResult> calculationResults)
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
                    _logger.Error($"Failed to find published calculation result for calculation id {calculation.Id}");

                    continue;
                }

                PublishedProviderResultsCalculationSummary calculationSummary = new PublishedProviderResultsCalculationSummary
                {
                    Amount = publishedProviderCalculationResult.Current.Value.HasValue ? publishedProviderCalculationResult.Current.Value.Value : 0,
                    CalculationType = publishedProviderCalculationResult.Current.CalculationType,
                    Name = publishedProviderCalculationResult.CalculationSpecification.Name,
                    Version = publishedProviderCalculationResult.Current.Version
                };

                calculationSummaries = calculationSummaries.Concat(new[] { calculationSummary });
            }

            return calculationSummaries;
        }

        private void UpdateCacheForSegmentDone(string specificationId, int percentageToSetTo, CalculationProgressStatus progressStatus, string message = null, DateTimeOffset? publishedResultsRefreshedAt = null, IEnumerable<PublishedProviderResult> publishedProviderResults = null)
        {
            SpecificationCalculationExecutionStatus calculationProgress = new SpecificationCalculationExecutionStatus(specificationId, percentageToSetTo, progressStatus)
            {
                ErrorMessage = message,
                PublishedResultsRefreshedAt = publishedResultsRefreshedAt
            };

            if (!publishedProviderResults.IsNullOrEmpty())
            {
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


        //temporary until migration work finished
        private void SetFeedIndexId(PublishedProviderResult publishedProviderResult, PublishedAllocationLineResultVersion version)
        {
             version.FeedIndexId
                = $"{publishedProviderResult.FundingStreamResult.AllocationLineResult.AllocationLine.Id}-{publishedProviderResult.FundingPeriod.Id}" +
                 $"-{version.Provider.UKPRN}-v{version.Major}-{version.Minor}";
        }
    }
}