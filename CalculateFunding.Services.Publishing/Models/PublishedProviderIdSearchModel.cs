using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Services.Publishing.Models
{
    public class PublishedProviderIdSearchModel
    {
        public PublishedProviderIdSearchModel()
        {
            Filters = new Dictionary<string, string[]>();
            SearchFields = Enumerable.Empty<string>();
        }

        public string SearchTerm { get; set; } = "";
        public IDictionary<string, string[]> Filters { get; set; }
        public IEnumerable<string> SearchFields { get; set; }
    }
}
