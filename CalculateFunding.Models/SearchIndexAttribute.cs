using System;

namespace CalculateFunding.Models
{
    public class SearchIndexAttribute : Attribute
    {
        public Type IndexerForType { get; set; }
        public IndexerType IndexerType { get; set; }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
        public string IndexName { get; set; }
        public string IndexerQuery { get; set; }
    }
}