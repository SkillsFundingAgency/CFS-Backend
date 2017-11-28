namespace CalculateFunding.Repository
{
    public class SearchResults<T>
    {
        public string SearchTerm { get; set; }
        public long? TotalCount { get; set; }
        public SearchResult<T>[] Results { get; set; }
    }
}