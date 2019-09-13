using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace CalculateFunding.Repositories.Common.Search
{
    public interface ISearchRepository<T> where T : class
    {
        Task<(bool Ok, string Message)> IsHealthOk();

        Task<ISearchIndexClient> GetOrCreateIndex();

        Task<IEnumerable<IndexError>> Index(IEnumerable<T> documents);

        Task<IEnumerable<IndexError>> Remove(IEnumerable<T> documents);

        Task Initialize();

        Task RunIndexer();

        Task<SearchResults<T>> Search(string searchText, SearchParameters searchParameters = null, bool allResults = false);

        Task<T> SearchById(string id, SearchParameters searchParameters = null, string IdFieldOverride = "");

        Task DeleteIndex();
    }
}