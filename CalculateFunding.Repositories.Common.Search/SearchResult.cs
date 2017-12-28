using System.Collections.Generic;

namespace CalculateFunding.Repositories.Common.Search
{
    public class SearchResult<T>
    {
        public double Score { get; set; }
        public T Result { get; set; }

        public Dictionary<string, IList<string>> HitHighLights { get; set; }
    }
}