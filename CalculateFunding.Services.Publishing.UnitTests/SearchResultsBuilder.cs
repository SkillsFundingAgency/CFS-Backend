using System.Collections.Generic;
using System.Linq;
using CalculateFunding.Repositories.Common.Search;
using CalculateFunding.Tests.Common.Helpers;

namespace CalculateFunding.Services.Publishing.UnitTests
{
    public class SearchResultsBuilder<TResult> : TestEntityBuilder
    {
        private IEnumerable<TResult> _results;

        public SearchResultsBuilder<TResult> WithResults(params TResult[] results)
        {
            _results = results;

            return this;
        }
        
        public SearchResults<TResult> Build()
        {
            return new SearchResults<TResult>
            {
                Results = _results?.Select(_ => new SearchResult<TResult>
                {
                    Result = _
                }).ToList()
            };
        }
    }
}