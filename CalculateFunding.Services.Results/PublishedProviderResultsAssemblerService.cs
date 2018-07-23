using CalculateFunding.Models;
using CalculateFunding.Models.Health;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Extensions;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Core.Interfaces.Services;
using CalculateFunding.Services.Results.Interfaces;
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

        public PublishedProviderResultsAssemblerService(
            ISpecificationsRepository specificationsRepository,
            ICacheProvider cacheProvider)
        {
            Guard.ArgumentNotNull(cacheProvider, nameof(cacheProvider));
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));

            _specificationsRepository = specificationsRepository;
            _cacheProvider = cacheProvider;
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

            IEnumerable<string> providerIds = providerResults.Select(m => m.Provider.Id);

            ConcurrentBag<PublishedProviderResult> publishedProviderResults = new ConcurrentBag<PublishedProviderResult>();

            IList<Task> assembleTasks = new List<Task>();

            foreach(ProviderResult providerResult in providerResults)
            {
                assembleTasks.Add(Task.Run(async () =>
                {
                    PublishedProviderResult publishedProviderResult = new PublishedProviderResult
                    {
                        Id = $"{providerResult.Provider.Id}_{specificationId}",
                        SpecificationId = specificationId,
                        ProviderId = providerResult.Provider.Id,
                        Name = providerResult.Provider.Name,
                        Ukprn = providerResult.Provider.UKPRN,

                        FundingStreamResults = await AssembleFundingStreamResults(providerResult, specificationCurrentVersion, author)
                    };

                    publishedProviderResults.Add(publishedProviderResult);

                }));
      
            }

            await TaskHelper.WhenAllAndThrow(assembleTasks.ToArray());

            return publishedProviderResults;
        }

        public IEnumerable<PublishedProviderCalculationResult> AssemblePublishedCalculationResults(IEnumerable<ProviderResult> providerResults, Reference author, SpecificationCurrentVersion specificationCurrentVersion)
        {
            Guard.ArgumentNotNull(providerResults, nameof(providerResults));
            Guard.ArgumentNotNull(author, nameof(author));
            Guard.ArgumentNotNull(specificationCurrentVersion, nameof(specificationCurrentVersion));

            string specificationId = specificationCurrentVersion.Id;

            IEnumerable<string> providerIds = providerResults.Select(m => m.Provider.Id);

            ConcurrentBag<PublishedProviderCalculationResult> publishedProviderCalculationResults = new ConcurrentBag<PublishedProviderCalculationResult>();

            IList<Task> assembleTasks = new List<Task>();

            foreach (ProviderResult providerResult in providerResults)
            {

                PublishedProviderCalculationResult publishedProviderCalculationResult = new PublishedProviderCalculationResult
                {
                    Id = $"{providerResult.Provider.Id}_{specificationId}",
                    ProviderId = providerResult.Provider.Id,
                    Name = providerResult.Provider.Name,
                    Ukprn = providerResult.Provider.UKPRN,
                    Specification = new Reference(specificationCurrentVersion.Id, specificationCurrentVersion.Name)
                };

                foreach (Policy policy in specificationCurrentVersion.Policies)
                {
                    PublishedProviderCalculationResultPolicy resultPolicy = AssemblePolicyCalculations(policy, providerResult, author);

                    foreach(Policy subPolicy in policy.SubPolicies)
                    {
                        PublishedProviderCalculationResultPolicy resultSubPolicy = AssemblePolicyCalculations(subPolicy, providerResult, author);

                        resultPolicy.SubPolicies = resultPolicy.SubPolicies.Concat(new[] { resultSubPolicy });
                    }

                    publishedProviderCalculationResult.Policies = publishedProviderCalculationResult.Policies.Concat(new[] { resultPolicy });
                }

                publishedProviderCalculationResults.Add(publishedProviderCalculationResult);
            }

            return publishedProviderCalculationResults;
        }

        PublishedProviderCalculationResultPolicy AssemblePolicyCalculations(Policy policy, ProviderResult providerResult, Reference author)
        {
            PublishedProviderCalculationResultPolicy resultPolicy = new PublishedProviderCalculationResultPolicy
            {
                Id = policy.Id,
                Name = policy.Name
            };

            foreach (Calculation calculationSpec in policy.Calculations)
            {
                PublishedProviderCalculationResultCalculationVersion current = new PublishedProviderCalculationResultCalculationVersion
                {
                    Value = providerResult.CalculationResults.FirstOrDefault(m => m.CalculationSpecification.Id == calculationSpec.Id) != null ?
                       providerResult.CalculationResults.FirstOrDefault(m => m.CalculationSpecification.Id == calculationSpec.Id).Value : 0,

                    Author = author,
                    Date = DateTimeOffset.Now,
                    Version = 1
                };

                PublishedCalculationResult publishedCalculationResult = new PublishedCalculationResult
                {
                    CalculationSpecification = new Reference(calculationSpec.Id, calculationSpec.Name),
                    CalculationType = calculationSpec.CalculationType,
                    IsPublic = calculationSpec.IsPublic,
                    Current = current,
                    History = new List<PublishedProviderCalculationResultCalculationVersion> { current }
                };

                resultPolicy.CalculationResults = resultPolicy.CalculationResults.Concat(new[] { publishedCalculationResult });

            }

            return resultPolicy;
        }

        async Task<IEnumerable<PublishedFundingStreamResult>> AssembleFundingStreamResults(ProviderResult providerResult, SpecificationCurrentVersion specificationCurrentVersion, Reference author)
        {
            IEnumerable<FundingStream> allFundingStreams = await  GetAllFundingStreams();

            IList<PublishedFundingStreamResult> publishedFundingStreamResults = new List<PublishedFundingStreamResult>();

            foreach(Reference fundingStreamReference in specificationCurrentVersion.FundingStreams)
            {
                FundingStream fundingStream = allFundingStreams.FirstOrDefault(m => m.Id == fundingStreamReference.Id);

                if (fundingStream == null)
                    throw new Exception($"Failed to find a funding stream for id: {fundingStreamReference.Id}");

                PublishedFundingStreamResult publishedFundingStreamResult = new PublishedFundingStreamResult();

                publishedFundingStreamResult.FundingStream = new Reference(fundingStreamReference.Id, fundingStreamReference.Name);

                IEnumerable<IGrouping<string, AllocationLineResult>> allocationLineGroups = providerResult.AllocationLineResults.GroupBy(m => m.AllocationLine.Id);

                foreach (IGrouping<string,AllocationLineResult> allocationLineResultGroup in allocationLineGroups)
                {
                    AllocationLine allocationLine = fundingStream.AllocationLines.FirstOrDefault(m => m.Id == allocationLineResultGroup.Key);

                    if(allocationLine != null)
                    {
                        PublishedAllocationLineResultVersion publishedAllocationLineResultVersion = new PublishedAllocationLineResultVersion
                        {
                            Author = author,
                            Date = DateTimeOffset.Now,
                            Status = AllocationLineStatus.Held,
                            Version = 1,
                            Value = allocationLineResultGroup.Sum(m => m.Value),
                        };

                        publishedFundingStreamResult.AllocationLineResults = publishedFundingStreamResult.AllocationLineResults.Concat(new[]
                        {
                            new PublishedAllocationLineResult
                            {
                                AllocationLine = new Reference
                                {
                                    Name = allocationLine.Name,
                                    Id = allocationLine.Id
                                },
                                Current = publishedAllocationLineResultVersion
                            }
                        }).ToList();
                    }
                }

                publishedFundingStreamResults.Add(publishedFundingStreamResult);
            }

            return publishedFundingStreamResults;
        }

        async Task<IEnumerable<FundingStream>> GetAllFundingStreams()
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
