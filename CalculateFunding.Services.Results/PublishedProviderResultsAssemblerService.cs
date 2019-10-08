using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using CalculateFunding.Common.ApiClient.Models;
using CalculateFunding.Common.ApiClient.Policies;
using CalculateFunding.Common.Models;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Obsoleted;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces;
using CalculateFunding.Services.Results.Interfaces;
using Serilog;
using PolicyModels = CalculateFunding.Common.ApiClient.Policies.Models;

namespace CalculateFunding.Services.Results
{
    public class PublishedProviderResultsAssemblerService : IPublishedProviderResultsAssemblerService
    {
        private readonly IPoliciesApiClient _policiesApiClient;
        private readonly Polly.Policy _policiesApiClientPolicy;
        private readonly ILogger _logger;
        private readonly IVersionRepository<PublishedAllocationLineResultVersion> _allocationResultsVersionRepository;
        private readonly IMapper _mapper;

        public PublishedProviderResultsAssemblerService(
            IPoliciesApiClient policiesApiClient,
            IResultsResiliencePolicies resiliencePolicies,
            ILogger logger,
            IVersionRepository<PublishedAllocationLineResultVersion> allocationResultsVersionRepository,
            IMapper mapper)
        {
            Guard.ArgumentNotNull(policiesApiClient, nameof(policiesApiClient));
            Guard.ArgumentNotNull(resiliencePolicies?.PoliciesApiClient, nameof(resiliencePolicies.PoliciesApiClient));
            Guard.ArgumentNotNull(logger, nameof(logger));
            Guard.ArgumentNotNull(allocationResultsVersionRepository, nameof(allocationResultsVersionRepository));
            Guard.ArgumentNotNull(mapper, nameof(mapper));

            _policiesApiClient = policiesApiClient;
            _policiesApiClientPolicy = resiliencePolicies.PoliciesApiClient;
            _logger = logger;
            _allocationResultsVersionRepository = allocationResultsVersionRepository;
            _mapper = mapper;
        }

        public async Task<IEnumerable<PublishedProviderResult>> AssemblePublishedProviderResults(IEnumerable<ProviderResult> providerResults, Reference author, SpecificationCurrentVersion specificationCurrentVersion)
        {
            Guard.ArgumentNotNull(providerResults, nameof(providerResults));
            Guard.ArgumentNotNull(author, nameof(author));
            Guard.ArgumentNotNull(specificationCurrentVersion, nameof(specificationCurrentVersion));

            string specificationId = specificationCurrentVersion.Id;

            ApiResponse<PolicyModels.FundingPeriod> fundingPeriodResponse = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingPeriodById(specificationCurrentVersion.FundingPeriod.Id));
            PolicyModels.FundingPeriod fundingPeriod = fundingPeriodResponse?.Content;

            if (fundingPeriod == null)
            {
                throw new NonRetriableException($"Failed to find a funding period for id: {specificationCurrentVersion.FundingPeriod.Id}");
            }

            IEnumerable<string> providerIds = providerResults.Select(m => m.Provider.Id);

            ConcurrentBag<PublishedProviderResult> publishedProviderResults = new ConcurrentBag<PublishedProviderResult>();

            IEnumerable<FundingStream> allFundingStreams = await GetAllFundingStreams();

            Parallel.ForEach(providerResults, (providerResult) =>
            {
                IEnumerable<PublishedFundingStreamResult> publishedFundingStreamResults = AssembleFundingStreamResults(providerResult, specificationCurrentVersion, author, allFundingStreams);

                foreach (PublishedFundingStreamResult publishedFundingStreamResult in publishedFundingStreamResults)
                {
                    PublishedProviderResult publishedProviderResult = new PublishedProviderResult
                    {
                        ProviderId = providerResult.Provider.Id,
                        SpecificationId = specificationId,
                        FundingStreamResult = publishedFundingStreamResult,
                        FundingPeriod = new Period
                        {
                            Id = fundingPeriod.Id,
                            Name = fundingPeriod.Name,
                            StartDate = fundingPeriod.StartDate,
                            EndDate = fundingPeriod.EndDate
                        },
                    };

                    publishedProviderResult.FundingStreamResult.AllocationLineResult.Current.PublishedProviderResultId = publishedProviderResult.Id;

                    publishedProviderResults.Add(publishedProviderResult);
                }

            });

            return publishedProviderResults;
        }

