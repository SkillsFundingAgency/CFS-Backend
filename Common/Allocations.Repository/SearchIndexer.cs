using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Search;
using Microsoft.Azure.Search.Models;

namespace Allocations.Repository
{
    public class SearchIndexer : IDisposable    
    {
        private readonly SearchIndexClient _indexClient;

        public SearchIndexer()
        {
            var searchServiceName = ConfigurationManager.AppSettings["SearchServiceName"];
            var searchServicePrimaryKey = ConfigurationManager.AppSettings["SearchServicePrimaryKey"];
            _indexClient = new SearchIndexClient(searchServiceName, "ProviderResults", new SearchCredentials(searchServicePrimaryKey));
        }
        public async Task<IList<IndexingResult>> AddToIndex<T>(List<T> objectsToIndex) where T : class
        {
            var actions = objectsToIndex.Select(IndexAction.MergeOrUpload);

            var batch = new IndexBatch<T>(actions);

            var result = await _indexClient.Documents.IndexAsync(batch);

            return result.Results;
        }

        public void Dispose()
        {
            _indexClient?.Dispose();
        }
    }
}
