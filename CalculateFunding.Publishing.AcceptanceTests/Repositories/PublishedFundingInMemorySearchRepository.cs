using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CalculateFunding.Models.Publishing;
using CalculateFunding.Repositories.Common.Search;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace CalculateFunding.Publishing.AcceptanceTests.Repositories
{
    public class PublishedFundingInMemorySearchRepository : ISearchRepository<PublishedFundingIndex>
    {
        public Task DeleteIndex()
        {
            throw new NotImplementedException();
        }

        public Task<ISearchIndexClient> GetOrCreateIndex()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IndexError>> Index(IEnumerable<PublishedFundingIndex> documents)
        {
            throw new NotImplementedException();
        }

        public Task Initialize()
        {
            throw new NotImplementedException();
        }

        public Task<(bool Ok, string Message)> IsHealthOk()
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IndexError>> Remove(IEnumerable<PublishedFundingIndex> documents)
        {
            throw new NotImplementedException();
        }

        public Task RunIndexer()
        {
            return Task.CompletedTask;
        }

        public Task<SearchResults<PublishedFundingIndex>> Search(string searchText, SearchParameters searchParameters = null, bool allResults = false)
        {
            throw new NotImplementedException();
        }

        public Task<PublishedFundingIndex> SearchById(string id)
        {
            throw new NotImplementedException();
        }
    }
}
