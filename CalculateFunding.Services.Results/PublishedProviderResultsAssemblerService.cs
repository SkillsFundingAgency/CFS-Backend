using CalculateFunding.Models;
using CalculateFunding.Models.Results;
using CalculateFunding.Models.Specs;
using CalculateFunding.Repositories.Common.Cosmos;
using CalculateFunding.Services.Core.Caching;
using CalculateFunding.Services.Core.Helpers;
using CalculateFunding.Services.Core.Interfaces.Caching;
using CalculateFunding.Services.Results.Interfaces;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Services.Results
{
    public class PublishedProviderResultsAssemblerService : IPublishedProviderResultsAssemblerService
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

        public async Task<IEnumerable<PublishedProviderResult>> Assemble(IEnumerable<ProviderResult> providerResults, Reference author, string specificationId)
        {
            Guard.ArgumentNotNull(providerResults, nameof(providerResults));
            Guard.ArgumentNotNull(author, nameof(author));
            Guard.IsNullOrWhiteSpace(specificationId, nameof(specificationId));

            SpecificationSummary specificationSummary = await _specificationsRepository.GetSpecificationSummaryById(specificationId);
            
            if(specificationSummary == null)
            {
                throw new Exception($"A specification with id: {specificationId} could not be found");
            }

            IEnumerable<string> providerIds = providerResults.Select(m => m.Provider.Id);

            ConcurrentBag<PublishedProviderResult> publishedProviderResults = new ConcurrentBag<PublishedProviderResult>();

            IList<Task> assembleTasks = new List<Task>();

            foreach(ProviderResult providerResult in providerResults)
            {
                assembleTasks.Add(Task.Run(async() =>
                {
                    PublishedProviderResult publishedProviderResult = new PublishedProviderResult();

                    publishedProviderResult.Id = $"{providerResult.Provider.Id}_{specificationId}";
                    publishedProviderResult.SpecificationId = specificationId;
                    publishedProviderResult.ProviderId = providerResult.Provider.Id;
                    publishedProviderResult.Name = providerResult.Provider.Name;
                    publishedProviderResult.Ukprn = providerResult.Provider.UKPRN;
                  
                    publishedProviderResult.FundingStreamResults = await AssembleFundingStreamResults(providerResult, specificationSummary, author);

                    publishedProviderResults.Add(publishedProviderResult);
                }));      
            }

            await TaskHelper.WhenAllAndThrow(assembleTasks.ToArray());

            return publishedProviderResults;
        }

        async Task<IEnumerable<PublishedFundingStreamResult>> AssembleFundingStreamResults(ProviderResult providerResult, SpecificationSummary specificationSummary, Reference author)
        {
            IEnumerable<FundingStream> allFundingStreams = await  GetAllFundingStreams();

            IList<PublishedFundingStreamResult> publishedFundingStreamResults = new List<PublishedFundingStreamResult>();

            foreach(Reference fundingStreamReference in specificationSummary.FundingStreams)
            {
                FundingStream fundingStream = allFundingStreams.FirstOrDefault(m => m.Id == fundingStreamReference.Id);

                if (fundingStream == null)
                    throw new Exception($"Failed to find a funding stream for id: {fundingStreamReference.Id}");

                PublishedFundingStreamResult publishedFundingStreamResult = new PublishedFundingStreamResult();

                publishedFundingStreamResult.FundingStream = fundingStreamReference;

                publishedFundingStreamResult.AllocationLineResults = Enumerable.Empty<PublishedAllocationLineResult>();

                foreach(AllocationLineResult allocationLineResult in providerResult.AllocationLineResults)
                {
                    AllocationLine allocationLine = fundingStream.AllocationLines.FirstOrDefault(m => m.Id == allocationLineResult.AllocationLine.Id);

                    if(allocationLine != null)
                    {
                        PublishedAllocationLineResultVersion publishedAllocationLineResultVersion = new PublishedAllocationLineResultVersion
                        {
                            Author = author,
                            Date = DateTimeOffset.Now,
                            Status = AllocationLineStatus.Held,
                            Version = 1
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
                                Value = allocationLineResult.Value,
                                Current = publishedAllocationLineResultVersion,
                                History = new List<PublishedAllocationLineResultVersion> { publishedAllocationLineResultVersion }
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