        public async Task<(IEnumerable<PublishedProviderResult>, IEnumerable<PublishedProviderResultExisting>)> GeneratePublishedProviderResultsToSave(IEnumerable<PublishedProviderResult> providerResults, IEnumerable<PublishedProviderResultExisting> existingResults)
        {
            Guard.ArgumentNotNull(providerResults, nameof(providerResults));
            Guard.ArgumentNotNull(existingResults, nameof(existingResults));

            ConcurrentBag<PublishedProviderResult> publishedProviderResultsToSave = new ConcurrentBag<PublishedProviderResult>();

            ConcurrentDictionary<string, ConcurrentDictionary<string, PublishedProviderResultExisting>> existingProviderResults = new ConcurrentDictionary<string, ConcurrentDictionary<string, PublishedProviderResultExisting>>();

            foreach (PublishedProviderResultExisting providerResult in existingResults)
            {
                if (!existingProviderResults.ContainsKey(providerResult.ProviderId))
                {
                    existingProviderResults.TryAdd(providerResult.ProviderId, new ConcurrentDictionary<string, PublishedProviderResultExisting>());
                }

                existingProviderResults[providerResult.ProviderId].TryAdd(providerResult.AllocationLineId, providerResult);
            }

            List<Task> allTasks = new List<Task>();

            SemaphoreSlim throttler = new SemaphoreSlim(initialCount: 30);

            foreach (PublishedProviderResult providerResult in providerResults)
            {
                await throttler.WaitAsync();
                allTasks.Add(
                    Task.Run(async () =>
                    {
                        try
                        {
                            if (existingProviderResults.ContainsKey(providerResult.ProviderId))
                            {
                                ConcurrentDictionary<string, PublishedProviderResultExisting> existingResultsForProvider = existingProviderResults[providerResult.ProviderId];

                                if (existingResultsForProvider.TryGetValue(providerResult.FundingStreamResult.AllocationLineResult.AllocationLine.Id, out PublishedProviderResultExisting existingResult))
                                {
                                    existingResultsForProvider.TryRemove(providerResult.FundingStreamResult.AllocationLineResult.AllocationLine.Id, out var removedExistingResult);

                                    if (!existingResultsForProvider.Any())
                                    {
                                        existingProviderResults.TryRemove(providerResult.ProviderId, out var removedProviderResult);
                                    }

                                    if (providerResult.FundingStreamResult.AllocationLineResult.Current.Value == existingResult.Value)
                                    {
                                        existingProviderResults.TryRemove(providerResult.ProviderId, out var removedProviderResult);

                                        return;
                                    }

                                    providerResult.FundingStreamResult.AllocationLineResult.Current.Version =
                                        await _allocationResultsVersionRepository.GetNextVersionNumber(providerResult.FundingStreamResult.AllocationLineResult.Current, existingResult.Version, incrementFromCurrentVersion: true);

                                    providerResult.FundingStreamResult.AllocationLineResult.HasResultBeenVaried = existingResult.HasResultBeenVaried;

                                    if (existingResult.Status != AllocationLineStatus.Held)
                                    {
                                        providerResult.FundingStreamResult.AllocationLineResult.Current.Status = AllocationLineStatus.Updated;
                                    }

                                    if (existingResult.Published != null)
                                    {
                                        providerResult.FundingStreamResult.AllocationLineResult.Published = existingResult.Published;
                                    }

                                    providerResult.FundingStreamResult.AllocationLineResult.Current.Major = existingResult.Major;
                                    providerResult.FundingStreamResult.AllocationLineResult.Current.Minor = existingResult.Minor;
                                }
                                else
                                {
                                    providerResult.FundingStreamResult.AllocationLineResult.Current.Version = 1;
                                }
                            }
                            else
                            {
                                providerResult.FundingStreamResult.AllocationLineResult.Current.Version = 1;
                            }

                            publishedProviderResultsToSave.Add(providerResult);
                        }
                        finally
                        {
                            throttler.Release();
                        }
                    }));
            }
            await TaskHelper.WhenAllAndThrow(allTasks.ToArray());

            // Need to remove results that have been varied
            List<PublishedProviderResult> publishedProviderResultsToSaveList = publishedProviderResultsToSave.ToList();
            publishedProviderResultsToSaveList.RemoveAll(r => r.FundingStreamResult.AllocationLineResult.HasResultBeenVaried);

            List<PublishedProviderResultExisting> existingRecordsExclude = new List<PublishedProviderResultExisting>(existingProviderResults.Values.Count);
            foreach (ConcurrentDictionary<string, PublishedProviderResultExisting> existingList in existingProviderResults.Values)
            {
                existingRecordsExclude.AddRange(existingList.Values.Where(r => !r.HasResultBeenVaried));
            }

            return (publishedProviderResultsToSaveList, existingRecordsExclude);
        }

        private PublishedCalculationType ConvertCalculationType(Models.Calcs.CalculationType calculationType)
        {
            switch (calculationType)
            {
                case Models.Calcs.CalculationType.Template:
                    return PublishedCalculationType.Funding;

                default:
                    throw new NonRetriableException($"Unknown CalculationType: {calculationType}");
            }
        }

