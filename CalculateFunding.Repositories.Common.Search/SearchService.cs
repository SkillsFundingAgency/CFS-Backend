using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Common.Utility;
using CalculateFunding.Models;
using Microsoft.Azure.Search.Models;

namespace CalculateFunding.Repositories.Common.Search
{
    public abstract class SearchService<T> where T : class
    {
        private readonly ISearchRepository<T> _searchRepository;

        protected IEnumerable<string> DefaultOrderBy = new[] { "lastUpdatedDate desc" };

        protected SearchService(ISearchRepository<T> searchRepository)
        {
            Guard.ArgumentNotNull(searchRepository, nameof(searchRepository));
            
            _searchRepository = searchRepository;
        }

        protected ISearchRepository<T> SearchRepository => _searchRepository;

        protected Task<SearchResults<T>> PerformNonFacetSearch(SearchModel searchModel)
        {
            int skip = (searchModel.PageNumber - 1) * searchModel.Top;

            return Task.Run(() =>
            {
                return _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                {
                    Skip = skip,
                    Top = searchModel.Top,
                    SearchMode = (SearchMode)searchModel.SearchMode,
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
            IDictionary<string, string> fullFilterList = BuildFullFilterList(facetDictionary, searchModel.Filters);

            IEnumerable<Task<SearchResults<T>>> searchTasks = new Task<SearchResults<T>>[0];

            if (searchModel.IncludeFacets)
            {
                foreach (var filterPair in facetDictionary)
                {
                    searchTasks = searchTasks.Concat(new[]
                    {
                        Task.Run(() =>
                        {
                            return _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                            {
                                Facets = new[]{ filterPair.Key },
                                SearchMode = (SearchMode)searchModel.SearchMode,
                                SearchFields = new List<string>{ "name" },
                                IncludeTotalResultCount = true,
                                Filter = string.Join(" and ", fullFilterList.Where(x => x.Key != filterPair.Key && !string.IsNullOrWhiteSpace(x.Value)).Select(x => x.Value)),
                                QueryType = QueryType.Full
                            });
                        })
                    });
                }
            }

            searchTasks = searchTasks.Concat(new[]
            {
                BuildItemsSearchTask(fullFilterList, searchModel)
            });

            return Task.FromResult(searchTasks);
        }

        private IDictionary<string, string> BuildFullFilterList(IDictionary<string, string> facetDictionary, IDictionary<string, string[]> searchFilters)
        {
            return facetDictionary.Concat(searchFilters.Where(_ => !facetDictionary.ContainsKey(_.Key))
                    .ToDictionary(_ => _.Key, _ => BuildFilter(_.Key, _.Value)))
                .ToDictionary(_ => _.Key, _ => _.Value);
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

        private string BuildFilter(string name, string[] values)
        {
            return $"({string.Join(" or ", values.Select(value => $"{name} eq '{value}'"))})";
        }

        private Task<SearchResults<T>> BuildItemsSearchTask(IDictionary<string, string> fullFilterList, SearchModel searchModel)
        {
            int skip = (searchModel.PageNumber - 1) * searchModel.Top;
            
            return Task.Run(() =>
            {
                return _searchRepository.Search(searchModel.SearchTerm, new SearchParameters
                {
                    Skip = skip,
                    Top = searchModel.Top,
                    SearchMode = (SearchMode)searchModel.SearchMode,
                    SearchFields = new List<string> { "name" },
                    IncludeTotalResultCount = true,
                    Filter = string.Join(" and ", fullFilterList.Values.Where(x => !string.IsNullOrWhiteSpace(x))),
                    OrderBy = searchModel.OrderBy.ToList(),
                    QueryType = QueryType.Full
                });
            });
        }
    }
}
