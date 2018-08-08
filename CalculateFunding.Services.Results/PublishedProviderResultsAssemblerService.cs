using CalculateFunding.Models;
using CalculateFunding.Models.Calcs;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Results.Interfaces;
using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class PublishedProviderResultsAssemblerService : IPublishedProviderResultsAssemblerService, IHealthChecker
    {
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly ICacheProvider _cacheProvider;
        private readonly ILogger _logger;

        public PublishedProviderResultsAssemblerService(
            ISpecificationsRepository specificationsRepository,
            ICacheProvider cacheProvider,
            ILogger logger)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));

            _specificationsRepository = specificationsRepository;
            _cacheProvider = cacheProvider;
            _logger = logger;
        }

        public async Task<ServiceHealth> IsHealthOk()
        {
            var cacheHealth = await _cacheProvider.IsHealthOk();

            ServiceHealth health = new ServiceHealth()
            {
                Name = nameof(PublishedProviderResultsAssemblerService)
            };
            health.Dependencies.Add(new DependencyHealth { HealthOk = cacheHealth.Ok, DependencyName = _cacheProvider.GetType().GetFriendlyName(), Message = cacheHealth.Message });

            return health;
        }

        public async Task<IEnumerable<PublishedProviderResult>> AssemblePublishedProviderResults(IEnumerable<ProviderResult> providerResults, Reference author, SpecificationCurrentVersion specificationCurrentVersion)
        {
            Guard.ArgumentNotNull(providerResults, nameof(providerResults));
            Guard.ArgumentNotNull(author, nameof(author));
            Guard.ArgumentNotNull(specificationCurrentVersion, nameof(specificationCurrentVersion));

            string specificationId = specificationCurrentVersion.Id;

            FundingPeriod fundingPeriod = await _specificationsRepository.GetFundingPeriodById(specificationCurrentVersion.FundingPeriod.Id);

            if (fundingPeriod == null)
            {
                throw new Exception($"Failed to find a funding period for id: {specificationCurrentVersion.FundingPeriod.Id}");
            }

            IEnumerable<string> providerIds = providerResults.Select(m => m.Provider.Id);

            ConcurrentBag<PublishedProviderResult> publishedProviderResults = new ConcurrentBag<PublishedProviderResult>();

            IList<Task> assembleTasks = new List<Task>();

            foreach (ProviderResult providerResult in providerResults)
            {
                assembleTasks.Add(Task.Run(async () =>
                {
                    IEnumerable<PublishedFundingStreamResult> publishedFundingStreamResults = await AssembleFundingStreamResults(providerResult, specificationCurrentVersion, author);

                    foreach (PublishedFundingStreamResult publishedFundingStreamResult in publishedFundingStreamResults)
                    {
                        PublishedProviderResult publishedProviderResult = new PublishedProviderResult
                        {
                            Id = Guid.NewGuid().ToString("N"),
                            SpecificationId = specificationId,
                            Provider = providerResult.Provider,
                            FundingStreamResult = publishedFundingStreamResult,
                            Summary = $"{providerResult.Provider.ProviderProfileIdType}: {providerResult.Provider.Id}, version {publishedFundingStreamResult.AllocationLineResult.Current.Version}",
                            Title = $"Allocation {publishedFundingStreamResult.AllocationLineResult.AllocationLine.Name} was {publishedFundingStreamResult.AllocationLineResult.Current.Status.ToString()}",
                            FundingPeriod = fundingPeriod
                        };

                        publishedProviderResults.Add(publishedProviderResult);
                    }

                }));

            }

            await TaskHelper.WhenAllAndThrow(assembleTasks.ToArray());

            return publishedProviderResults;
        }

        /// <summary>
        /// AssemblePublishedCalculationResults - currently only handles initial create, not updating values through approvals etc
        /// </summary>
        /// <param name="providerResults">Provider Results from Calculation Engine</param>
        /// <param name="author">Author - user who performed this action</param>
        /// <param name="specificationCurrentVersion">Specification</param>
        /// <returns></returns>
        public IEnumerable<PublishedProviderCalculationResult> AssemblePublishedCalculationResults(IEnumerable<ProviderResult> providerResults, Reference author, SpecificationCurrentVersion specificationCurrentVersion)
        {
            Guard.ArgumentNotNull(providerResults, nameof(providerResults));
            Guard.ArgumentNotNull(author, nameof(author));
            Guard.ArgumentNotNull(specificationCurrentVersion, nameof(specificationCurrentVersion));

            string specificationId = specificationCurrentVersion.Id;

            IEnumerable<string> providerIds = providerResults.Select(m => m.Provider.Id);

            List<PublishedProviderCalculationResult> publishedProviderCalculationResults = new List<PublishedProviderCalculationResult>();

            Reference specification = new Reference(specificationCurrentVersion.Id, specificationCurrentVersion.Name);

            foreach (ProviderResult providerResult in providerResults)
            {
                foreach (CalculationResult calculationResult in providerResult.CalculationResults)
                {
                    (Policy policy, Policy parentPolicy) = FindPolicy(calculationResult.CalculationSpecification.Id, specificationCurrentVersion.Policies);

                    PublishedProviderCalculationResult publishedProviderCalculationResult = new PublishedProviderCalculationResult()
                    {
                        Id = Guid.NewGuid().ToString("N"),
                        ProviderId = providerResult.Provider.Id,
                        Approved = null,
                        CalculationSpecification = calculationResult.CalculationSpecification,
                        Current = new PublishedProviderCalculationResultCalculationVersion()
                        {
                            Author = author,
                            CalculationType = ConvertCalculationType(calculationResult.CalculationType),
                            Commment = null,
                            Date = DateTimeOffset.Now,
                            Provider = providerResult.Provider,
                            Value = calculationResult.Value,
                            Version = 1,
                        },
                        Published = null,
                        Specification = new Reference(specification.Id, specification.Name)
                    };

                    if(policy != null)
                    {
                        publishedProviderCalculationResult.Policy = new Reference(policy.Id, policy.Name);
                    }

                    if(parentPolicy != null)
                    {
                        publishedProviderCalculationResult.ParentPolicy = new Reference(parentPolicy.Id, parentPolicy.Name);
                    }

                    publishedProviderCalculationResults.Add(publishedProviderCalculationResult);
                }
            }

            return publishedProviderCalculationResults;
        }

        private (Policy policy, Policy parentPolicy) FindPolicy(string calculationSpecificationId, IEnumerable<Policy> policies)
        {
            foreach (Policy policy in policies)
            {
                if (policy != null)
                {
                    if (policy.Calculations != null)
                    {
                        if (policy.Calculations.Any(c => c.Id == calculationSpecificationId))
                        {
                            return (policy, null);
                        }
                    }

                    if (policy.SubPolicies != null)
                    {
                        foreach (Policy subpolicy in policy.SubPolicies)
                        {
                            if (subpolicy.Calculations.Any(c => c.Id == calculationSpecificationId))
                            {
                                return (subpolicy, policy);
                            }
                        }
                    }
                }
            }

            return (null, null);
        }

        private PublishedCalculationType ConvertCalculationType(Models.Calcs.CalculationType calculationType)
        {
            switch (calculationType)
            {
                case Models.Calcs.CalculationType.Funding:
                    return PublishedCalculationType.Funding;
                case Models.Calcs.CalculationType.Number:
                    return PublishedCalculationType.Number;
                default:
                    throw new InvalidOperationException($"Unknown {typeof(Models.Calcs.CalculationType)}");
            }
        }

        private async Task<IEnumerable<PublishedFundingStreamResult>> AssembleFundingStreamResults(ProviderResult providerResult, SpecificationCurrentVersion specificationCurrentVersion, Reference author)
        {
            IEnumerable<FundingStream> allFundingStreams = await GetAllFundingStreams();

            IList<PublishedFundingStreamResult> publishedFundingStreamResults = new List<PublishedFundingStreamResult>();

            foreach (Reference fundingStreamReference in specificationCurrentVersion.FundingStreams)
            {
                FundingStream fundingStream = allFundingStreams.FirstOrDefault(m => m.Id == fundingStreamReference.Id);

                if (fundingStream == null)
                    throw new Exception($"Failed to find a funding stream for id: {fundingStreamReference.Id}");

                IEnumerable<IGrouping<string, AllocationLineResult>> allocationLineGroups = providerResult.AllocationLineResults.GroupBy(m => m.AllocationLine.Id);

                foreach (IGrouping<string, AllocationLineResult> allocationLineResultGroup in allocationLineGroups)
                {
                    AllocationLine allocationLine = fundingStream.AllocationLines.FirstOrDefault(m => m.Id == allocationLineResultGroup.Key);

                    if (allocationLine != null)
                    {
                        PublishedFundingStreamResult publishedFundingStreamResult = new PublishedFundingStreamResult();

                        publishedFundingStreamResult.FundingStream = new Reference(fundingStreamReference.Id, fundingStreamReference.Name);

                        PublishedAllocationLineResultVersion publishedAllocationLineResultVersion = new PublishedAllocationLineResultVersion
                        {
                            Author = author,
                            Date = DateTimeOffset.Now,
                            Status = AllocationLineStatus.Held,
                            Version = 1,
                            Value = allocationLineResultGroup.Sum(m => m.Value),
                        };

                        publishedFundingStreamResult.AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = new Reference
                            {
                                Name = allocationLine.Name,
                                Id = allocationLine.Id
                            },
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
            IEnumerable<FundingStream> allFundingStreams = await _cacheProvider.GetAsync<FundingStream[]>(CacheKeys.AllFundingStreams);

            if (allFundingStreams.IsNullOrEmpty())
            {
                allFundingStreams = await _specificationsRepository.GetFundingStreams();

                if (allFundingStreams.IsNullOrEmpty())
                {
                    throw new Exception("Failed to get all funding streams");
                }

                await _cacheProvider.SetAsync<FundingStream[]>(CacheKeys.AllFundingStreams, allFundingStreams.ToArray());
            }

            return allFundingStreams;
        }
    }
}
