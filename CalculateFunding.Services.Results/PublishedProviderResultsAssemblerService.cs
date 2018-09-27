using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Results.Interfaces;
using Serilog;

namespace CalculateFunding.Services.Results
{
    public class PublishedProviderResultsAssemblerService : IPublishedProviderResultsAssemblerService
    {
        private readonly ISpecificationsRepository _specificationsRepository;
        private readonly ILogger _logger;

        public PublishedProviderResultsAssemblerService(
            ISpecificationsRepository specificationsRepository,
            ILogger logger)
        {
            Guard.ArgumentNotNull(specificationsRepository, nameof(specificationsRepository));

            _specificationsRepository = specificationsRepository;
            _logger = logger;
        }

        public async Task<IEnumerable<PublishedProviderResult>> AssemblePublishedProviderResults(IEnumerable<ProviderResult> providerResults, Reference author, SpecificationCurrentVersion specificationCurrentVersion)
        {
            Guard.ArgumentNotNull(providerResults, nameof(providerResults));
            Guard.ArgumentNotNull(author, nameof(author));
            Guard.ArgumentNotNull(specificationCurrentVersion, nameof(specificationCurrentVersion));

            string specificationId = specificationCurrentVersion.Id;

            Period fundingPeriod = await _specificationsRepository.GetFundingPeriodById(specificationCurrentVersion.FundingPeriod.Id);

            if (fundingPeriod == null)
            {
                throw new Exception($"Failed to find a funding period for id: {specificationCurrentVersion.FundingPeriod.Id}");
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
                        Summary = $"{providerResult.Provider.ProviderProfileIdType}: {providerResult.Provider.Id}, version {publishedFundingStreamResult.AllocationLineResult.Current.Version}",
                        Title = $"Allocation {publishedFundingStreamResult.AllocationLineResult.AllocationLine.Name} was {publishedFundingStreamResult.AllocationLineResult.Current.Status.ToString()}",
                        FundingPeriod = fundingPeriod
                    };

                    publishedProviderResults.Add(publishedProviderResult);
                }
            });

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
                    (Policy policy, Policy parentPolicy, Models.Specs.Calculation calculation) = FindPolicy(calculationResult.CalculationSpecification.Id, specificationCurrentVersion.Policies);

                    if (calculation.CalculationType == Models.Specs.CalculationType.Number && !calculation.IsPublic)
                    {
                        continue;
                    }

                    PublishedProviderCalculationResult publishedProviderCalculationResult = new PublishedProviderCalculationResult()
                    {
                        ProviderId = providerResult.Provider.Id,
                        Approved = null,
                        CalculationSpecification = calculationResult.CalculationSpecification,
                        FundingPeriod = specificationCurrentVersion.FundingPeriod,
                        AllocationLine = calculationResult.AllocationLine,
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

                    if (policy != null)
                    {
                        publishedProviderCalculationResult.Policy = new PolicySummary(policy.Id, policy.Name, policy.Description);
                    }

                    if (parentPolicy != null)
                    {
                        publishedProviderCalculationResult.ParentPolicy = new PolicySummary(parentPolicy.Id, parentPolicy.Name, parentPolicy.Description);
                    }

                    publishedProviderCalculationResults.Add(publishedProviderCalculationResult);
                }
            }

            return publishedProviderCalculationResults;
        }

        public (IEnumerable<PublishedProviderResult>, IEnumerable<PublishedProviderResultExisting>) GeneratePublishedProviderResultsToSave(IEnumerable<PublishedProviderResult> providerResults, IEnumerable<PublishedProviderResultExisting> existingResults)
        {
            Guard.ArgumentNotNull(providerResults, nameof(providerResults));
            Guard.ArgumentNotNull(existingResults, nameof(existingResults));

            List<PublishedProviderResult> publishedProviderResultsToSave = new List<PublishedProviderResult>();

            Dictionary<string, List<PublishedProviderResultExisting>> existingProviderResults = new Dictionary<string, List<PublishedProviderResultExisting>>();
            foreach (PublishedProviderResultExisting providerResult in existingResults)
            {
                if (!existingProviderResults.ContainsKey(providerResult.ProviderId))
                {
                    existingProviderResults.Add(providerResult.ProviderId, new List<PublishedProviderResultExisting>());
                }

                existingProviderResults[providerResult.ProviderId].Add(providerResult);
            }

            foreach (PublishedProviderResult providerResult in providerResults)
            {
                if (existingProviderResults.ContainsKey(providerResult.ProviderId))
                {
                    List<PublishedProviderResultExisting> existingResultsForProvider = existingProviderResults[providerResult.ProviderId];
                    PublishedProviderResultExisting existingResult = existingResultsForProvider.Where(p => p.AllocationLineId == providerResult.FundingStreamResult.AllocationLineResult.AllocationLine.Id).SingleOrDefault();
                    if (existingResult != null)
                    {
                        existingResultsForProvider.Remove(existingResult);

                        if (!existingResultsForProvider.Any())
                        {
                            existingProviderResults.Remove(providerResult.ProviderId);
                        }

                        if (providerResult.FundingStreamResult.AllocationLineResult.Current.Value == existingResult.Value)
                        {
                            continue;
                        }
                    }
                }

                publishedProviderResultsToSave.Add(providerResult);
            }

            List<PublishedProviderResultExisting> existingRecordsExclude = new List<PublishedProviderResultExisting>(existingProviderResults.Values.Count);
            foreach (List<PublishedProviderResultExisting> existingList in existingProviderResults.Values)
            {
                existingRecordsExclude.AddRange(existingList);
            }

            return (publishedProviderResultsToSave, existingRecordsExclude);
        }

        private (Policy policy, Policy parentPolicy, Models.Specs.Calculation calculation) FindPolicy(string calculationSpecificationId, IEnumerable<Policy> policies)
        {
            foreach (Policy policy in policies)
            {
                if (policy != null)
                {
                    if (policy.Calculations != null)
                    {
                        Models.Specs.Calculation calc = policy.Calculations.FirstOrDefault(c => c.Id == calculationSpecificationId);
                        if (calc != null)
                        {
                            return (policy, null, calc);
                        }
                    }

                    if (policy.SubPolicies != null)
                    {
                        foreach (Policy subpolicy in policy.SubPolicies)
                        {
                            Models.Specs.Calculation calc = subpolicy.Calculations.FirstOrDefault(c => c.Id == calculationSpecificationId);

                            if (subpolicy.Calculations.Any(c => c.Id == calculationSpecificationId))
                            {
                                return (subpolicy, policy, calc);
                            }
                        }
                    }
                }
            }

            return (null, null, null);
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

        private IEnumerable<PublishedFundingStreamResult> AssembleFundingStreamResults(ProviderResult providerResult, SpecificationCurrentVersion specificationCurrentVersion, Reference author, IEnumerable<FundingStream> allFundingStreams)
        {
            IList<PublishedFundingStreamResult> publishedFundingStreamResults = new List<PublishedFundingStreamResult>();

            foreach (Reference fundingStreamReference in specificationCurrentVersion.FundingStreams)
            {
                FundingStream fundingStream = allFundingStreams.FirstOrDefault(m => m.Id == fundingStreamReference.Id);

                if (fundingStream == null)
                {
                    throw new Exception($"Failed to find a funding stream for id: {fundingStreamReference.Id}");
                }

                IEnumerable<IGrouping<string, CalculationResult>> allocationLineGroups = providerResult
                    .CalculationResults
                    .Where(c => c.CalculationType == Models.Calcs.CalculationType.Funding && c.Value.HasValue && c.AllocationLine != null && !string.IsNullOrWhiteSpace(c.AllocationLine.Id))
                    .GroupBy(m => m.AllocationLine.Id);

                foreach (IGrouping<string, CalculationResult> allocationLineResultGroup in allocationLineGroups)
                {
                    AllocationLine allocationLine = fundingStream.AllocationLines.FirstOrDefault(m => m.Id == allocationLineResultGroup.Key);

                    if (allocationLine != null)
                    {
                        PublishedFundingStreamResult publishedFundingStreamResult = new PublishedFundingStreamResult
                        {
                            FundingStream = fundingStream,

                            FundingStreamPeriod = $"{fundingStream.Id}{specificationCurrentVersion.FundingPeriod.Id}",

                            DistributionPeriod = $"{fundingStream.PeriodType.Id}{specificationCurrentVersion.FundingPeriod.Id}"
                        };

                        PublishedAllocationLineResultVersion publishedAllocationLineResultVersion = new PublishedAllocationLineResultVersion
                        {
                            Author = author,
                            Date = DateTimeOffset.Now,
                            Status = AllocationLineStatus.Held,
                            Version = 1,
                            Value = allocationLineResultGroup.Sum(m => m.Value),
                            Provider = providerResult.Provider
                        };

                        publishedFundingStreamResult.AllocationLineResult = new PublishedAllocationLineResult
                        {
                            AllocationLine = allocationLine,
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

            IEnumerable<FundingStream> allFundingStreams = await _specificationsRepository.GetFundingStreams();

            if (allFundingStreams.IsNullOrEmpty())
            {
                throw new Exception("Failed to get all funding streams");
            }

            return allFundingStreams;
        }
    }
}
