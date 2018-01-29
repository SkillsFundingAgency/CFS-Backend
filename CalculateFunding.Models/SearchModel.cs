using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Models
{
    public class SearchModel
    {
        public SearchModel()
        {
            OrderBy = Enumerable.Empty<string>();
            Filters = new Dictionary<string, string[]>();
        }

        public int PageNumber { get; set; }

        public int Top { get; set; }

        public string SearchTerm { get; set; } = "";

        public IEnumerable<string> OrderBy { get; set; }

        public IDictionary<string, string[]> Filters { get; set; }
    }
}
