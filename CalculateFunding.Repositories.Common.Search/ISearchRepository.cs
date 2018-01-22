using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace CalculateFunding.Repositories.Common.Search
{
    public interface ISearchRepository<T> where T : class
    {
        Task<ISearchIndexClient> GetOrCreateIndex();
        Task<IList<IndexError>> Index(IList<T> documents);
        Task Initialize();
        Task<SearchResults<T>> Search(string searchTerm, SearchParameters searchParameters = null);
    }
}