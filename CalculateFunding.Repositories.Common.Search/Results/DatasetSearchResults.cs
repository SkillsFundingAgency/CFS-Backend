﻿using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Repositories.Common.Search.Results
{
    public class DatasetSearchResults
    {
        public DatasetSearchResults()
        {
            Results = Enumerable.Empty<DatasetSearchResult>();
            Facets = Enumerable.Empty<Facet>();
        }

        public int TotalCount { get; set; }

        public IEnumerable<DatasetSearchResult> Results { get; set; }

        public IEnumerable<Facet> Facets { get; set; }

    }
}
