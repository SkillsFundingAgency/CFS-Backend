using CalculateFunding.Common.Utility;
using System;
using System.Collections.Generic;
using System.Text;

namespace CalculateFunding.Migrations.Specification.Etl.Migrations
{
    public class Search
    {
        private readonly string _searchkey;

        public readonly string _searchServiceName;

        public string SearchKey => _searchkey;

        public string SearchServiceName => _searchServiceName;

        public Search(string searchkey, string searchServiceName)
        {
            Guard.IsNullOrWhiteSpace(searchkey, nameof(searchkey));
            Guard.IsNullOrWhiteSpace(searchServiceName, nameof(searchServiceName));

            _searchkey = searchkey;
            _searchServiceName = searchServiceName;
        }
    }
}
