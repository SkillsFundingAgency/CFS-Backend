using System;

namespace CalculateFunding.Models
{
    public enum IndexerType
    {
        SqlServer,
        DocumentDb
    }
    public class SearchIndexAttribute : Attribute
    {
        public Type IndexerForType { get; set; }
        public IndexerType IndexerType { get; set; }
        public string DatabaseName { get; set; }
        public string CollectionName { get; set; }
        public string IndexerQuery { get; set; }
    }
}