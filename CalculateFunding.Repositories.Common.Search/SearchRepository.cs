using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CalculateFunding.Common.Models;
using CalculateFunding.Models;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace CalculateFunding.Repositories.Common.Search
{
    public class SearchRepositorySettings
    {
        public string SearchServiceName { get; set; }
        public string SearchKey { get; set; }
    }

    public class SearchRepository<T> : ISearchRepository<T>, IDisposable where T : class
    {
        private static readonly string[] SpecialCharacters = { "\\", "+", "-", "&&", "||", "!", "(", ")", "{", "}", "[", "]", "^", "\"", "~", "*", "?", ":", "/" };

        private ISearchIndexClient _searchIndexClient;

        private static readonly SearchParameters DefaultParameters = new SearchParameters { IncludeTotalResultCount = true };
        private readonly SearchRepositorySettings _settings;
        private readonly SearchServiceClient _searchServiceClient;
        private readonly string _indexName;
        private readonly SearchInitializer _searchInitializer;

        public SearchRepository(SearchRepositorySettings settings)
        {
            _indexName = typeof(T).Name.ToLowerInvariant();
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _searchInitializer = new SearchInitializer(_settings.SearchServiceName, _settings.SearchKey, null);
            _searchServiceClient = new SearchServiceClient(_settings.SearchServiceName, new SearchCredentials(_settings.SearchKey));
        }

        public static string ParseSearchText(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return string.Empty;
            }

            searchText = EscapeSpecialCharacters(searchText);
            string[] terms = searchText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            StringBuilder newSearchText = new StringBuilder();
            newSearchText.Append(string.Join("* ", terms));
            newSearchText.Append("*");
            return newSearchText.ToString();
        }

        private static string EscapeSpecialCharacters(string searchText)
        {
            StringBuilder builder = new StringBuilder(searchText);

            // Need to do a prefix search on each term passed in the search text, so append a wildcard character to the end of each term (breaking on spaces)
            // Also need to remove quotation marks as we don't support that
            builder.Replace("\"", string.Empty);

            foreach (string character in SpecialCharacters)
            {
                builder.Replace(character, $"\\{character}");
            }

            return builder.ToString();
        }

        public async Task<(bool Ok, string Message)> IsHealthOk()
        {
            try
            {
                var dataSources = await _searchServiceClient.DataSources.ListAsync();
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public async Task<ISearchIndexClient> GetOrCreateIndex()
        {
            try
            {
                if (_searchIndexClient == null)
                {
                    if (!await _searchServiceClient.Indexes.ExistsAsync(_indexName))
                    {
                        await Initialize();
                    }

                    _searchIndexClient = _searchServiceClient.Indexes.GetClient(_indexName);
                }

                return _searchIndexClient;
            }
            catch (Exception e)
            {
                throw new Exception($"Index failure calling search service client: {e.Message}", e);
            }
        }

        public async Task Initialize()
        {
            await _searchInitializer.Initialise<T>();
        }

        public async Task RunIndexer()
        {
            await _searchInitializer.RunIndexer<T>();
        }

        public async Task<SearchResults<T>> Search(string searchText, SearchParameters searchParameters = null, bool allResults = false)
        {
            var client = await GetOrCreateIndex();

            try
            {
                searchText = ParseSearchText(searchText);

                DocumentSearchResult<T> azureSearchResult = await client.Documents.SearchAsync<T>(searchText, searchParameters ?? DefaultParameters);

                IEnumerable<SearchResult<T>> results = azureSearchResult.Results.Select(x => new SearchResult<T>
                {
                    HitHighLights = x.Highlights,
                    Result = x.Document,
                    Score = x.Score
                });

                SearchContinuationToken continuationToken = azureSearchResult.ContinuationToken;

                // only keep querying to return all items if we want all results to be returned
                while (allResults && continuationToken != null)
                {
                    DocumentSearchResult<T> continuationResult = await client.Documents.ContinueSearchAsync<T>(continuationToken);
                    results = results.Concat(continuationResult.Results.Select(x => new SearchResult<T>
                    {
                        HitHighLights = x.Highlights,
                        Result = x.Document,
                        Score = x.Score
                    }));
                    continuationToken = continuationResult.ContinuationToken;
                }

                var response = new SearchResults<T>
                {
                    SearchTerm = searchText,
                    TotalCount = azureSearchResult.Count,
                    Facets = azureSearchResult.Facets?.Select(x => new Facet
                    {
                        Name = x.Key,
                        FacetValues = x.Value.Where(f => !string.IsNullOrWhiteSpace(f.Value.ToString())).Select(m => new FacetValue
                        {
                            Name = m.Value.ToString(),
                            Count = (int)(m.Count ?? 0)
                        })
                    }).ToList(),
                    Results = results.ToList()
                };
                return response;
            }
            catch (Exception ex)
            {
                throw new FailedToQuerySearchException("Failed to query search", ex);
            }
        }

        public async Task<T> SearchById(string id, SearchParameters searchParameters = null, string IdFieldOverride = "")
        {
            var client = await GetOrCreateIndex();

            string idField = string.IsNullOrWhiteSpace(IdFieldOverride) ? "id" : IdFieldOverride;

            searchParameters = new SearchParameters
            {
                SearchFields = new List<string> { idField },
                Top = 1
            };

            try
            {
                var azureSearchResult = await client.Documents.SearchAsync<T>(id, searchParameters ?? DefaultParameters);

                if (azureSearchResult == null || azureSearchResult.Results == null)
                {
                    return null;
                }

                var response = new SearchResults<T>
                {
                    Results = azureSearchResult.Results.Select(x => new SearchResult<T>
                    {
                        HitHighLights = x.Highlights,
                        Result = x.Document,
                        Score = x.Score
                    }).ToList()
                };
                return response.Results.FirstOrDefault()?.Result;
            }
            catch (Exception ex)
            {
                throw new FailedToQuerySearchException("Failed to query search", ex);
            }

        }

        private async Task<IEnumerable<IndexError>> Index(IEnumerable<T> documents, Func<T, IndexAction<T>> action)
        {
            var client = await GetOrCreateIndex();
            IEnumerable<IndexingResult> indexResults = null;

            foreach (var batch in documents.ToBatches(100))
            {
                try
                {
                    var indexResult = await client.Documents.IndexAsync(new IndexBatch<T>(batch.Select(action)));
                    indexResults = indexResult.Results;
                }
                catch (IndexBatchException ex)
                {
                    indexResults = ex.IndexingResults;
                }
            }

            return indexResults?
                .Where(x => !x.Succeeded)
                .Select(x => new IndexError { Key = x.Key, ErrorMessage = x.ErrorMessage }) ?? Enumerable.Empty<IndexError>();
        }

        public async Task<IEnumerable<IndexError>> Index(IEnumerable<T> documents)
        {
            return await Index(documents, IndexAction.MergeOrUpload);
        }

        public async Task<IEnumerable<IndexError>> Remove(IEnumerable<T> documents)
        {
            return await Index(documents, IndexAction.Delete);
        }

        public async Task DeleteIndex()
        {
            bool indexExists = await _searchServiceClient.Indexes.ExistsAsync(_indexName);

            if (indexExists)
                await _searchServiceClient.Indexes.DeleteAsync(_indexName);
        }

        public void Dispose()
        {
            Dispose(true);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _searchIndexClient?.Dispose();
                _searchServiceClient?.Dispose();
            }
        }
    }
}
