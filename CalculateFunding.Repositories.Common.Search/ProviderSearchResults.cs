using System.Collections.Generic;
using System.Linq;

namespace CalculateFunding.Repositories.Common.Search.Results
{

	public class ProviderSearchResults
	{
		public ProviderSearchResults()
		{
			Results = Enumerable.Empty<ProviderSearchResult>();
			Facets = Enumerable.Empty<Facet>();
		}

		public int TotalCount { get; set; }

		public IEnumerable<ProviderSearchResult> Results { get; set; }

		public IEnumerable<Facet> Facets { get; set; }

	}
}
