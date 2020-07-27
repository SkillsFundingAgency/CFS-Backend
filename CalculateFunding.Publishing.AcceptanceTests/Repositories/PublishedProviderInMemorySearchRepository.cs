using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class PublishedProviderInMemorySearchRepository : ISearchRepository<PublishedProviderIndex>
    {
        public ConcurrentDictionary<string, PublishedProviderIndex> PublishedProviderIndex = new ConcurrentDictionary<string, PublishedProviderIndex>();

        public Task DeleteIndex()
        {
            throw new NotImplementedException();
        }

        public Task<ISearchIndexClient> GetOrCreateIndex()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IndexError>> Index(IEnumerable<PublishedProviderIndex> documents)
        {
            foreach (PublishedProviderIndex item in documents)
            {
                PublishedProviderIndex[item.Id] = item;
            }

            return Task.FromResult(Enumerable.Empty<IndexError>());
        }

        public Task Initialize()
        {
            throw new NotImplementedException();
        }

        public Task<(bool Ok, string Message)> IsHealthOk()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IndexError>> Remove(IEnumerable<PublishedProviderIndex> documents)
        {
            throw new NotImplementedException();
        }

        public Task RunIndexer()
        {
            return Task.CompletedTask;
        }

        public Task<SearchResults<PublishedProviderIndex>> Search(string searchText, SearchParameters searchParameters = null, bool allResults = false)
        {
            throw new NotImplementedException();
        }

        public Task<PublishedProviderIndex> SearchById(string id)
        {
            throw new NotImplementedException();
        }
    }
}
