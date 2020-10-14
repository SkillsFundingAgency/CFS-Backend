using CalculateFunding.Common.Utility;
using CalculateFunding.Services.Results.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Results.SearchIndex
{
    public class SearchIndexProcessorFactory : ISearchIndexProcessorFactory
    {
        private readonly ISearchIndexProcessor[] _searchIndexProcessors;

        public SearchIndexProcessorFactory(IEnumerable<ISearchIndexProcessor> searchIndexProcessors)
        {
            Guard.ArgumentNotNull(searchIndexProcessors, nameof(searchIndexProcessors));
            _searchIndexProcessors = searchIndexProcessors.ToArray();
        }
        public ISearchIndexProcessor CreateProcessor(string indexWriterType)
        {
            return _searchIndexProcessors.SingleOrDefault(_ => _.IndexWriterType.Equals(indexWriterType, StringComparison.InvariantCultureIgnoreCase)) 
                ?? throw new NotSupportedException($"Search index writer not supported for {indexWriterType}");
        }
    }
}
