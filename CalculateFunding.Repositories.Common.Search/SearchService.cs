using CalculateFunding.Models;
using Microsoft.Azure.Search.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CalculateFunding.Repositories.Common.Search
{
    public abstract class SearchService<T> where T : class
    {
        private readonly ISearchRepository<T> _searchRepository;

        protected IEnumerable<string> DefaultOrderBy = new[] { "lastUpdatedDate desc" };

        public SearchService(ISearchRepository<T> searchRepository)
        {
            _searchRepository = searchRepository;
        }

        protected ISearchRepository<T> SearchRepository
        {
            get { return _searchRepository; }
        }

        protected Task<SearchResults<T>> PerformNonfacetSearch(SearchModel searchModel)
        {
            int skip = (searchModel.PageNumber - 1) * searchModel.Top;

            return Task.Run(() =>
            {
                return _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                {
                    Skip = skip,
                    Top = searchModel.Top,
                    SearchMode = SearchMode.Any,
                    IncludeTotalResultCount = true,
                    Filter = string.Join(" and ", searchModel.Filters.Where(m => !string.IsNullOrWhiteSpace(m.Value.FirstOrDefault())).Select(m => $"({m.Key} eq '{m.Value.First()}')")),
                    OrderBy = searchModel.OrderBy.ToList(),
                    QueryType = QueryType.Full
                });
            });
        }

        protected Task<IEnumerable<Task<SearchResults<T>>>> BuildSearchTasks(SearchModel searchModel, FacetFilterType[] facets)
        {
            if (searchModel == null)
            {
                throw new ArgumentNullException(nameof(searchModel), "Null search model provided");
            }

            if (facets == null)
            {
                throw new ArgumentNullException(nameof(searchModel), "Null search model provided");
            }

            IDictionary<string, string> facetDictionary = BuildFacetDictionary(searchModel, facets);

            IEnumerable<Task<SearchResults<T>>> searchTasks = new Task<SearchResults<T>>[0];

            if (searchModel.IncludeFacets)
            {
                foreach (var filterPair in facetDictionary)
                {
                    searchTasks = searchTasks.Concat(new[]
                    {
                        Task.Run(() =>
                        {
                            var s = facetDictionary.Where(x => x.Key != filterPair.Key && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value);

                            return _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                            {
                                Facets = new[]{ filterPair.Key },
                                SearchMode = SearchMode.Any,
                                SearchFields = new List<string>{ "name" },
                                IncludeTotalResultCount = true,
                                Filter = string.Join(" and ", facetDictionary.Where(x => x.Key != filterPair.Key && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value)),
                                QueryType = QueryType.Full
                            });
                        })
                    });
                }
            }

            searchTasks = searchTasks.Concat(new[]
            {
                BuildItemsSearchTask(facetDictionary, searchModel)
            });

            return Task.FromResult(searchTasks);
        }

        IDictionary<string, string> BuildFacetDictionary(SearchModel searchModel, FacetFilterType[] facets)
        {
            if (searchModel.Filters == null)
                searchModel.Filters = new Dictionary<string, string[]>();

            searchModel.Filters = searchModel.Filters.ToList().Where(m => m.Value != null)
                .ToDictionary(m => m.Key, m => m.Value);

            IDictionary<string, string> facetDictionary = new Dictionary<string, string>();

            foreach (var facet in facets)
            {
                string filter = "";
                if (searchModel.Filters.ContainsKey(facet.Name) && searchModel.Filters[facet.Name] != null)
                {
                    if (facet.IsMulti)
                        filter = $"({facet.Name}/any(x: {string.Join(" or ", searchModel.Filters[facet.Name].Select(x => $"x eq '{x}'"))}))";
                    else
                        filter = $"({string.Join(" or ", searchModel.Filters[facet.Name].Select(x => $"{facet.Name} eq '{x}'"))})";
                }
                facetDictionary.Add(facet.Name, filter);
            }

            return facetDictionary;
        }

        Task<SearchResults<T>> BuildItemsSearchTask(IDictionary<string, string> facetDictionary, SearchModel searchModel)
        {
            int skip = (searchModel.PageNumber - 1) * searchModel.Top;
            return Task.Run(() =>
            {
                return _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                {
                    Skip = skip,
                    Top = searchModel.Top,
                    SearchMode = SearchMode.Any,
                    SearchFields = new List<string> { "name" },
                    IncludeTotalResultCount = true,
                    Filter = string.Join(" and ", facetDictionary.Values.Where(x => !string.IsNullOrWhiteSpace(x))),
                    OrderBy = searchModel.OrderBy.ToList(),
                    QueryType = QueryType.Full
                });
            });
        }
    }
}