        private IEnumerable<PublishedFundingStreamResult> AssembleFundingStreamResults(ProviderResult providerResult, SpecificationCurrentVersion specificationCurrentVersion, Reference author, IEnumerable<FundingStream> allFundingStreams)
        {
            IList<PublishedFundingStreamResult> publishedFundingStreamResults = new List<PublishedFundingStreamResult>();

            Dictionary<string, PublishedAllocationLineDefinition> publishedAllocationLines = new Dictionary<string, PublishedAllocationLineDefinition>();

            foreach (Reference fundingStreamReference in specificationCurrentVersion.FundingStreams)
            {
                FundingStream fundingStream = allFundingStreams.FirstOrDefault(m => m.Id == fundingStreamReference.Id);

                if (fundingStream == null)
                {
                    throw new NonRetriableException($"Failed to find a funding stream for id: {fundingStreamReference.Id}");
                }

                PublishedFundingStreamDefinition publishedFundingStreamDefinition = _mapper.Map<PublishedFundingStreamDefinition>(fundingStream);

                List<PublishedProviderCalculationResult> publishedProviderCalculationResults = new List<PublishedProviderCalculationResult>(providerResult.CalculationResults.Count());

                foreach (CalculationResult calculationResult in providerResult.CalculationResults)
                {
                    PublishedProviderCalculationResult publishedProviderCalculationResult = new PublishedProviderCalculationResult()
                    {
                        AllocationLine = calculationResult.AllocationLine,
                        CalculationType = ConvertCalculationType(calculationResult.CalculationType),
                        Value = calculationResult.Value,
                        CalculationVersion = calculationResult.Version
                    };

                    publishedProviderCalculationResults.Add(publishedProviderCalculationResult);
                }

                IEnumerable<IGrouping<string, CalculationResult>> allocationLineGroups = providerResult
                    .CalculationResults
                    .Where(c => c.CalculationType == Models.Calcs.CalculationType.Template && c.Value.HasValue && c.AllocationLine != null && !string.IsNullOrWhiteSpace(c.AllocationLine.Id))
                    .GroupBy(m => m.AllocationLine.Id);

                foreach (IGrouping<string, CalculationResult> allocationLineResultGroup in allocationLineGroups)
                {
                    PublishedAllocationLineDefinition publishedAllocationLine;
                    if (!publishedAllocationLines.TryGetValue(allocationLineResultGroup.Key, out publishedAllocationLine))
                    {
                        AllocationLine allocationLine = fundingStream.AllocationLines.FirstOrDefault(m => m.Id == allocationLineResultGroup.Key);
                        if (allocationLine != null)
                        {
                            publishedAllocationLine = _mapper.Map<PublishedAllocationLineDefinition>(allocationLine);
                            publishedAllocationLines.Add(allocationLineResultGroup.Key, publishedAllocationLine);
                        }
                    }

                    if (publishedAllocationLine != null)
                    {
                        PublishedFundingStreamResult publishedFundingStreamResult = new PublishedFundingStreamResult
                        {
                            FundingStream = publishedFundingStreamDefinition,

                            FundingStreamPeriod = $"{fundingStream.Id}{specificationCurrentVersion.FundingPeriod.Id}",

                            DistributionPeriod = $"{fundingStream.PeriodType.Id}{specificationCurrentVersion.FundingPeriod.Id}"
                        };

                        PublishedAllocationLineResultVersion publishedAllocationLineResultVersion = new PublishedAllocationLineResultVersion
                        {
                            Author = author,
                            Date = DateTimeOffset.Now,
                            Status = AllocationLineStatus.Held,
                            Value = allocationLineResultGroup.Sum(m => m.Value),
                            Provider = providerResult.Provider,
                            SpecificationId = specificationCurrentVersion.Id,
                            ProviderId = providerResult.Provider.Id,
                            Calculations = publishedProviderCalculationResults.Where(c => c.AllocationLine == null || string.Equals(c.AllocationLine.Id, publishedAllocationLine.Id, StringComparison.InvariantCultureIgnoreCase)),
                        };

                        publishedFundingStreamResult.AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = publishedAllocationLine,
                            Current = publishedAllocationLineResultVersion
                        };

                        publishedFundingStreamResults.Add(publishedFundingStreamResult);
                    }
                }
            }

            return publishedFundingStreamResults;
        }

        private async Task<IEnumerable<FundingStream>> GetAllFundingStreams()
        {
            ApiResponse<IEnumerable<PolicyModels.FundingStream>> allFundingStreamsResponse = await _policiesApiClientPolicy.ExecuteAsync(() => _policiesApiClient.GetFundingStreams());
            IEnumerable<FundingStream> allFundingStreams = _mapper.Map<IEnumerable<FundingStream>>(allFundingStreamsResponse?.Content);

            if (allFundingStreams.IsNullOrEmpty())
            {
                throw new NonRetriableException("Failed to get all funding streams");
            }

            return allFundingStreams;
        }
    }
}
