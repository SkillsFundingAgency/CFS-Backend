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

        Task Initialize();

        Task<SearchResults<T>> Search(string searchTerm, SearchParameters searchParameters = null);

        Task<T> SearchById(string id, SearchParameters searchParameters = null, string IdFieldOverride = "");

        Task DeleteIndex();
    }
}