using System;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Services.Publishing.Interfaces;
using Microsoft.Azure.Search.Models;
using Polly;
using Serilog;

namespace CalculateFunding.Services.Publishing.Providers
{
    public class DeleteFundingSearchDocumentsService : IDeleteFundingSearchDocumentsService
    {
        private readonly ISearchRepository<PublishedProviderIndex> _publishedProviderSearch;
        private readonly ISearchRepository<PublishedFundingIndex> _publishedFundingSearch;
        private readonly Policy _searchPolicy;
        private readonly ILogger _logger;
 
        public DeleteFundingSearchDocumentsService(ISearchRepository<PublishedProviderIndex> publishedProviderSearch,
            ISearchRepository<PublishedFundingIndex> publishedFundingSearch,
            IPublishingResiliencePolicies resiliencePolicies,
            ILogger logger)
        {
            Guard.ArgumentNotNull(publishedFundingSearch, nameof(publishedFundingSearch));
            Guard.ArgumentNotNull(publishedFundingSearch, nameof(publishedFundingSearch));
            Guard.ArgumentNotNull(resiliencePolicies?.PublishedProviderSearchRepository, nameof(resiliencePolicies.PublishedProviderSearchRepository));
            Guard.ArgumentNotNull(logger, nameof(logger));

            _publishedProviderSearch = publishedProviderSearch;
            _publishedFundingSearch = publishedFundingSearch;
            _searchPolicy = resiliencePolicies.PublishedProviderSearchRepository;
            _logger = logger;
        }

        public async Task DeleteFundingSearchDocuments<TIndex>(string fundingStreamId, string fundingPeriodId)
            where TIndex : class
        {
            _logger.Information($"Deleting {typeof(TIndex).Name} documents for {fundingStreamId} {fundingPeriodId}");

            ISearchRepository<TIndex> search = GetRepository<TIndex>();

            int page = 0;

            while (true)
            {
                SearchResults<TIndex> searchResults = await _searchPolicy
                    .ExecuteAsync(() => search.Search(string.Empty,
                            new SearchParameters
                            {
                                Top = 50,
                                SearchMode = SearchMode.Any,
                                Filter = $"fundingStreamId eq '{fundingStreamId}' and fundingPeriodId eq '{fundingPeriodId}'",
                                QueryType = QueryType.Full,
                                Skip = page * 50
                            }
                        )
                    );

                if (searchResults == null || searchResults.Results?.Any() == false)
                {
                    return;
                }

                await _searchPolicy
                    .ExecuteAsync(() => search.Remove(searchResults.Results.Select(_ => _.Result)));

                page += 1;
            }
        }

        private ISearchRepository<SearchIndexType> GetRepository<SearchIndexType>()
            where SearchIndexType : class
        {
            if (typeof(SearchIndexType) == typeof(PublishedProviderIndex)) return (ISearchRepository<SearchIndexType>) _publishedProviderSearch;
            if (typeof(SearchIndexType) == typeof(PublishedFundingIndex)) return (ISearchRepository<SearchIndexType>) _publishedFundingSearch;

            throw new ArgumentOutOfRangeException(nameof(SearchIndexType));
        }
    }
}