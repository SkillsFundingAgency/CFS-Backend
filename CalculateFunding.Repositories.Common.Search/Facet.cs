using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CalculateFunding.Repositories.Common.Search
{
    public class Facet
    {
        public Facet()
        {
            FacetValues = Enumerable.Empty<FacetValue>();
        }

        public string Name { get; set; }

        public IEnumerable<FacetValue> FacetValues { get; set; }
    }
}
